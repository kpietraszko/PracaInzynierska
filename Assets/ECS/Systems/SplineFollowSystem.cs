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

public class SplineFollowSystem : ComponentSystem
{

	struct ControlPointsData
	{
		public readonly int Length;
		[ReadOnly]
		public SharedComponentDataArray<SplineId> CurveIds;
		public ComponentDataArray<ControlPointId> ControlPointIds;
		public ComponentDataArray<Position2D> Positions;
	}
	struct CarsData
	{
		public readonly int Length;
		public ComponentDataArray<PositionAlongSpline> Positions;
		[ReadOnly]
		public SharedComponentDataArray<SplineId> SplineIds;
		public ComponentArray<Transform> Transforms;
		public ComponentDataArray<Velocity> Velocities;
		public EntityArray Entities;
	}
	[Inject] ControlPointsData ControlPoints;
	[Inject] CarsData Cars;
	protected override void OnCreateManager(int capacity)
	{
		base.OnCreateManager(capacity);
		Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-GB");
	}

	protected override void OnUpdate()
	{
		// ogolnie działa, ale czasem freezuje Unity po przejechaniu spline'a
		var maxMovementError = 0.001f; //1mm na klatke // TODO: tweak
		var controlPointsIndices = new List<int>();
		for (int carIndex = 0; carIndex < Cars.Length; carIndex++)
		{
			var curveId = Cars.SplineIds[carIndex];
			GetCurvesControlPoints(curveId, controlPointsIndices);
			var numOfCurves = controlPointsIndices.Count / 2; //krzywych w tym splinie
			//PostUpdateCommands.SetComponent(Cars.Entities[carIndex], new Velocity(15f)); //do testów TODO: usunąć
			float v = Cars.Velocities[carIndex] * Time.deltaTime; //TODO: predkość brać z komponentu samochodu
			float positionAlongSpline = Cars.Positions[carIndex];
			float leftBound = 0f;
			float rightBound = 1f - positionAlongSpline; 

			float2 position = new float2();
			Vector3 newPosition;
			float splineT;
			float error = float.MaxValue;
			if (positionAlongSpline + maxMovementError >= 1f)
			{
				continue; //to moze psuc przy t>0.9
			}
			int iterations = 0; //do debugowania TODO: usunąć
			float midPoint;
			do
			{
				midPoint = (leftBound+rightBound) / 2f; // punkt środkowy obecnego przedziału
				splineT = positionAlongSpline + midPoint;
				int currentCurveStartIndex;
				if (splineT >= 1f)
				{
					splineT = 1f; //ograniczone do 1 bo spline jest zdefiniowany w zakresie 0-1
					currentCurveStartIndex = numOfCurves * 2 - 2;
				}
				else currentCurveStartIndex = (int)(splineT * numOfCurves) * 2;
				var curveT = frac(splineT * numOfCurves);
				position = lerp(lerp(ControlPoints.Positions[currentCurveStartIndex], ControlPoints.Positions[currentCurveStartIndex + 1], curveT),
										lerp(ControlPoints.Positions[currentCurveStartIndex + 1], ControlPoints.Positions[currentCurveStartIndex + 2], curveT), curveT); //sampling odpowiedniej krzywej beziera
				var currentPosition = Cars.Transforms[carIndex].position;
				newPosition = new Vector3(position.x, 0, position.y);
				error = Vector3.Distance(currentPosition, newPosition) - v;
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
					Debug.LogError("Stuck in do while loop"); // czasem leftBound == rightBound i iteruje w nieskonczonosc
				}
			} while (abs(error) > maxMovementError);
			Cars.Positions[carIndex] = new PositionAlongSpline { Value = splineT };
			var newRotation = Quaternion.LookRotation(newPosition - Cars.Transforms[carIndex].position, Vector3.up); //* Quaternion.Euler(-90,0,0); //obrocone o -90 bo blender - działa
			//File.Delete("debug.csv");
			//using (var sw = new StreamWriter("debug.csv", true))
			//{
			//	sw.WriteLine((newPosition - Cars.Transforms[carIndex].position).magnitude / Time.deltaTime);
			//}
			Cars.Transforms[carIndex].SetPositionAndRotation(newPosition, newRotation);
		}
	}
	private void GetCurvesControlPoints(int curveId, List<int> pointsIndices)
	{
		pointsIndices.Clear();
		for (int pointIndex = 0; pointIndex < ControlPoints.Length; pointIndex++)
		{
			if (ControlPoints.CurveIds[pointIndex] == curveId)
			{
				pointsIndices.Add(pointIndex);
			}
		}
	}
}
