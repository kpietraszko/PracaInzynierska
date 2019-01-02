using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Assertions;

[UpdateAfter(typeof(UnityEngine.Experimental.PlayerLoop.FixedUpdate))]
public class SwitchGenotypeSimSystem : ComponentSystem
{
    struct NewGenotypeData
    {
        public readonly int Length;
        public ComponentDataArray<CurrentlySimulated> CurrentlySimulated;
        public ComponentDataArray<GenotypeId> GenotypeIds;
        public SubtractiveComponent<CurrentlySimulatedSystemState> NotYetProcessed;
        public EntityArray Entities;
    }

    struct FinishedGenotypeData
    {
        public readonly int Length;
        public ComponentDataArray<CurrentlySimulatedSystemState> NotYetRemoved;
        public ComponentDataArray<GenotypeId> GenotypeIds;
        public SubtractiveComponent<CurrentlySimulated> CurrentlySimulated;
        public EntityArray Entities;
    }

    struct EveryGenotypeData
    {
        public readonly int Length;
        public ComponentDataArray<GenotypeId> GenotypeIds;
        public EntityArray Entities;
    }

    struct ConfigData
    {
        public readonly int Length;
        public ComponentDataArray<Config> Configs;
    }

    struct TimeSinceSimulationStartData
    {
        public readonly int Length;
        public ComponentDataArray<TimeSinceSimulationStart> TimeSinceSimulationStart;
    }

    struct CurrentGenerationData
    {
        public ComponentDataArray<CurrentGeneration> CurrentGenerations;
        public EntityArray Entities;
    }

    struct DisplayedStepData
    {
        public readonly int Length;
        public ComponentDataArray<ScenarioStepForDisplay> ScenarioStepsForDisplay;
        public ComponentDataArray<ScenarioStep> ScenarioSteps;
        public EntityArray Entities;
    }

    struct ScenarioStepData
    {
        public readonly int Length;
        public SubtractiveComponent<ScenarioStepForDisplay> NotDisplaySteps;
        public ComponentDataArray<ScenarioStep> ScenarioSteps;
        public EntityArray Entities;
    }

    struct UiInfoData
    {
        [ReadOnly]
        public SharedComponentDataArray<UiInfo> UiInfos;
    }

    struct CurrentlySimulatedSystemState : ISystemStateComponentData { }

    [Inject] NewGenotypeData NewGenotype;
    [Inject] FinishedGenotypeData FinishedGenotype;
    [Inject] ConfigData Config;
    [Inject] TimeSinceSimulationStartData TimeSinceSimulationStart;
    [Inject] EveryGenotypeData AllGenotypes;
    [Inject] CurrentGenerationData CurrentGenerations;
    [Inject] DisplayedStepData DisplayedSteps;
    [Inject] ScenarioStepData ScenarioSteps;
    [Inject] UiInfoData UiInfo;

    protected override void OnUpdate()
    {
        Assert.IsTrue(TimeSinceSimulationStart.Length == 1);
        var justFinished = false;
        Entity? newGenotypeEntity = null;
        int? newGenotypeId = null;

        #region proceed to simulating next genotype
        for (int i = 0; i < FinishedGenotype.Length; i++)
        {
            int finishedGenotypeId = FinishedGenotype.GenotypeIds[i];
            justFinished = true;
            var foundNextGenotype = false;
            var simulationDuration = TimeSinceSimulationStart.TimeSinceSimulationStart[0].Seconds;
            Debug.Log($"Finished genotype #{finishedGenotypeId} in {simulationDuration} s");
            PostUpdateCommands.AddComponent(FinishedGenotype.Entities[i], new GenotypeSimulationDuration(simulationDuration));

            for (int genotypeIndex = 0; genotypeIndex < AllGenotypes.Length; genotypeIndex++)
            {
                if (AllGenotypes.GenotypeIds[genotypeIndex] == finishedGenotypeId + 1)
                {
                    newGenotypeEntity = AllGenotypes.Entities[genotypeIndex];
                    newGenotypeId = AllGenotypes.GenotypeIds[genotypeIndex];
                    PostUpdateCommands.AddComponent(newGenotypeEntity.Value, new CurrentlySimulated());
                    foundNextGenotype = true;
                    //Debug.Log("Switching to next genotype");
                    SetCurrentGenotypeUiInfo(AllGenotypes.GenotypeIds[genotypeIndex] + 1);
                    break;
                }
            }
            PostUpdateCommands.RemoveComponent<CurrentlySimulatedSystemState>(FinishedGenotype.Entities[i]);

            if (!foundNextGenotype)
            {
                PostUpdateCommands.RemoveComponent<CurrentGeneration>(CurrentGenerations.Entities[0]);
                SetCurrentGenotypeUiInfo(1);
                return;
            }
        }
        #endregion

        #region initialize new genotype simulation
        if (NewGenotype.Length == 0 && !justFinished)
            return;
        newGenotypeEntity = newGenotypeEntity ?? NewGenotype.Entities[0];
        newGenotypeId = newGenotypeId ?? NewGenotype.GenotypeIds[0];
        PostUpdateCommands.AddComponent(newGenotypeEntity.Value, new CurrentlySimulatedSystemState());
        Assert.IsFalse(Config.Length == 0);
        var config = Config.Configs[0];
        var carsPerSpline = config.CarsToSpawnPerSpline;
        var numOfSplines = config.NumberOfSplines;
        //Debug.Log($"Starting sim of genotype {newGenotypeId}");
        for (int splineIndex = 0; splineIndex < numOfSplines; splineIndex++)
        {
            for (int carIndex = 0; carIndex < carsPerSpline; carIndex++)
            {
                PostUpdateCommands.CreateEntity();
                PostUpdateCommands.AddComponent(new CarSpawn { SplineId = splineIndex });
            }
        }
        // ustawić encjom które mają ScenarioStepForDisplay ScenarioStep.Duration na duration z ScenarioStep nowego genotypu
        for (int displayStepIndex = 0; displayStepIndex < DisplayedSteps.Length; displayStepIndex++)
        {
            var stepId = DisplayedSteps.ScenarioSteps[displayStepIndex].StepId;
            var stepFromGenotypeIndex = GetStepIndexWithGenotypeId(newGenotypeId.Value, stepId);
            var durationFromCurrentGenotype = ScenarioSteps.ScenarioSteps[stepFromGenotypeIndex];
            PostUpdateCommands.SetComponent(DisplayedSteps.Entities[displayStepIndex],
                new ScenarioStep {
                    DurationInS = durationFromCurrentGenotype,
                    GenotypeId = newGenotypeId.Value,
                    StepId = stepId });
        }
        TimeSinceSimulationStart.TimeSinceSimulationStart[0] = new TimeSinceSimulationStart(0f, 0);
        #endregion
    }
    int GetStepIndexWithGenotypeId(int genotypeId, int stepId)
    {
        for (int i = 0; i < ScenarioSteps.Length; i++)
        {
            if (ScenarioSteps.ScenarioSteps[i].GenotypeId == genotypeId &&
                ScenarioSteps.ScenarioSteps[i].StepId == stepId)
            {
                return i;
            }
        }
        throw new ArgumentException("Step with given id not found");
    }
    void SetCurrentGenotypeUiInfo(int genotypeNumber)
    {
        UiInfo.UiInfos[0].CurrentGenotypeInfo.text = $"Obecny genotyp: {genotypeNumber}";
    }
}
