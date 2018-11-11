using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public class EcsBootstrap : MonoBehaviour
{
	[SerializeField]
	GameObject CarPrefab;

	[SerializeField]
	int CarPoolSize;

	[SerializeField]
	GameObject TrafficLightPrefab;

	[SerializeField]
	int CarsToSpawnTemp;

	[SerializeField]
	Spline[] Splines;

    [SerializeField]
    Scenario[] Scenarios;

	void Awake()
	{
		var em = World.Active.GetOrCreateManager<EntityManager>();
		CreateSplineEntities(em);
		InstantiateTrafficLights();
		InstantiateCars(em);
        CreateArchetypes(em);
        //CreateScenario(em, 0); // TODO: to będzie w handlerze eventu UI
		var startEntity = em.CreateEntity();
		em.AddComponent(startEntity, typeof(Start));
		for (int i = 0; i < CarsToSpawnTemp; i++) // TEMP
		{
            for (int splineIndex = 0; splineIndex < Splines.Length; splineIndex++) // TEMP
            {
                var carToSpawnEntity = em.CreateEntity();
                em.AddComponentData(carToSpawnEntity, new CarSpawn { SplineId = splineIndex });
            }
        }
	}
	void CreateSplineEntities(EntityManager em)
	{
		var numberOfSplines = Splines.Length;
        var lightCounter = 0;
		for (int splineIndex = 0; splineIndex < numberOfSplines; splineIndex++)
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
				var lightEntity = em.CreateEntity(typeof(Obstacle), typeof(TrafficLightId));
				var worldPos = Splines[splineIndex].GetPointOnSpline(light/* - 0.035f*/);
				em.SetComponentData(lightEntity, new Obstacle { SplineId = splineIndex, PositionAlongSpline = light/*-0.035f*/, Position = new float2(worldPos.x, worldPos.z) });
                em.SetComponentData(lightEntity, new TrafficLightId(lightCounter++));
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
        var propBlock = new MaterialPropertyBlock();
		for (int i = 0; i < CarPoolSize; i++)
		{
			var newCar = GameObject.Instantiate(CarPrefab, new Vector3(1000f,0f,0f), Quaternion.identity);
            var randomColor = GetRandomHSVColor();

            var meshRenderer = newCar.GetComponent<MeshRenderer>();
            meshRenderer.GetPropertyBlock(propBlock);
            propBlock.SetColor("_Color", randomColor);
            meshRenderer.SetPropertyBlock(propBlock);
            //meshRenderer.material.color = randomColor; //zmienić na per instance properties
        }
	}
    void CreateScenario(EntityManager em, int scenarioIndex)
    {
        var scenarioSteps = Scenarios[scenarioIndex].ScenarioSteps;
        for (int stepIndex = 0; stepIndex < scenarioSteps.Length; stepIndex++)
        {
            var stepEntity = em.CreateEntity();
            em.AddComponentData(stepEntity, new ScenarioStepId(stepIndex));
            em.AddBuffer<GreenLightInScenarioStep>(stepEntity);
            em.AddComponentData(stepEntity, new ScenarioStepStartTime(stepIndex * 4000L));
            var buffer = em.GetBuffer<GreenLightInScenarioStep>(stepEntity);
            for (int lightIndex = 0; lightIndex < scenarioSteps[stepIndex].GreenLights.Length; lightIndex++)
            {
                buffer.Add(new GreenLightInScenarioStep(scenarioSteps[stepIndex].GreenLights[lightIndex]));
            }
        }
    }
    void CreateArchetypes(EntityManager em) // tworzenie archetypów zmienia układ komponentów w pamięci, zmniejszając liczbe przesunięć i alokacji
    { // poprawka, jednak to nic nie daje, może iteracja po chunkach to naprawia?
        // samochody nieużywane
        //em.CreateArchetype(typeof(Car), typeof(Position2D), typeof(MaxVelocity), typeof(Transform), typeof(MeshFilter), typeof(MeshRenderer));
        //em.CreateArchetype(typeof(CarSpawn));
        //// samochody przyspieszające
        //em.CreateArchetype(typeof(Car), typeof(Position2D), typeof(MaxVelocity), typeof(Transform), typeof(MeshFilter), typeof(MeshRenderer), typeof(SplineId), typeof(PositionAlongSpline), typeof(Acceleration), typeof(Velocity), typeof(Obstacle), typeof(Accelerating));
        //// samochody w stanie pośrednim 
        //em.CreateArchetype(typeof(Car), typeof(Position2D), typeof(MaxVelocity), typeof(Transform), typeof(MeshFilter), typeof(MeshRenderer), typeof(SplineId), typeof(PositionAlongSpline), typeof(Acceleration), typeof(Velocity), typeof(Obstacle));
        //// samochody hamujące
        //em.CreateArchetype(typeof(Car), typeof(Position2D), typeof(MaxVelocity), typeof(Transform), typeof(MeshFilter), typeof(MeshRenderer), typeof(SplineId), typeof(PositionAlongSpline), typeof(Acceleration), typeof(Velocity), typeof(Obstacle), typeof(Decelerating));
        //// punkty kontrolne
        //em.CreateArchetype(typeof(SplineId), typeof(ControlPointId), typeof(Position2D));
        

    }
    Color GetRandomHSVColor()
    {
        var hue = UnityEngine.Random.Range(0f, 1f);
        var saturation = 0.5f + UnityEngine.Random.Range(-0.05f, 0.05f);
        var value = 0.4f + UnityEngine.Random.Range(-0.1f, 0.1f);
        return Color.HSVToRGB(hue, saturation, value);
    }
    private void OnApplicationQuit()
    {
        if (!Application.isEditor)
        {
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }
    }
}
