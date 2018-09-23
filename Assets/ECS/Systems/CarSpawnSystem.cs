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
		public ComponentDataArray<CarSpawn> CarSpawns;
		public readonly int Length;
		public EntityArray Entities;
	}
	
	[Inject] UnusedCarsData UnusedCars;
	[Inject] SpawnCarData CarSpawns;
	
	protected override void OnUpdate()
	{
		const float carLength = 4.5f;
		var unusedCarIndex = 0;
		
		for (int i = 0; i < CarSpawns.Length; i++)
		{
			var spawningCar = UnusedCars.Entities[unusedCarIndex++];
			PostUpdateCommands.AddSharedComponent(spawningCar, new SplineId(CarSpawns.CarSpawns[i].SplineId));
			PostUpdateCommands.AddComponent(spawningCar, new PositionAlongSpline(0f));
			PostUpdateCommands.AddComponent(spawningCar, new Accelerating());
			PostUpdateCommands.AddComponent(spawningCar, new Obstacle { SplineId = CarSpawns.CarSpawns[i].SplineId });
			PostUpdateCommands.RemoveComponent<CarSpawn>(CarSpawns.Entities[i]);
		}
	}
}
