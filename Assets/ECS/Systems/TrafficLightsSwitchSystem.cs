using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Assertions;

[UpdateAfter(typeof(UnityEngine.Experimental.PlayerLoop.FixedUpdate))]
public class TrafficLightsSwitchSystem : ComponentSystem
{
    struct ScenarioStepsData
    {
        public readonly int Length;
        public ComponentDataArray<ScenarioStepId> StepsIds;
        public ComponentDataArray<ScenarioStepDuration> Durations;
        public BufferArray<GreenLightInScenarioStep> GreenLightsBuffers;
        public EntityArray Entities;
    }
    struct RedTrafficLightsData
    {
        public readonly int Length;
        public ComponentDataArray<Obstacle> Obstacles;
        public ComponentDataArray<TrafficLightId> Ids;
        [ReadOnly]
        public SharedComponentDataArray<MeshInstanceRenderer> Renderers;
        public EntityArray Entitites;
    }
    struct GreenTrafficLightsData
    {
        public readonly int Length;
        public ComponentDataArray<TrafficLightId> Ids;
        public SubtractiveComponent<Obstacle> Obstacle;
        public ComponentDataArray<Position> Positions;
        [ReadOnly]
        public SharedComponentDataArray<MeshInstanceRenderer> Renderers;
        public EntityArray Entitites;
    }
    struct TimeSinceSimulationStartData
    {
        public readonly int Length;
        public ComponentDataArray<TimeSinceSimulationStart> Time;
    }

    [Inject] ScenarioStepsData ScenarioSteps;
    [Inject] RedTrafficLightsData RedLights;
    [Inject] GreenTrafficLightsData GreenLights;
    [Inject] TimeSinceSimulationStartData TimeSinceSimulationStart;

    protected override void OnUpdate()
    {
        Assert.IsFalse(TimeSinceSimulationStart.Length == 0);

        var scenarioStepsDurations = new ScenarioStepDuration[ScenarioSteps.Length];
        var scenarioStepsIds = new ScenarioStepId[ScenarioSteps.Length];
        for (int i = 0; i < scenarioStepsDurations.Length; i++)
        {
            scenarioStepsDurations[i] = ScenarioSteps.Durations[i];
            scenarioStepsIds[i] = ScenarioSteps.StepsIds[i];
        }
        Array.Sort(scenarioStepsIds, scenarioStepsDurations);

        var timeSinceSimulationStart = TimeSinceSimulationStart.Time[0];
        var scenarioDuration = scenarioStepsDurations.Sum(x => x.DurationInS);
        var scenarioTimeToSample = timeSinceSimulationStart % scenarioDuration;

        var sumOfStepsDurations = 0f;
        var currentStepId = 0;
        for (int i = 0; i < scenarioStepsDurations.Length; i++)
        {
            sumOfStepsDurations += scenarioStepsDurations[i];
            if (sumOfStepsDurations >= scenarioTimeToSample)
            {
                currentStepId = i;
                break;
            }
        }
        //Debug.Log($"Current scenario step: {currentStepId}");
        var greenLightBuffer = ScenarioSteps.GreenLightsBuffers[currentStepId];
        for (int lightIndex = 0; lightIndex < RedLights.Length; lightIndex++)
        {
            for (int i = 0; i < greenLightBuffer.Length; i++)
            {
                if(greenLightBuffer[i] == RedLights.Ids[lightIndex])
                {
                    // turn it green and remove obstacle
                    RedLights.Renderers[lightIndex].material.color = Color.green;
                    RedLights.Renderers[lightIndex].material.SetColor("_EmissionColor", new Vector4(0f, 1.2f, 0f, 1f));
                    PostUpdateCommands.RemoveComponent<Obstacle>(RedLights.Entitites[lightIndex]);
                }
            }
        }
        for (int lightIndex = 0; lightIndex < GreenLights.Length; lightIndex++)
        {
            bool isInGreenLightBuffer = false;
            for (int i = 0; i < greenLightBuffer.Length; i++)
            {
                if(greenLightBuffer[i] == GreenLights.Ids[lightIndex])
                {
                    isInGreenLightBuffer = true;
                }
            }
            if (!isInGreenLightBuffer)
            {
                // turn it red and add obstacle
                GreenLights.Renderers[lightIndex].material.color = Color.red;
                GreenLights.Renderers[lightIndex].material.SetColor("_EmissionColor", new Vector4(1.5f, 0f, 0f, 1f));
                var lightPosition = GreenLights.Positions[lightIndex];
                PostUpdateCommands.AddComponent(GreenLights.Entitites[lightIndex],
                    new Obstacle { Position = new float2(lightPosition.Value.x, lightPosition.Value.z) });
            }
        }


    }
}
