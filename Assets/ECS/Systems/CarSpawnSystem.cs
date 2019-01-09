using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

[UpdateAfter(typeof(UnityEngine.Experimental.PlayerLoop.FixedUpdate))]
[UpdateBefore(typeof(SplineFollowSystem))]
[UpdateAfter(typeof(CreateGenerationSystem))]
public class CarSpawnSystem : ComponentSystem
{
    struct UnusedCarsData
    {
        public ComponentDataArray<Car> Cars;
        public SubtractiveComponent<SplineId> SplineIds;
        public SubtractiveComponent<PositionAlongSpline> PositionsAlongSpline;
        public ComponentDataArray<MaxVelocity> MaxVelocities;
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
        if (CarSpawns.Length == 0)
        {
            return;
        }
        if (UnusedCars.Length == 0)
        {
            Debug.LogError("Attempting to spawn car, but no unused cars found!");
        }
        NativeList<int> occupiedSplines = new NativeList<int>(10, Allocator.Temp);
        for (int occupiedIndex = 0; occupiedIndex < OccupiedSplines.Length; occupiedIndex++)
        {
            occupiedSplines.Add(OccupiedSplines.OccupiedSplines[occupiedIndex].SplineId);
        }

        // tworzy tylko 1 samochód (łącznie) na klatke
        for (int i = 0; i < CarSpawns.Length; i++)
        {
            if (!occupiedSplines.Contains(CarSpawns.CarSpawns[i].SplineId))
            {
                var splineId = CarSpawns.CarSpawns[i].SplineId;
                var spawningCar = UnusedCars.Entities[0];
                PostUpdateCommands.AddSharedComponent(spawningCar, new SplineId(CarSpawns.CarSpawns[i].SplineId));
                PostUpdateCommands.AddComponent(spawningCar, new PositionAlongSpline(0f));
                PostUpdateCommands.AddComponent(spawningCar, new Accelerating());
                PostUpdateCommands.AddComponent(spawningCar, new Obstacle { SplineId = splineId, PositionAlongSpline = 0 });
                PostUpdateCommands.AddComponent(spawningCar, new Velocity(UnusedCars.MaxVelocities[0] / 2f));
                PostUpdateCommands.AddComponent(spawningCar, new Acceleration());
                PostUpdateCommands.AddComponent(spawningCar, new Heading());
                PostUpdateCommands.AddComponent(spawningCar, new FirstCarFrame());
                PostUpdateCommands.DestroyEntity(CarSpawns.Entities[i]);
                occupiedSplines.Dispose();
                return;
            }
        }
        occupiedSplines.Dispose();
    }
}
