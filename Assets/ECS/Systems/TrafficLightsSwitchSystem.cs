﻿using System;
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
        const float pauseBetweenSteps = 2f;
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
        var scenarioDuration = scenarioStepsDurations.Sum(x => x.DurationInS + pauseBetweenSteps);
        var scenarioTimeToSample = timeSinceSimulationStart % scenarioDuration;

        var sumOfStepsDurations = 0f;
        int? currentStepId = null;
        for (int i = 0; i < scenarioStepsDurations.Length; i++)
        {
            sumOfStepsDurations += scenarioStepsDurations[i];
            if (sumOfStepsDurations >= scenarioTimeToSample)
            {
                currentStepId = i;
                break;
            }
            sumOfStepsDurations += pauseBetweenSteps;
            if (sumOfStepsDurations >= scenarioTimeToSample)
            {
                currentStepId = null;
                break;
            }
        }
        //Debug.Log($"Current scenario step: {currentStepId}");
        var isBetweenSteps = currentStepId == null;
        if (isBetweenSteps) // wszystkie zielone światła zmienia w czerwone
        {
            for (int lightIndex = 0; lightIndex < GreenLights.Length; lightIndex++)
            {
                GreenLights.Renderers[lightIndex].material.color = Color.red;
                GreenLights.Renderers[lightIndex].material.SetColor("_EmissionColor", new Vector4(1.5f, 0f, 0f, 1f));
                var lightPosition = GreenLights.Positions[lightIndex];
                PostUpdateCommands.AddComponent(GreenLights.Entitites[lightIndex],
                    new Obstacle { Position = new float2(lightPosition.Value.x, lightPosition.Value.z) });
            }
            return;
        }
        var greenLightBuffer = ScenarioSteps.GreenLightsBuffers[currentStepId.Value];
        for (int lightIndex = 0; lightIndex < RedLights.Length; lightIndex++)
        { // zmienia czerwone światła, które są w liście zielonych świateł obecnego kroku na zielone
            for (int i = 0; i < greenLightBuffer.Length; i++)
            {
                if(greenLightBuffer[i] == RedLights.Ids[lightIndex])
                {
                    RedLights.Renderers[lightIndex].material.color = Color.green;
                    RedLights.Renderers[lightIndex].material.SetColor("_EmissionColor", new Vector4(0f, 1.2f, 0f, 1f));
                    PostUpdateCommands.RemoveComponent<Obstacle>(RedLights.Entitites[lightIndex]);
                }
            }
        }
        for (int lightIndex = 0; lightIndex < GreenLights.Length; lightIndex++)
        { // zmienia zielone światła, które nie są w liście zielonych świateł obecnego kroku na czerwone
            bool isInGreenLightBuffer = false;
            for (int i = 0; i < greenLightBuffer.Length; i++)
            {
                if(greenLightBuffer[i] == GreenLights.Ids[lightIndex])
                {
                    isInGreenLightBuffer = true;
                    break;
                }
            }
            if (!isInGreenLightBuffer)
            {
                GreenLights.Renderers[lightIndex].material.color = Color.red;
                GreenLights.Renderers[lightIndex].material.SetColor("_EmissionColor", new Vector4(1.5f, 0f, 0f, 1f));
                var lightPosition = GreenLights.Positions[lightIndex];
                PostUpdateCommands.AddComponent(GreenLights.Entitites[lightIndex],
                    new Obstacle { Position = new float2(lightPosition.Value.x, lightPosition.Value.z) });
            }
        }


    }
}
