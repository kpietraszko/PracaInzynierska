using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Profiling;
using static Unity.Mathematics.math;

[UpdateAfter(typeof(UnityEngine.Experimental.PlayerLoop.FixedUpdate))]
[UpdateBefore(typeof(AccelerationChangeSystem))]
public class BrakeOnObstacleSystem : ComponentSystem
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
    struct TrafficLightsData
    {
        public readonly int Length;
        public ComponentDataArray<TrafficLightId> TrafficLightsIds;
        [ReadOnly]
        public SharedComponentDataArray<SplineId> SplineIds;
        public ComponentDataArray<PositionAlongSpline> PositionsAlongSpline;
        public EntityArray Entities;
    }
    struct TimeSinceSimulationStartData
    {
        public readonly int Length;
        public ComponentDataArray<TimeSinceSimulationStart> TimeSinceSimulationStart;
    }

    [Inject] StaticObstacleData StaticObstacles;
    [Inject] DynamicObstacleData DynamicObstacles;
    [Inject] NotBrakingCarsData NotBrakingCars;
    [Inject] BrakingCarsData BrakingCars;
    [Inject] TrafficLightsData TrafficLights;
    [Inject] TimeSinceSimulationStartData TimeSinceSimulationStart;
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
        Assert.IsTrue(TimeSinceSimulationStart.TimeSinceSimulationStart.Length == 1);
        if (TimeSinceSimulationStart.TimeSinceSimulationStart[0].StepNumber % 2 == 0)
            return;
        Profiler.BeginSample("BrakeOnObstacleSystem Caching");
        // indeksery ComponentDataArray są wolne, dlatego zapisuję wartości przed wewnętrznymi pętlami
        #region caching
        var staticObstaclesLength = StaticObstacles.Length; // jako że to nie jest tak naprawdę tablica to nawet Length jest wolne
        var dynamicObstaclesLength = DynamicObstacles.Length;
        var staticObstacles = new NativeArray<Obstacle>(staticObstaclesLength, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        var staticObstaclesEntities = new NativeArray<Entity>(staticObstaclesLength, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        var dynamicObstacles = new NativeArray<Obstacle>(dynamicObstaclesLength, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        var dynamicObstaclesEntities = new NativeArray<Entity>(dynamicObstaclesLength, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        var dynamicObstaclesVelocitites = new NativeArray<Velocity>(dynamicObstaclesLength, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        var dynamicObstaclesAccelerations = new NativeArray<Acceleration>(dynamicObstaclesLength, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        for (int i = 0; i < staticObstaclesLength; i++)
        {
            staticObstacles[i] = StaticObstacles.Obstacles[i];
            staticObstaclesEntities[i] = StaticObstacles.Entities[i];
        }
        for (int i = 0; i < dynamicObstaclesLength; i++)
        {
            dynamicObstacles[i] = DynamicObstacles.Obstacles[i];
            dynamicObstaclesEntities[i] = DynamicObstacles.Entities[i];
            dynamicObstaclesVelocitites[i] = DynamicObstacles.Velocities[i];
            dynamicObstaclesAccelerations[i] = DynamicObstacles.Accelerations[i];
        }
        #endregion

        Profiler.EndSample();
        var brakingAcceleration = -15f;
        var brakingDistanceOffset = 4.5f;

        Profiler.BeginSample("BrakeOnObstacleSystem NotBrakingCars");
        if (TimeSinceSimulationStart.TimeSinceSimulationStart[0].StepNumber % 2 == 0) // co drugą klatkę animacji
        {
            for (int carIndex = 0; carIndex < NotBrakingCars.Length; carIndex++)
            {
                var carEntity = NotBrakingCars.Entities[carIndex];
                var v = NotBrakingCars.Velocities[carIndex];
                var splineId = NotBrakingCars.SplineIds[carIndex];
                var positionAlongSpline = NotBrakingCars.PositionsAlongSpline[carIndex];
                var position = NotBrakingCars.Positions[carIndex];
                var headingRad = radians(NotBrakingCars.Headings[carIndex]);
                var headingVector = new float2(cos((float)PI/2f - headingRad), sin((float)PI/2f - headingRad));
                var skipDynamicObstacles = false;
                for (int obstacleIndex = 0; obstacleIndex < staticObstaclesLength; obstacleIndex++)
                {
                    if (GetPositionAlongSplineOfTrafficLight(splineId) <= positionAlongSpline)
                    {   //po przekroczeniu światła wyłączam wszystkie kolizje
                        skipDynamicObstacles = true;
                        break;
                    }
                    // jeśli choć jedna statyczna przeszkoda jest w pobliżu
                    if (ShouldBrake(v, brakingAcceleration, brakingDistanceOffset, carEntity,
                    position, headingVector, true,
                    staticObstacles[obstacleIndex], staticObstaclesEntities[obstacleIndex], default(Velocity), default(Acceleration)))
                    {
                        CarsToStartBraking.Add(carEntity);
                        skipDynamicObstacles = true;
                        break;
                    }
                }
                if (!skipDynamicObstacles)
                {
                    for (int obstacleIndex = 0; obstacleIndex < dynamicObstaclesLength; obstacleIndex++)
                    {
                        // jeśli choć jedna dynamiczna przeszkoda jest w pobliżu
                        if (ShouldBrake(v, brakingAcceleration, brakingDistanceOffset, carEntity,
                        position, headingVector, false,
                        dynamicObstacles[obstacleIndex], dynamicObstaclesEntities[obstacleIndex],
                        dynamicObstaclesVelocitites[obstacleIndex], dynamicObstaclesAccelerations[obstacleIndex]))
                        {
                            CarsToStartBraking.Add(carEntity);
                            break;
                        }
                    }
                }
            }
        }
        Profiler.EndSample();
        Profiler.BeginSample("BrakeOnObstacleSystem BrakingCars");
        if (!(TimeSinceSimulationStart.TimeSinceSimulationStart[0].StepNumber % 3 == 0)) // co trzecią klatkę symulacji
        {
            for (int carIndex = 0; carIndex < BrakingCars.Length; carIndex++)
            {
                var carEntity = BrakingCars.Entities[carIndex];
                var v = BrakingCars.Velocities[carIndex];
                var splineId = BrakingCars.SplineIds[carIndex];
                var positionAlongSpline = BrakingCars.PositionsAlongSpline[carIndex];
                var position = BrakingCars.Positions[carIndex];
                var shouldKeepBraking = false;
                var headingRad = BrakingCars.Headings[carIndex] * (float)PI/180f;
                var headingVector = new float2(cos((float)PI/2f - headingRad), sin((float)PI/2f - headingRad));
                var skipDynamicObstacles = false;
                for (int obstacleIndex = 0; obstacleIndex < staticObstaclesLength; obstacleIndex++)
                {
                    if (ShouldBrake(v, brakingAcceleration, brakingDistanceOffset, carEntity,
                    position, headingVector, true,
                    staticObstacles[obstacleIndex], staticObstaclesEntities[obstacleIndex], default(Velocity), default(Acceleration)))
                    {
                        shouldKeepBraking = true;
                        skipDynamicObstacles = true;
                        break;
                    }
                }
                if (!skipDynamicObstacles)
                {
                    for (int obstacleIndex = 0; obstacleIndex < dynamicObstaclesLength; obstacleIndex++)
                    {
                        if (ShouldBrake(v, brakingAcceleration, brakingDistanceOffset, carEntity,
                        position, headingVector, false,
                        dynamicObstacles[obstacleIndex], dynamicObstaclesEntities[obstacleIndex],
                        dynamicObstaclesVelocitites[obstacleIndex], dynamicObstaclesAccelerations[obstacleIndex]))
                        {
                            shouldKeepBraking = true;
                        }
                    }
                }
                if (!shouldKeepBraking) // jeśli żadna przeszkoda nie jest blisko
                {
                    CarsToStartAccelerating.Add(carEntity);
                    break;
                }
            }
        }
        Profiler.EndSample();
        Profiler.BeginSample("BrakeOnObstacleSystem Applying to EntityManager");
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
        staticObstacles.Dispose();
        staticObstaclesEntities.Dispose();
        dynamicObstacles.Dispose();
        dynamicObstaclesEntities.Dispose();
        dynamicObstaclesVelocitites.Dispose();
        dynamicObstaclesAccelerations.Dispose();
        Profiler.EndSample();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool ShouldBrake(float v, float brakingAcceleration, float brakingDistanceOffset, Entity carEntity,
        float2 carPosition, float2 headingVector, bool isObstacleStatic,
        Obstacle obstacle, Entity obstacleEntity, Velocity obstacleVelocity, Acceleration obstacleAcceleration)
    {
        var distanceToObstacleSquared = lengthsq(obstacle.Position - carPosition);
        if (carEntity == obstacleEntity //ignoruje samego siebie
            || distanceToObstacleSquared > 25f * 25f) //i dalkie przeszkody
        {
            return false;
        }

        var brakingDistance = v * v / -brakingAcceleration / 2; // pole trójkąta

        var distanceObstacleWillTravel = 0f;
        var obstacleRelativePos = obstacle.Position - carPosition;
        var isObstacleInFront = dot(normalize(obstacleRelativePos), headingVector) > 0.985; // 0.97 to 14 stopni, 0.985 to 10 stopni
        if (!isObstacleInFront)
        {
            return false;
        }
        if (!isObstacleStatic)
        {
            var brakingTime = v / -brakingAcceleration;
            if (distanceToObstacleSquared < brakingDistanceOffset * brakingDistanceOffset)
            {
                // zabezpieczenie w przypadkach złego wyliczenia distanceObstacleWillTravel
                return true;
            }
            // ten dystans jest źle liczony jeśli przyspieszenie się zmienia (szczególnie jeśli obstacle właśnie startuje)
            distanceObstacleWillTravel = ((2 * obstacleVelocity + obstacleAcceleration * brakingTime) * brakingTime) / 2f; // pole trapezu
        }
        var distanceToObstacle = distance(obstacle.Position, carPosition); // raczej nie da się zoptymalizować
        bool shouldBrake = distanceToObstacle + distanceObstacleWillTravel <= brakingDistance + brakingDistanceOffset;
        return shouldBrake;
    }
    private float GetPositionAlongSplineOfTrafficLight(int splineId)
    {
        var length = TrafficLights.Length;
        for (int i = 0; i < length; i++)
        {
            if (TrafficLights.SplineIds[i] == splineId)
            {
                return TrafficLights.PositionsAlongSpline[i];
            }
        }
        throw new System.Exception("Spline has no traffic light!");
    }

    protected override void OnDestroyManager()
    {
        base.OnDestroyManager();
        CarsToStartBraking.Dispose();
        CarsToStartAccelerating.Dispose();
    }
}
