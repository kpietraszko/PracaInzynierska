using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;

[UpdateAfter(typeof(UnityEngine.Experimental.PlayerLoop.FixedUpdate))]
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
        public SharedComponentDataArray<SplineId> SplineIds;
        public ComponentDataArray<ControlPointId> ControlPointIds;
        public ComponentDataArray<Position2D> Positions;
    }
    [Inject] ObstacleData Obstacles;
    [Inject] OccupiedSplineStartData OccupiedSplinesStarts;
    [Inject] ControlPointsData ControlPoints;
    Dictionary<int, float2> _splineStartPositions = new Dictionary<int, float2>(10);
    Dictionary<int, int> _sharedSplineStarts = new Dictionary<int, int>(10);
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
        _sharedSplineStarts.Clear();
        for (int i = 0; i < ControlPoints.Length; i++)
        {
            //ustawia pozycję pierwszego punktu jako początek jego spline'a
            if (ControlPoints.ControlPointIds[i] == 0)
            {
                var splineId = ControlPoints.SplineIds[i];
                var position = ControlPoints.Positions[i];
                if (_splineStartPositions.ContainsValue(position))
                {
                    var splineIdWithSameStart = _splineStartPositions.FirstOrDefault(x => all(x.Value == position)).Key; // raczej działa
                    _sharedSplineStarts.Add(splineIdWithSameStart, splineId);
                }
                _splineStartPositions[splineId] = position;
            }
        }
        //sprawdzenie czy początek spline'a jest pusty
        _occupiedSplines.Clear();
        for (int obstacleIndex = 0; obstacleIndex < Obstacles.Length; obstacleIndex++)
        {
            var obstacle = Obstacles.Obstacles[obstacleIndex];
            var spaceRequiredToSpawn = 23.3f;
            if (lengthsq(obstacle.Position - _splineStartPositions[obstacle.SplineId]) < spaceRequiredToSpawn * spaceRequiredToSpawn/*distance(obstacle.Position, _splineStartPositions[obstacle.SplineId]) < spaceRequiredToSpawn*/ /*możliwe że 1.5*carLength plus distanceBetweenCars*/)
            {
                int splineId = obstacle.SplineId;
                if (!_occupiedSplines.Contains(splineId))
                {
                    _occupiedSplines.Add(splineId);
                    PostUpdateCommands.CreateEntity();
                    PostUpdateCommands.AddComponent(new SplineStartOccupied { SplineId = splineId });
                    int? sharedId = null;
                    if (_sharedSplineStarts.Keys.Contains(splineId))
                    {
                        sharedId = _sharedSplineStarts[splineId];
                    }
                    else if (_sharedSplineStarts.Values.Contains(splineId))
                    {
                        sharedId = _sharedSplineStarts.First(x => x.Value == splineId).Key;
                    }
                    if (sharedId.HasValue)
                    {
                        _occupiedSplines.Add((int)sharedId);
                        PostUpdateCommands.CreateEntity();
                        PostUpdateCommands.AddComponent(new SplineStartOccupied { SplineId = (int)sharedId });
                    }
                }
            }
        }
    }
}
