using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class IsSplineStartClearSystem : ComponentSystem
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
		var splineStartSizeInT = 0.05f;
		//sprawdzenie czy początek spline'a jest pusty
		//usunac wszystkie occupiedSplinesStarts (entities) i dodać dla tych co mają obstacle
		for (int i = 0; i < OccupiedSplinesStarts.Length; i++)
		{
			PostUpdateCommands.DestroyEntity(OccupiedSplinesStarts.Entities[i]);
		}
		List<int> occupiedSplines = new List<int>(10);
		for (int obstacleIndex = 0; obstacleIndex < Obstacles.Length; obstacleIndex++)
		{
			if (Obstacles.Obstacles[obstacleIndex].PositionAlongCurve <= splineStartSizeInT)
			{
				int splineId = Obstacles.Obstacles[obstacleIndex].SplineId;
				if (!occupiedSplines.Contains(splineId))
				{
					PostUpdateCommands.CreateEntity(EntityManager.CreateArchetype(typeof(SplineStartOccupied))); //jak do tego dodać komponent?
				}
			}
		}
	}
}
