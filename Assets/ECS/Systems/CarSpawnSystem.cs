using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[UpdateAfter(typeof(UnityEngine.Experimental.PlayerLoop.FixedUpdate))]
[UpdateBefore(typeof(SplineFollowSystem))]
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
        const float carLength = 4.5f;
        var unusedCarIndex = 0;
        List<int> occupiedSplines = new List<int>(10);
        for (int occupiedIndex = 0; occupiedIndex < OccupiedSplines.Length; occupiedIndex++)
        {
            occupiedSplines.Add(OccupiedSplines.OccupiedSplines[occupiedIndex].SplineId);
        }

        // teraz spawnuje tylko 1 samochód (łącznie) na klatke
        if (!occupiedSplines.Contains(CarSpawns.CarSpawns[0].SplineId))
        {
            //Debug.Log($"[Frame {Time.frameCount}]Spline free, spawning");
            var splineId = CarSpawns.CarSpawns[0].SplineId;
            var spawningCar = UnusedCars.Entities[unusedCarIndex];
            PostUpdateCommands.AddSharedComponent(spawningCar, new SplineId(CarSpawns.CarSpawns[0].SplineId));
            PostUpdateCommands.AddComponent(spawningCar, new PositionAlongSpline(0f));
            PostUpdateCommands.AddComponent(spawningCar, new Accelerating());
            // OD RAZU POTRZEBNE OBSTACLE.POSITION 
            PostUpdateCommands.AddComponent(spawningCar, new Obstacle { SplineId = splineId, PositionAlongSpline = 0 });
            PostUpdateCommands.AddComponent(spawningCar, new Velocity(UnusedCars.MaxVelocities[unusedCarIndex] / 2f));
            PostUpdateCommands.AddComponent(spawningCar, new Acceleration());
            PostUpdateCommands.AddComponent(spawningCar, new Heading());
            PostUpdateCommands.AddComponent(spawningCar, new FirstCarFrame());
            PostUpdateCommands.DestroyEntity(CarSpawns.Entities[0]);
            occupiedSplines.Add(splineId);
            unusedCarIndex++;
        }
    }
}
