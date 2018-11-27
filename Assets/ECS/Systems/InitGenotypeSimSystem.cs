using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Assertions;

public class InitGenotypeSimSystem : ComponentSystem
{
    struct CurrentGenotypeData
    {
        public readonly int Length;
        public ComponentDataArray<CurrentlySimulated> CurrentlySimulated;
        public ComponentDataArray<GenotypeId> GenotypeIds;
        public SubtractiveComponent<CurrentlySimulatedSystemState> NotYetProcessed;
        public EntityArray Entities;
    }
    struct ConfigData
    {
        public readonly int Length;
        public ComponentDataArray<Config> Configs;
    }
    struct CurrentlySimulatedSystemState : ISystemStateComponentData { }

    [Inject] CurrentGenotypeData CurrentGenotype;
    [Inject] ConfigData Config;

    protected override void OnUpdate()
    {
        if (CurrentGenotype.Length == 0)
            return;
        PostUpdateCommands.AddComponent(CurrentGenotype.Entities[0], new CurrentlySimulatedSystemState());
        Assert.IsFalse(Config.Length == 0);
        var config = Config.Configs[0];
        var carsPerSpline = config.CarsToSpawnPerSpline;
        var numOfSplines = config.NumberOfSplines;
        Debug.Log($"Starting sim of genotype {CurrentGenotype.GenotypeIds[0].Value}");
        for (int splineIndex = 0; splineIndex < numOfSplines; splineIndex++)
        {
            for (int carIndex = 0; carIndex < carsPerSpline; carIndex++)
            {
                PostUpdateCommands.CreateEntity();
                PostUpdateCommands.AddComponent(new CarSpawn { SplineId = splineIndex });
            }
        }
        // on CurrentlySimulated added create car spawns and reset TimeSinceSimulationStart
    }
}
