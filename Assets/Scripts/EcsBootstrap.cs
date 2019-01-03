using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using System;
using Unity.Rendering;
using Unity.Transforms;
using TMPro;

public class EcsBootstrap : MonoBehaviour
{
    EntityManager Em;

    [SerializeField]
    GameObject CarPrefab;

    [SerializeField]
    Transform CarPoolContainer;

    [SerializeField]
    Mesh TrafficLightMesh;

    [SerializeField]
    Material TrafficLightMaterial;

    [SerializeField]
    Spline[] Splines;

    [SerializeField]
    Scenario[] Scenarios;

    [SerializeField]
    MenuSettings Settings;

    [SerializeField]
    GameObject MainMenuCanvas;

    [SerializeField]
    TextMeshProUGUI CurrentGenerationInfo;

    [SerializeField]
    TextMeshProUGUI CurrentGenotypeInfo;

    [SerializeField]
    TextMeshProUGUI PrevGenerationInfo;

    [SerializeField]
    TextMeshProUGUI FinishInfo;

    [SerializeField]
    GameObject FinishScreen;

    [Serializable]
    class MenuSettings
    {
        public TMP_InputField SimSpeed;
        public TMP_InputField CarsPerSpline;
        public TMP_InputField GenerationPopulation;
        public TMP_InputField NumberOfGenerations;
        public TMP_InputField MinGenerationStepLength;
        public TMP_InputField MaxGenerationStepLength;
    }

    void Awake()
    {
        Em = World.Active.GetOrCreateManager<EntityManager>();
        CurrentGenerationInfo.enabled = false;
        CurrentGenotypeInfo.enabled = false;
        PrevGenerationInfo.enabled = false;
        FinishScreen.SetActive(false);
        FinishInfo.enabled = false;
        MainMenuCanvas.SetActive(true);
    }
    void CreateSplinesAndLightsEntities(EntityManager em)
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
                var lightRenderer = GetTrafficLightMeshInstanceRenderer(TrafficLightMaterial);
                var lightEntity = em.CreateEntity(typeof(TrafficLightId),
                    typeof(Position), typeof(LocalToWorld), typeof(Scale), typeof(PositionAlongSpline), typeof(SplineId));
                var worldPos = Splines[splineIndex].GetPointOnSpline(light) + Vector3.up * 1f;
                if (!Splines[splineIndex].IsTrafficLightHidden)
                {
                    em.AddComponentData(lightEntity, new Obstacle { SplineId = splineIndex, PositionAlongSpline = light, Position = new float2(worldPos.x, worldPos.z) });
                    em.AddSharedComponentData(lightEntity, lightRenderer);
                }
                em.SetComponentData(lightEntity, new TrafficLightId(lightCounter++));
                em.SetComponentData(lightEntity, new Position { Value = worldPos });
                em.SetComponentData(lightEntity, new Scale { Value = new float3(1.5f, 1.5f, 1.5f) });
                em.SetComponentData(lightEntity, new PositionAlongSpline(light));
                em.SetSharedComponentData(lightEntity, new SplineId(splineIndex));
            }
        }
    }
    MeshInstanceRenderer GetTrafficLightMeshInstanceRenderer(Material baseMaterial)
    {
        return new MeshInstanceRenderer
        {
            mesh = TrafficLightMesh,
            material = new Material(baseMaterial), // żeby zmiana koloru jednego światła nie zmieniała wszystkich
            subMesh = 0,
            castShadows = UnityEngine.Rendering.ShadowCastingMode.Off,
            receiveShadows = false
        };
    }
    void InstantiateCars(EntityManager em, int carPoolSize)
    {
        var propBlock = new MaterialPropertyBlock();
        for (int i = 0; i < carPoolSize; i++)
        {
            var newCar = GameObject.Instantiate(CarPrefab, new Vector3(1000f,0f,0f), Quaternion.identity, CarPoolContainer);
            var randomColor = GetRandomHSVColor();

            var meshRenderer = newCar.GetComponent<MeshRenderer>();
            meshRenderer.GetPropertyBlock(propBlock);
            propBlock.SetColor("_Color", randomColor);
            meshRenderer.SetPropertyBlock(propBlock);
        }
    }
    void CreateScenario(EntityManager em, int scenarioIndex)
    {
        var scenarioSteps = Scenarios[scenarioIndex].ScenarioSteps;
        for (int stepIndex = 0; stepIndex < scenarioSteps.Length; stepIndex++)
        {
            var stepEntity = em.CreateEntity();
            em.AddComponentData(stepEntity, new ScenarioStepForDisplay());
            em.AddBuffer<GreenLightInScenarioStep>(stepEntity);
            em.AddComponentData(stepEntity, new ScenarioStep { StepId = stepIndex }); // w tej encji to będzie faktyczna długość obecnie symulowanego
            var buffer = em.GetBuffer<GreenLightInScenarioStep>(stepEntity);
            for (int lightIndex = 0; lightIndex < scenarioSteps[stepIndex].GreenLights.Length; lightIndex++)
            {
                buffer.Add(new GreenLightInScenarioStep((int)scenarioSteps[stepIndex].GreenLights[lightIndex]));
            }
        }
    }
    Color GetRandomHSVColor()
    {
        var hue = UnityEngine.Random.Range(0f, 1f);
        var saturation = 0.5f + UnityEngine.Random.Range(-0.05f, 0.05f);
        var value = 0.4f + UnityEngine.Random.Range(-0.1f, 0.1f);
        return Color.HSVToRGB(hue, saturation, value);
    }
    void SetConfig(EntityManager em, int carsPerSpline, int numberOfScenarioSteps, int generationPopulation,
        float minScenarioStepDuration, float maxScenarioStepDuration, int numberOfGenerations)
    {
        var configEntity = em.CreateEntity();
        em.AddComponentData(configEntity, new Config
        {
            CarsToSpawnPerSpline = carsPerSpline, //20,
            NumberOfScenarioSteps = numberOfScenarioSteps, //4,
            NumberOfSplines = Splines.Length
        });
        em.AddComponentData(configEntity, new GeneticConfig
        {
            GenerationPopulation = generationPopulation, //20,
            MinimumStepDuration = minScenarioStepDuration, //5f,
            MaximumStepDuration = maxScenarioStepDuration, //50f
            NumberOfGenerations = numberOfGenerations
        });
    }
    public void StartApp(int scenarioIndex)
    {
        MainMenuCanvas.SetActive(false);
        Time.fixedDeltaTime = (1 / 30f) / float.Parse(Settings.SimSpeed.text);
        CreateScenario(Em, scenarioIndex);
        CreateSplinesAndLightsEntities(Em);
        var carPoolSize = int.Parse(Settings.CarsPerSpline.text) * Splines.Length;
        InstantiateCars(Em, carPoolSize);
        SetConfig(
            Em,
            int.Parse(Settings.CarsPerSpline.text),
            Scenarios[scenarioIndex].ScenarioSteps.Length,
            int.Parse(Settings.GenerationPopulation.text),
            int.Parse(Settings.MinGenerationStepLength.text),
            int.Parse(Settings.MaxGenerationStepLength.text),
            int.Parse(Settings.NumberOfGenerations.text)
            );

        var uiInfoEntity = Em.CreateEntity();
        Em.AddSharedComponentData(uiInfoEntity, new UiInfo { CurrentGenerationInfo = CurrentGenerationInfo, CurrentGenotypeInfo = CurrentGenotypeInfo,
            PrevGenerationInfo = PrevGenerationInfo, FinishInfo = FinishInfo, FinishScreen = FinishScreen });
        CurrentGenerationInfo.enabled = true;
        CurrentGenotypeInfo.enabled = true;

        var startEntity = Em.CreateEntity();
        Em.AddComponent(startEntity, typeof(Start));

    }
    public void ExitApp()
    {
        Application.Quit();
    }
    void OnApplicationQuit()
    {
        if (!Application.isEditor)
        {
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }
    }
}
