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
	GameObject TrafficLightPrefab;


	void Awake()
	{
		var em = World.Active.GetOrCreateManager<EntityManager>();
		CreateSplineEntities(em);
		InstantiateTrafficLights();
		InstantiateCars(em);

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
				var worldPos = Splines[splineIndex].GetPointOnSpline(light);
				em.SetComponentData(lightEntity, new Obstacle { CurveId = splineIndex, PositionAlongCurve = light, Position = worldPos });
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
		GameObject.Instantiate(CarPrefab);
	}
}
