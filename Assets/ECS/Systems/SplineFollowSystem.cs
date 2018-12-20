using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using System;
using System.IO;
using System.Threading;
using System.Globalization;

[UpdateAfter(typeof(UnityEngine.Experimental.PlayerLoop.FixedUpdate))]
public class SplineFollowSystem : ComponentSystem
{

    struct ControlPointsData
    {
        public readonly int Length;
        [ReadOnly]
        public SharedComponentDataArray<SplineId> SplineIds;
        public ComponentDataArray<ControlPointId> ControlPointIds;
        public ComponentDataArray<Position2D> Positions;
    }

    struct CarsData
    {
        public readonly int Length;
        public ComponentDataArray<Car> Cars;
        public ComponentDataArray<PositionAlongSpline> PositionsAlongSpline;
        [ReadOnly]
        public SharedComponentDataArray<SplineId> SplineIds;
        public ComponentArray<Transform> Transforms;
        public ComponentDataArray<Velocity> Velocities;
        public ComponentDataArray<Position2D> Positions;
        public ComponentDataArray<Obstacle> Obstacles;
        public ComponentDataArray<Heading> Headings;
        public EntityArray Entities;
    }

    struct CurrentGenotypeData
    {
        public readonly int Length;
        public ComponentDataArray<CurrentlySimulated> CurrentlySimulated;
        public EntityArray Entities;
    }
    struct NewCurrentGenerationData
    {
        public readonly int Length;
        public ComponentDataArray<CurrentGeneration> CurrentGenerations;
        public SubtractiveComponent<CurrentGenerationSystemState> NotProcessed;
        public EntityArray Entities;
    }
    struct CurrentGenerationSystemState : ISystemStateComponentData { }

    [Inject] ControlPointsData ControlPoints;
    [Inject] CarsData Cars;
    [Inject] CurrentGenotypeData CurrentGenotype;
    [Inject] NewCurrentGenerationData NewGeneration;

    protected override void OnCreateManager()
    {
        base.OnCreateManager();
        Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-GB");
    }

    protected override void OnUpdate()
    {
        var newGeneration = false;
        if (NewGeneration.Length == 1)
        {
            PostUpdateCommands.AddComponent(NewGeneration.Entities[0], new CurrentGenerationSystemState());
            newGeneration = true;
        }
        if (Cars.Length == 0 && CurrentGenotype.Length == 1 && !newGeneration) // wszystkie samochody opuściły skrzyżowanie
        {
            //Assert.IsTrue(CurrentGenotype.Length == 1);
            var currentGenotypeEntity = CurrentGenotype.Entities[0];
            PostUpdateCommands.RemoveComponent<CurrentlySimulated>(currentGenotypeEntity);
            Debug.Log($"Removing {nameof(CurrentlySimulated)}");
        }

        const float carLength = 4.5f;
        var maxMovementError = 0.004f; // TODO: dostosować
        var controlPoints = new NativeList<Position2D>(10, Allocator.Temp);

        for (int carIndex = 0; carIndex < Cars.Length; carIndex++)
        {
            var splineId = Cars.SplineIds[carIndex];
            GetSplineControlPoints(splineId, ref controlPoints);
            var numOfCurves = controlPoints.Length / 2; //krzywych w tym splinie
            float v = Cars.Velocities[carIndex] * (1/30f)/*Time.fixedDeltaTime*/;
            float vSquared = v * v;
            var obstacle = Cars.Obstacles[carIndex];
            float2 currentPosition = Cars.Positions[carIndex];
            if (EntityManager.HasComponent<FirstCarFrame>(Cars.Entities[carIndex]))
            {
                obstacle.Position = GetSplineFirstControlPoint(Cars.SplineIds[carIndex]);
                // uwaga, nowy samochód w ten sposób może mieć pozycję przed poprzednim samochodem (który ma pozycję przesuniętą do tyłu)
                Cars.Obstacles[carIndex] = obstacle;
                Cars.Positions[carIndex] = obstacle.Position;
                currentPosition = obstacle.Position; //GetSplineFirstControlPoint(Cars.SplineIds[carIndex]);
                PostUpdateCommands.RemoveComponent<FirstCarFrame>(Cars.Entities[carIndex]);
            }
            if (v <= maxMovementError)
            {
                
                continue;
            }
            //Debug.Log("v = " + v);
            float positionAlongSpline = Cars.PositionsAlongSpline[carIndex];
            float leftBound = 0f;
            float rightBound = 1f - positionAlongSpline;

            float2 position = new float2();
            float2 newPosition;
            float splineT;
            float error = float.MaxValue;
            if (positionAlongSpline + maxMovementError >= 0.998f)
            {
                DeSpawnCar(carIndex);
                continue;
            }

            int iterations = 0; //do debugowania TODO: usunąć
            float midPoint;
            var isFirstFrame = EntityManager.HasComponent<FirstCarFrame>(Cars.Entities[carIndex]);

            do
            {
                midPoint = (leftBound + rightBound) / 2f; // punkt środkowy obecnego przedziału
                splineT = positionAlongSpline + midPoint;
                int currentCurveStartIndex;
                if (splineT >= 1f)
                {
                    splineT = 1f; //ograniczone do 1 bo spline jest zdefiniowany w zakresie 0-1
                    currentCurveStartIndex = numOfCurves * 2 - 2;
                }
                else currentCurveStartIndex = (int)(splineT * numOfCurves) * 2;
                var curveT = frac(splineT * numOfCurves);
                var secondPoint = controlPoints[currentCurveStartIndex + 1];
                position = lerp(lerp(controlPoints[currentCurveStartIndex], secondPoint, curveT),
                                       lerp(secondPoint, controlPoints[currentCurveStartIndex + 2], curveT), curveT); //sampling odpowiedniej krzywej beziera

                if (!isFirstFrame)
                    currentPosition = Cars.Positions[carIndex];

                newPosition = position;
                error = distance(currentPosition, newPosition) - v;
                if (error > 0)
                {
                    rightBound = midPoint;
                }
                else
                {
                    leftBound = midPoint;
                }
                if (iterations++ == 100)  //TODO: usunąć
                {
                    Debug.LogError("Stuck in do while loop"); // czasem iteruje w nieskonczonosc
                    Debug.Break();
                }
            } while (abs(error) > maxMovementError);
            Cars.PositionsAlongSpline[carIndex] = new PositionAlongSpline { Value = splineT };
            Cars.Positions[carIndex] = newPosition;
            var newPosVector = new Vector3(newPosition.x, 0f, newPosition.y);
            var newRotation = Quaternion.LookRotation(newPosVector - Cars.Transforms[carIndex].position, Vector3.up); //* Quaternion.Euler(-90,0,0); //obrocone o -90 bo blender - działa
            Cars.Transforms[carIndex].SetPositionAndRotation(newPosVector, newRotation);
            Cars.Headings[carIndex] = new Heading { Value = newRotation.eulerAngles.y };

            obstacle.PositionAlongSpline = splineT;
            // BUG: obstacle jest przed splinem, nie jestem pewien czy to problem
            if (any(currentPosition != newPosition))
            {
                obstacle.Position = GetOffsetObstaclePosition(currentPosition, newPosition, carLength);
                Cars.Obstacles[carIndex] = obstacle;
            }
        }
        controlPoints.Dispose();
    }
    private void GetSplineControlPoints(int splineId, ref NativeList<Position2D> resultPoints)
    {
        resultPoints.Clear();
        var positions = ControlPoints.Positions;
        var splineIds = ControlPoints.SplineIds;
        for (int pointIndex = 0; pointIndex < positions.Length; pointIndex++)
        {
            if (splineIds[pointIndex] == splineId)
            {
                resultPoints.Add(positions[pointIndex]);
            }
        }
    }
    private Position2D GetSplineFirstControlPoint(int splineId)
    {
        for (int pointIndex = 0; pointIndex < ControlPoints.Length; pointIndex++)
        {
            if (ControlPoints.SplineIds[pointIndex] == splineId &&
                ControlPoints.ControlPointIds[pointIndex] == 0)
            {
                return ControlPoints.Positions[pointIndex];
            }
        }
        throw new IndexOutOfRangeException($"First control point not found for spline #{splineId}");
    }
    private float2 GetOffsetObstaclePosition(float2 currentPosition, float2 newPosition, float carLength)
    {

        var obstaclePosition = newPosition - normalize(newPosition - currentPosition) * (carLength / 2); // pozycja przesunięta do tyłu o pół długości samochodu
        //System.Diagnostics.Debug.WriteLine(obstaclePosition);
        return obstaclePosition;
    }
    private void DeSpawnCar(int carIndex)
    {
        var carEntity = Cars.Entities[carIndex];
        PostUpdateCommands.RemoveComponent<Velocity>(carEntity);
        PostUpdateCommands.RemoveComponent<Obstacle>(carEntity);
        PostUpdateCommands.RemoveComponent<PositionAlongSpline>(carEntity);
        PostUpdateCommands.RemoveComponent<SplineId>(carEntity);
        PostUpdateCommands.RemoveComponent<Accelerating>(carEntity);
        PostUpdateCommands.RemoveComponent<Acceleration>(carEntity);
        PostUpdateCommands.RemoveComponent<Heading>(carEntity);
        PostUpdateCommands.SetComponent(carEntity, new Position2D(1000f, 0f)); // może niepotrzebne
        Cars.Transforms[carIndex].position = new Vector3(1000f, 0f, 0f); // wyrzuca samochód gdzieś daleko żeby schować
        // TODO: powinien trafić do unused cars, sprawdzić
    }
}
