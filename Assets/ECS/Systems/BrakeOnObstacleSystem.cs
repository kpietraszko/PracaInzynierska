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
    protected override void OnUpdate()
    {
        for (int obstacleIndex = 0; obstacleIndex < Obstacles.Length; obstacleIndex++)
        {
            var brakingAcceleration = -20f;
            var brakingDistanceOffset = 3.5f;
            for (int carIndex = 0; carIndex < NotBrakingCars.Length; carIndex++)
            {
                var carEntity = NotBrakingCars.Entities[carIndex];
                var v = NotBrakingCars.Velocities[carIndex];
                var a = NotBrakingCars.Accelerations[carIndex];
                if (shouldBrake(v, a, brakingAcceleration, brakingDistanceOffset, obstacleIndex, carEntity,
                    NotBrakingCars.SplineIds[carIndex], NotBrakingCars.PositionsAlongSpline[carIndex], NotBrakingCars.Positions[carIndex]))
                {
                    if (EntityManager.HasComponent<Accelerating>(carEntity))
                    {
                        PostUpdateCommands.RemoveComponent<Accelerating>(carEntity);
                    }
                    PostUpdateCommands.AddComponent(carEntity, new Decelerating());
                }
            }
            for (int carIndex = 0; carIndex < BrakingCars.Length; carIndex++)
            {
                var carEntity = BrakingCars.Entities[carIndex];
                var v = BrakingCars.Velocities[carIndex];
                var a = BrakingCars.Accelerations[carIndex];
                if (!shouldBrake(v, a, brakingAcceleration, brakingDistanceOffset, obstacleIndex, carEntity,
                    BrakingCars.SplineIds[carIndex], BrakingCars.PositionsAlongSpline[carIndex], BrakingCars.Positions[carIndex]))
                {
                    if (EntityManager.HasComponent<Decelerating>(carEntity))
                    {
                        PostUpdateCommands.RemoveComponent<Decelerating>(carEntity); //USUWA PARE RAZY, PORA NA REFACTOR CAŁEGO OnUpdate
                    }
                    PostUpdateCommands.AddComponent(carEntity, new Accelerating());
                }
            }
        }
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
        bool shouldBrake = distance(Obstacles.Obstacles[obstacleIndex].Position, carPosition) <= brakingDistance + brakingDistanceOffset;
        return shouldBrake;
    }
}
