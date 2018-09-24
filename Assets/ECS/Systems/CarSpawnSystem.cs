using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class CarSpawnSystem : ComponentSystem
{
    struct UnusedCarsData
    {
        public ComponentDataArray<Car> Cars;
        public SubtractiveComponent<SplineId> SplineIds;
        public SubtractiveComponent<PositionAlongSpline> PositionsAlongSpline;
        public readonly int Length;
        public EntityArray Entities;
    }
    struct SpawnCarData
    {
        public readonly int Length;
        public ComponentDataArray<CarSpawn> CarSpawns;
        public EntityArray Entities;
    }
    struct OccupiedSplinesData
    {
        public ComponentDataArray<SplineStartOccupied> OccupiedSplines;
        public readonly int Length;
    }
    [Inject] UnusedCarsData UnusedCars;
    [Inject] SpawnCarData CarSpawns;
    [Inject] OccupiedSplinesData OccupiedSplines;

    protected override void OnUpdate()
    {
        const float carLength = 4.5f;
        var unusedCarIndex = 0;
        List<int> occupiedSplines = new List<int>(10);
        for (int occupiedIndex = 0; occupiedIndex < OccupiedSplines.Length; occupiedIndex++)
        {
            occupiedSplines.Add(OccupiedSplines.OccupiedSplines[occupiedIndex].SplineId);
        }

        for (int i = 0; i < CarSpawns.Length; i++)
        {
            foreach (var item in occupiedSplines)
            {
                Debug.Log(item);
            }
            if (!occupiedSplines.Contains(CarSpawns.CarSpawns[i].SplineId))
            {
                Debug.Log("Spawning");
                var splineId = CarSpawns.CarSpawns[i].SplineId;
                var spawningCar = UnusedCars.Entities[unusedCarIndex++];
                PostUpdateCommands.AddSharedComponent(spawningCar, new SplineId(CarSpawns.CarSpawns[i].SplineId));
                PostUpdateCommands.AddComponent(spawningCar, new PositionAlongSpline(0f));
                PostUpdateCommands.AddComponent(spawningCar, new Accelerating());
                PostUpdateCommands.AddComponent(spawningCar, new Obstacle { SplineId = splineId }); //wyjatek?
                PostUpdateCommands.AddComponent(spawningCar, new Velocity());
                PostUpdateCommands.AddComponent(spawningCar, new Acceleration());
                PostUpdateCommands.DestroyEntity(CarSpawns.Entities[i]);
                occupiedSplines.Add(splineId);
            }
        }
    }
}
