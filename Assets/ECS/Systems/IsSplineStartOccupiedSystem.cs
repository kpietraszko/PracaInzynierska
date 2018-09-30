using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using static Unity.Mathematics.math;

[UpdateBefore(typeof(CarSpawnSystem))]
public class IsSplineStartOccupiedSystem : ComponentSystem
{
	struct ObstacleData
	{
		public readonly int Length;
		public ComponentDataArray<Obstacle> Obstacles;
		public EntityArray Entities;
	}
	struct OccupiedSplineStartData
	{
		public readonly int Length;
		public ComponentDataArray<SplineStartOccupied> OccupiedSplinesStarts;
		public EntityArray Entities;
	}
	[Inject] ObstacleData Obstacles;
	[Inject] OccupiedSplineStartData OccupiedSplinesStarts;
	protected override void OnUpdate() //TODO: sprawdzić czy działa
	{
		var splineStartSizeInT = 0.2f;
		const float carLength = 4.5f;
		const float distanceBetweenCars = 0.4f;
		//sprawdzenie czy początek spline'a jest pusty
		//usunac wszystkie occupiedSplinesStarts (entities) i dodać dla tych co mają obstacle
		for (int i = 0; i < OccupiedSplinesStarts.Length; i++)
		{
			PostUpdateCommands.DestroyEntity(OccupiedSplinesStarts.Entities[i]);
		}
		List<int> occupiedSplines = new List<int>(10);
		for (int obstacleIndex = 0; obstacleIndex < Obstacles.Length; obstacleIndex++)
		{
			if (Obstacles.Obstacles[obstacleIndex].PositionAlongCurve <= splineStartSizeInT) //slabe, bedzie zalezalo od dlugosci spline'a //TODO: wymyślić coś lepszego
			{
				int splineId = Obstacles.Obstacles[obstacleIndex].SplineId;
				if (!occupiedSplines.Contains(splineId))
				{
					occupiedSplines.Add(splineId);
					PostUpdateCommands.CreateEntity();//EntityManager.CreateArchetype(typeof(SplineStartOccupied)));
					PostUpdateCommands.AddComponent(new SplineStartOccupied { SplineId = splineId });
				}
			}
		}
		//string debugSplines = "";
		//foreach (var item in occupiedSplines)
		//{
		//	debugSplines += item + ", ";
		//}
		//Debug.Log(debugSplines);
	}
}
