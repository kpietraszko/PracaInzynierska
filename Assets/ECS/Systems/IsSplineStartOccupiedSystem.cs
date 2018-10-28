using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;

[UpdateAfter(typeof(SplineFollowSystem))]
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
    struct ControlPointsData
    {
        public readonly int Length;
        [ReadOnly]
        public SharedComponentDataArray<SplineId> CurveIds;
        public ComponentDataArray<ControlPointId> ControlPointIds;
        public ComponentDataArray<Position2D> Positions;
    }
    [Inject] ObstacleData Obstacles;
	[Inject] OccupiedSplineStartData OccupiedSplinesStarts;
    [Inject] ControlPointsData ControlPoints;
    Dictionary<int, float2> _splineStartPositions = new Dictionary<int, float2>(10);
    List<int> _occupiedSplines = new List<int>(10);
    protected override void OnUpdate() //TODO: sprawdzić czy działa
	{
		const float carLength = 4.5f;
		//usunac wszystkie occupiedSplinesStarts (entities) i dodać dla tych co mają obstacle
		for (int i = 0; i < OccupiedSplinesStarts.Length; i++)
		{
			PostUpdateCommands.DestroyEntity(OccupiedSplinesStarts.Entities[i]);
		}
        _splineStartPositions.Clear();
        for (int i = 0; i < ControlPoints.Length; i++)
        {
            //ustawia pozycję pierwszego punktu jako początek jego spline'a
            if (ControlPoints.ControlPointIds[i] == 0)
            {
                _splineStartPositions[ControlPoints.CurveIds[i]] = ControlPoints.Positions[i];
            }
        }
        //sprawdzenie czy początek spline'a jest pusty
        _occupiedSplines.Clear();
        for (int obstacleIndex = 0; obstacleIndex < Obstacles.Length; obstacleIndex++)
		{
            var obstacle = Obstacles.Obstacles[obstacleIndex];
            var distanceToObstacleDebug = distance(obstacle.Position, _splineStartPositions[obstacle.SplineId]);
            if (lengthsq(obstacle.Position - _splineStartPositions[obstacle.SplineId]) < carLength * carLength/*distance(obstacle.Position, _splineStartPositions[obstacle.SplineId]) < carLength*/ /*możliwe że 1.5*carLength plus distanceBetweenCars*/)
			{
				int splineId = obstacle.SplineId;
				if (!_occupiedSplines.Contains(splineId))
				{
					_occupiedSplines.Add(splineId);
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
