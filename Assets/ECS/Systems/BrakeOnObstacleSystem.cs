using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;

[UpdateBefore(typeof(AccelerationChangeSystem))]

public class BreakOnObstacleSystem : ComponentSystem
{
    struct ObstacleData
    {
        public readonly int Length;
        public ComponentDataArray<Obstacle> Obstacles;
        public EntityArray Entities;
    }
    struct NotBrakingCarsData
    {
        public readonly int Length;
        public ComponentDataArray<Position2D> Positions;
        [ReadOnly]
        public SharedComponentDataArray<SplineId> SplineIds;
        public ComponentDataArray<Accelerating> Acceleratings;
        public SubtractiveComponent<Decelerating> Decelerating; //nie przetwarza samochodów które już hamują
        public ComponentDataArray<Acceleration> Accelerations;
        public ComponentDataArray<Velocity> Velocities;
        public ComponentDataArray<PositionAlongSpline> PositionsAlongSpline;
        public EntityArray Entities;
    }
    struct BrakingCarsData
    {
        public readonly int Length;
        public ComponentDataArray<Position2D> Positions;
        [ReadOnly]
        public SharedComponentDataArray<SplineId> SplineIds;
        public ComponentDataArray<Decelerating> Decelerating;
        public ComponentDataArray<Acceleration> Accelerations;
        public ComponentDataArray<Velocity> Velocities;
        public ComponentDataArray<PositionAlongSpline> PositionsAlongSpline;
        public EntityArray Entities;
    }
    [Inject] ObstacleData Obstacles;
    [Inject] NotBrakingCarsData NotBrakingCars;
    [Inject] BrakingCarsData BrakingCars;
    NativeList<Entity> CarsToStartBraking;
    NativeList<Entity> CarsToStartAccelerating;

    protected override void OnCreateManager()
    {
        base.OnCreateManager();
        CarsToStartBraking = new NativeList<Entity>(100, Allocator.Persistent);
        CarsToStartAccelerating = new NativeList<Entity>(100, Allocator.Persistent);
    }
    protected override void OnUpdate()
    {
        //var archetypesList = new List<EntityArchetype>();
        //using (NativeList<EntityArchetype> allArchetypes = new NativeList<EntityArchetype>(Allocator.Temp))
        //{
        //    EntityManager.GetAllArchetypes(allArchetypes);
        //    for (int i = 0; i < allArchetypes.Length; i++)
        //    {
        //        archetypesList.Add(allArchetypes[i]);
        //    }
        //}
        //Debug.Log($"{archetypesList.Count} archetypes");
        var brakingAcceleration = -15f; //-20f było w miarę, ale chyba trochę szybko hamują
        var brakingDistanceOffset = 3.4f;
        for (int carIndex = 0; carIndex < NotBrakingCars.Length; carIndex++)
        {
            var carEntity = NotBrakingCars.Entities[carIndex];
            var v = NotBrakingCars.Velocities[carIndex];
            var a = NotBrakingCars.Accelerations[carIndex];
            for (int obstacleIndex = 0; obstacleIndex < Obstacles.Length; obstacleIndex++)
            {
                // jeśli choć jedna przeszkoda jest w pobliżu
                if (shouldBrake(v, a, brakingAcceleration, brakingDistanceOffset, obstacleIndex, carEntity,
                NotBrakingCars.SplineIds[carIndex], NotBrakingCars.PositionsAlongSpline[carIndex], NotBrakingCars.Positions[carIndex]))
                {
                    CarsToStartBraking.Add(carEntity);
                    //PostUpdateCommands.RemoveComponent<Accelerating>(carEntity);
                    //PostUpdateCommands.AddComponent(carEntity, new Decelerating());
                    break;
                }
            }
        }

        for (int carIndex = 0; carIndex < BrakingCars.Length; carIndex++)
        {
            var carEntity = BrakingCars.Entities[carIndex];
            var v = BrakingCars.Velocities[carIndex];
            var a = BrakingCars.Accelerations[carIndex];
            var shouldKeepBraking = false;
            for (int obstacleIndex = 0; obstacleIndex < Obstacles.Length; obstacleIndex++)
            {
                if (shouldBrake(v, a, brakingAcceleration, brakingDistanceOffset, obstacleIndex, carEntity,
                BrakingCars.SplineIds[carIndex], BrakingCars.PositionsAlongSpline[carIndex], BrakingCars.Positions[carIndex]))
                {
                    shouldKeepBraking = true;
                }
            }
            if (!shouldKeepBraking) // jeśli żadna przeszkoda nie jest blisko
            {
                //Debug.Log($"Car {BrakingCars.Entities[carIndex].Index}: Road clear, accelerating");
                CarsToStartAccelerating.Add(carEntity);
                //PostUpdateCommands.RemoveComponent<Decelerating>(carEntity);
                //PostUpdateCommands.AddComponent(carEntity, new Accelerating());
                break;
            }
        }
        for (int i = 0; i < CarsToStartBraking.Length; i++)
        {
            var carEntity = CarsToStartBraking[i];
            EntityManager.RemoveComponent<Accelerating>(carEntity);
            EntityManager.AddComponent(carEntity, typeof(Decelerating));
        }
        CarsToStartBraking.Clear();
        for (int i = 0; i < CarsToStartAccelerating.Length; i++)
        {
            var carEntity = CarsToStartAccelerating[i];
            EntityManager.RemoveComponent<Decelerating>(carEntity);
            EntityManager.AddComponent(carEntity, typeof(Accelerating));
        }
        CarsToStartAccelerating.Clear();
    }

    private bool shouldBrake(float v, float a, float brakingAcceleration, float brakingDistanceOffset, int obstacleIndex, Entity carEntity,
        int carSpline, float carPositionAlongSpline, float2 carPosition)
    {
        var brakingDistance = v * v / -brakingAcceleration / 2;
        var obstacle = Obstacles.Obstacles[obstacleIndex];
        if (Obstacles.Entities[obstacleIndex] == carEntity || //ignoruje samego siebie
            obstacle.SplineId != carSpline || //i przeszkody z innych spline'ów
            obstacle.PositionAlongSpline < carPositionAlongSpline) //i przeszkody za samochodem 
        {
            return false;
        }
        var distanceToObstacle = distance(Obstacles.Obstacles[obstacleIndex].Position, carPosition);
        bool shouldBrake = distanceToObstacle <= brakingDistance + brakingDistanceOffset;
        return shouldBrake;
    }
    protected override void OnDestroyManager()
    {
        base.OnDestroyManager();
        CarsToStartBraking.Dispose();
        CarsToStartAccelerating.Dispose();
    }
}
