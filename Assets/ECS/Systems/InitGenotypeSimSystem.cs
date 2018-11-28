using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Assertions;

public class InitGenotypeSimSystem : ComponentSystem
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

    struct CurrentlySimulatedSystemState : ISystemStateComponentData { }

    [Inject] NewGenotypeData NewGenotype;
    [Inject] FinishedGenotypeData FinishedGenotype;
    [Inject] ConfigData Config;
    [Inject] TimeSinceSimulationStartData TimeSinceSimulationStart;

    protected override void OnUpdate() // executed once when CurrentlySimulated appears
    {
        Assert.IsTrue(TimeSinceSimulationStart.Length == 1);

        for (int i = 0; i < FinishedGenotype.Length; i++)
        {

        }

        if (NewGenotype.Length == 0)
            return;
        PostUpdateCommands.AddComponent(NewGenotype.Entities[0], new CurrentlySimulatedSystemState());
        Assert.IsFalse(Config.Length == 0);
        var config = Config.Configs[0];
        var carsPerSpline = config.CarsToSpawnPerSpline;
        var numOfSplines = config.NumberOfSplines;
        Debug.Log($"Starting sim of genotype {NewGenotype.GenotypeIds[0].Value}");
        for (int splineIndex = 0; splineIndex < numOfSplines; splineIndex++)
        {
            for (int carIndex = 0; carIndex < carsPerSpline; carIndex++)
            {
                PostUpdateCommands.CreateEntity();
                PostUpdateCommands.AddComponent(new CarSpawn { SplineId = splineIndex });
            }
        }
        TimeSinceSimulationStart.TimeSinceSimulationStart[0] = new TimeSinceSimulationStart(0);
    }
}
