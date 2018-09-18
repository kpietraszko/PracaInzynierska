using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

[UpdateBefore(typeof(AccelerationChangeSystem))]
public class BreakOnObstacleSystem : ComponentSystem
{
	struct ObstacleData
	{
		public readonly int Length;
		public ComponentDataArray<Obstacle> Obstacles;
	}
	struct NotBrakingCarsData
	{
		public readonly int Length;
		public ComponentDataArray<PositionAlongSpline> Positions;
		[ReadOnly]
		public SharedComponentDataArray<SplineId> SplineIds;
		public SubtractiveComponent<Decelerating> Decelerating; //nie przetwarza samochodów które już hamują
		public ComponentDataArray<Acceleration> Accelerations;
		public ComponentDataArray<Velocity> Velocities;
		public EntityArray Entities;
	}
	struct BrakingCarsData
	{
		public readonly int Length;
		public ComponentDataArray<PositionAlongSpline> Positions;
		[ReadOnly]
		public SharedComponentDataArray<SplineId> SplineIds;
		public ComponentDataArray<Decelerating> Decelerating;
		public ComponentDataArray<Velocity> Velocities;
		public EntityArray Entities;
	}
	[Inject] ObstacleData Obstacles;
	[Inject] NotBrakingCarsData NotBrakingCars;
	protected override void OnUpdate()
	{
		for (int carIndex = 0; carIndex < NotBrakingCars.Length; carIndex++)
		{
			var v = NotBrakingCars.Velocities[carIndex];
			var a = NotBrakingCars.Accelerations[carIndex];
			var brakingAcceleration = -20f;
			var brakingDistance = v*v/-brakingAcceleration/2;
			for (int obstacleIndex = 0; obstacleIndex < Obstacles.Length; obstacleIndex++)
			{

			}
		}
	}
}
