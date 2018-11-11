using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;

[UpdateAfter(typeof(UnityEngine.Experimental.PlayerLoop.FixedUpdate))]
[UpdateBefore(typeof(AccelerationChangeSystem))]
public class BreakOnObstacleSystem : ComponentSystem
{
    struct StaticObstacleData
    {
        public readonly int Length;
        public ComponentDataArray<Obstacle> Obstacles;
        public SubtractiveComponent<Velocity> Velocity;
        public SubtractiveComponent<Acceleration> Acceleration;
        public EntityArray Entities;
    }
    struct DynamicObstacleData
    {

        public readonly int Length;
        public ComponentDataArray<Obstacle> Obstacles;
        public ComponentDataArray<Velocity> Velocities;
        public ComponentDataArray<Acceleration> Accelerations;
        public ComponentDataArray<Heading> Headings;
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
        public ComponentDataArray<Heading> Headings;
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
        public ComponentDataArray<Heading> Headings;
        public EntityArray Entities;
    }
    [Inject] StaticObstacleData StaticObstacles;
    [Inject] DynamicObstacleData DynamicObstacles;
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
        // indeksery ComponentDataArray są wolne, dlatego zapisuję wartości przed wewnętrznymi pętlami
        var brakingAcceleration = -15f; //-20f było w miarę, ale chyba trochę szybko hamują
        var brakingDistanceOffset = 3.6f;
        for (int carIndex = 0; carIndex < NotBrakingCars.Length; carIndex++)
        {
            var carEntity = NotBrakingCars.Entities[carIndex];
            var v = NotBrakingCars.Velocities[carIndex];
            var a = NotBrakingCars.Accelerations[carIndex];
            var splineId = NotBrakingCars.SplineIds[carIndex];
            var positionAlongSpline = NotBrakingCars.PositionsAlongSpline[carIndex];
            var position = NotBrakingCars.Positions[carIndex];
            var headingRad = NotBrakingCars.Headings[carIndex] * (float)PI/180f;
            var headingVector = new float2(cos((float)PI/2f - headingRad), sin((float)PI/2f - headingRad));
            var alreadyAddedToBraking = false;
            for (int obstacleIndex = 0; obstacleIndex < StaticObstacles.Length; obstacleIndex++)
            {
                // jeśli choć jedna statyczna przeszkoda jest w pobliżu
                if (shouldBrake(v, a, brakingAcceleration, brakingDistanceOffset, obstacleIndex, carEntity,
                splineId, positionAlongSpline, position,headingVector, true))
                {
                    CarsToStartBraking.Add(carEntity);
                    alreadyAddedToBraking = true;
                    break;
                }
            }
            if (!alreadyAddedToBraking)
            {
                for (int obstacleIndex = 0; obstacleIndex < DynamicObstacles.Length; obstacleIndex++)
                {
                    // jeśli choć jedna dynamiczna przeszkoda jest w pobliżu
                    if (shouldBrake(v, a, brakingAcceleration, brakingDistanceOffset, obstacleIndex, carEntity,
                    splineId, positionAlongSpline, position,
                    headingVector, false))
                    {
                        CarsToStartBraking.Add(carEntity);
                        break;
                    }
                }
            }
        }

        for (int carIndex = 0; carIndex < BrakingCars.Length; carIndex++)
        {
            var carEntity = BrakingCars.Entities[carIndex];
            var v = BrakingCars.Velocities[carIndex];
            var a = BrakingCars.Accelerations[carIndex];
            var splineId = BrakingCars.SplineIds[carIndex];
            var positionAlongSpline = BrakingCars.PositionsAlongSpline[carIndex];
            var position = BrakingCars.Positions[carIndex];
            var shouldKeepBraking = false;
            var headingRad = BrakingCars.Headings[carIndex] * (float)PI/180f;
            var headingVector = new float2(cos((float)PI/2f - headingRad), sin((float)PI/2f - headingRad));
            for (int obstacleIndex = 0; obstacleIndex < StaticObstacles.Length; obstacleIndex++)
            {
                if (shouldBrake(v, a, brakingAcceleration, brakingDistanceOffset, obstacleIndex, carEntity,
                splineId, positionAlongSpline, position, headingVector, true))
                {
                    shouldKeepBraking = true;
                }
            }
            for (int obstacleIndex = 0; obstacleIndex < DynamicObstacles.Length; obstacleIndex++)
            {
                if (shouldBrake(v, a, brakingAcceleration, brakingDistanceOffset, obstacleIndex, carEntity,
                splineId, positionAlongSpline, position, headingVector, false))
                {
                    shouldKeepBraking = true;
                }
            }
            if (!shouldKeepBraking) // jeśli żadna przeszkoda nie jest blisko
            {
                CarsToStartAccelerating.Add(carEntity);
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
        int carSpline, float carPositionAlongSpline, float2 carPosition, float2 headingVector, bool isObstacleStatic)
    {
        var obstacleEntity = isObstacleStatic ? StaticObstacles.Entities[obstacleIndex] : DynamicObstacles.Entities[obstacleIndex];
        if (obstacleEntity == carEntity) //ignoruje samego siebie
        {
            return false;
        }
        // TODO: native array staticObstacles i dynamicObstacles na początku klatki? indekser tutaj spowalnia
        var obstacle = isObstacleStatic ? StaticObstacles.Obstacles[obstacleIndex] : DynamicObstacles.Obstacles[obstacleIndex];
        if (lengthsq(obstacle.Position - carPosition) > 20f * 20f)
        {
            return false;
        }
        var brakingTime = v / -brakingAcceleration;
        var brakingDistance = v * v / -brakingAcceleration / 2; // pole trójkąta

        var distanceObstacleWillTravel = 0f;
        var obstacleRelativePos = obstacle.Position - carPosition;
        var isObstacleInFront = dot(normalize(obstacleRelativePos), headingVector) > 0.97; // mniej niz 14 stopni
        if (!isObstacleStatic)
        {
            if (obstacle.SplineId != carSpline && !isObstacleInFront)
                return false;

            float obstacleVelocity = DynamicObstacles.Velocities[obstacleIndex];
            float obstacleAcceleration = DynamicObstacles.Accelerations[obstacleIndex];
            distanceObstacleWillTravel = ((2 * obstacleVelocity + obstacleAcceleration * brakingTime) * brakingTime) / 2f; // pole trapezu
        }
        else
        {
            if (!isObstacleInFront)
                return false;
        }
        var distanceToObstacle = distance(obstacle.Position, carPosition);
        bool shouldBrake = distanceToObstacle + distanceObstacleWillTravel <= brakingDistance + brakingDistanceOffset;
        return shouldBrake;
    }
    protected override void OnDestroyManager()
    {
        base.OnDestroyManager();
        CarsToStartBraking.Dispose();
        CarsToStartAccelerating.Dispose();
    }
}
