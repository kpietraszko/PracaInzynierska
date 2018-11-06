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
        const float carLength = 4.5f;
        var unusedCarIndex = 0;
        List<int> occupiedSplines = new List<int>(10);
        for (int occupiedIndex = 0; occupiedIndex < OccupiedSplines.Length; occupiedIndex++)
        {
            occupiedSplines.Add(OccupiedSplines.OccupiedSplines[occupiedIndex].SplineId);
        }

        for (int i = 0; i < CarSpawns.Length; i++)
        {
            // BUG: przy którymś samochodzie nagle z każdą klatką usuwa jeden carSpawn mimo że nie spawnuje bo spline zajęty
            // spline start zostaje usunięty (chyba prawidłowo) i zaczyna sie usuwanie z każdą kolejną klatką
            // usuwa nie tylko Spawny ale i UnusedCars
            // jak ustawione na 4 lub 5 samochodów to działa wszystko ok, jak 6 lub wiecej to gdy 4. samochód minie początek to zaczyna sie psuć
            // wygląda na to że zależy od długości spline'a, im dłuższy tym więcej samochodów spawnuje sie poprawnie zanim sie zepsuje
            if (!occupiedSplines.Contains(CarSpawns.CarSpawns[i].SplineId))
            {
                //Debug.Log($"[Frame {Time.frameCount}]Spline free, spawning");
                var splineId = CarSpawns.CarSpawns[i].SplineId;
                var spawningCar = UnusedCars.Entities[unusedCarIndex++];
                PostUpdateCommands.AddSharedComponent(spawningCar, new SplineId(CarSpawns.CarSpawns[i].SplineId));
                PostUpdateCommands.AddComponent(spawningCar, new PositionAlongSpline(0f));
                PostUpdateCommands.AddComponent(spawningCar, new Accelerating());
                // OD RAZU POTRZEBNE OBSTACLE.POSITION 
                PostUpdateCommands.AddComponent(spawningCar, new Obstacle { SplineId = splineId, PositionAlongSpline = 0 }); 
                PostUpdateCommands.AddComponent(spawningCar, new Velocity());
                PostUpdateCommands.AddComponent(spawningCar, new Acceleration());
                PostUpdateCommands.AddComponent(spawningCar, new FirstCarFrame());
                PostUpdateCommands.DestroyEntity(CarSpawns.Entities[i]);
                occupiedSplines.Add(splineId);
            }
        }
    }
}
