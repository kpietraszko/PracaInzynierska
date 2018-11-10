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
    [Inject] ControlPointsData ControlPoints;
    [Inject] CarsData Cars;

    protected override void OnCreateManager()
    {
        base.OnCreateManager();
        Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-GB");
    }

    protected override void OnUpdate()
    {
        const float carLength = 4.5f;
        var maxMovementError = 0.001f; //chyba wystarczy 1mm na klatke // TODO: dostosować
        //var allControlPoints = ControlPoints.Positions.GetChunkArray(0, ControlPoints.Length);
        var controlPoints = new NativeList<Position2D>(10, Allocator.Temp);

        for (int carIndex = 0; carIndex < Cars.Length; carIndex++)
        {
            var splineId = Cars.SplineIds[carIndex];
            GetSplineControlPoints(splineId, ref controlPoints);
            var numOfCurves = controlPoints.Length / 2; //krzywych w tym splinie
            float v = Cars.Velocities[carIndex] * Time.fixedDeltaTime;
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
            if (positionAlongSpline + maxMovementError >= 0.99f)
            {
                PostUpdateCommands.RemoveComponent<Velocity>(Cars.Entities[carIndex]);
                PostUpdateCommands.RemoveComponent<Obstacle>(Cars.Entities[carIndex]);
                PostUpdateCommands.RemoveComponent<PositionAlongSpline>(Cars.Entities[carIndex]);
                PostUpdateCommands.RemoveComponent<SplineId>(Cars.Entities[carIndex]);
                PostUpdateCommands.RemoveComponent<Accelerating>(Cars.Entities[carIndex]);
                PostUpdateCommands.SetComponent(Cars.Entities[carIndex], new Position2D(1000f, 0f)); // może niepotrzebne
                Cars.Transforms[carIndex].position = new Vector3(1000f, 0f, 0f); // wyrzuca samochód gdzieś daleko żeby schować
                // TODO: powinien trafić do unused cars, sprawdzić
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
                // TODO: wolne, bo używa indeksora ComponentDataArray, który nie jest tablicą i robi różne dodatkowe operacje
                // chyba jednak nie ma tu istotnego wpływu na wydajność
                //position = lerp(lerp(ControlPoints.Positions[currentCurveStartIndex], ControlPoints.Positions[currentCurveStartIndex + 1], curveT),
                //                        lerp(ControlPoints.Positions[currentCurveStartIndex + 1], ControlPoints.Positions[currentCurveStartIndex + 2], curveT), curveT); //sampling odpowiedniej krzywej beziera
                position = lerp(lerp(controlPoints[currentCurveStartIndex], controlPoints[currentCurveStartIndex + 1], curveT),
                                       lerp(controlPoints[currentCurveStartIndex + 1], controlPoints[currentCurveStartIndex + 2], curveT), curveT); //sampling odpowiedniej krzywej beziera

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
}
