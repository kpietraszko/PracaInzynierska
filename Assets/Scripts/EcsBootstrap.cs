using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public class EcsBootstrap : MonoBehaviour
{
	[SerializeField]
	Spline[] Splines;
	[SerializeField]
	GameObject CarPrefab;
	[SerializeField]
	int CarPoolSize;
	[SerializeField]
	GameObject TrafficLightPrefab;
	[SerializeField]
	int CarsToSpawnTemp;

	void Awake()
	{
		var em = World.Active.GetOrCreateManager<EntityManager>();
		CreateSplineEntities(em);
		InstantiateTrafficLights();
		InstantiateCars(em);
		var startEntity = em.CreateEntity();
		em.AddComponent(startEntity, typeof(Start));
		for (int i = 0; i < CarsToSpawnTemp; i++) //temp
		{
			var carToSpawnEntity = em.CreateEntity();
			em.AddComponentData(carToSpawnEntity, new CarSpawn { SplineId = 0 });
		}
	}
	void CreateSplineEntities(EntityManager em)
	{
		var numberOfCurves = Splines.Length;
		for (int splineIndex = 0; splineIndex < numberOfCurves; splineIndex++)
		{
			for (int pointIndex = 0; pointIndex < Splines[splineIndex].ControlPointCount; pointIndex++)
			{
				var entity = em.CreateEntity(typeof(SplineId), typeof(ControlPointId), typeof(Position2D));
				em.SetSharedComponentData(entity, new SplineId { Value = splineIndex });
				em.SetComponentData(entity, new ControlPointId { Value = pointIndex });
				em.SetComponentData(entity, new Position2D
				{
					Value =
						  new float2(Splines[splineIndex][pointIndex].x,
						  Splines[splineIndex][pointIndex].z)
				});
			}
			foreach (var light in Splines[splineIndex].TrafficLights)
			{
				var lightEntity = em.CreateEntity(typeof(Obstacle));
				var worldPos = Splines[splineIndex].GetPointOnSpline(light/* - 0.035f*/);
				em.SetComponentData(lightEntity, new Obstacle { SplineId = splineIndex, PositionAlongSpline = light/*-0.035f*/, Position = new float2(worldPos.x, worldPos.z) });
			}
		}
	}
	void InstantiateTrafficLights()
	{
		foreach (var spline in Splines)
		{
			foreach (var lightPos in spline.TrafficLights)
			{
				GameObject.Instantiate(TrafficLightPrefab, spline.GetPointOnSpline(lightPos), Quaternion.identity, transform);
			}
		}
	}
	void InstantiateCars(EntityManager em)
	{
		for (int i = 0; i < CarPoolSize; i++)
		{
			var newCar = GameObject.Instantiate(CarPrefab, new Vector3(1000f,0f,0f), Quaternion.identity);
			var meshRenderer = newCar.GetComponent<MeshRenderer>();
			var hue = UnityEngine.Random.Range(0f, 1f);
            var saturation = 0.55f + UnityEngine.Random.Range(-0.05f, 0.05f);
            var value = 0.5f + UnityEngine.Random.Range(-0.1f, 0.1f);
            var randomColor = Color.HSVToRGB(hue, saturation, value);
			meshRenderer.material.color = randomColor;
		}
	}
}
