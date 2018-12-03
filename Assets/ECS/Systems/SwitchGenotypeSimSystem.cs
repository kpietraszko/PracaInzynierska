using System.Collections;
using System.Collections.Generic;
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

    struct CurrentlySimulatedSystemState : ISystemStateComponentData { }

    [Inject] NewGenotypeData NewGenotype;
    [Inject] FinishedGenotypeData FinishedGenotype;
    [Inject] ConfigData Config;
    [Inject] TimeSinceSimulationStartData TimeSinceSimulationStart;
    [Inject] EveryGenotypeData AllGenotypes;

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
            Debug.Log($"Finished genotype #{finishedGenotypeId}");
            var simulationDuration = TimeSinceSimulationStart.TimeSinceSimulationStart[0].Seconds;
            PostUpdateCommands.AddComponent(FinishedGenotype.Entities[finishedGenotypeId], new GenotypeSimulationDuration(simulationDuration));

            for (int genotypeId = 0; genotypeId < AllGenotypes.Length; genotypeId++)
            {
                if (AllGenotypes.GenotypeIds[genotypeId] == finishedGenotypeId + 1)
                {
                    newGenotypeEntity = AllGenotypes.Entities[genotypeId];
                    newGenotypeId = genotypeId;
                    PostUpdateCommands.AddComponent(newGenotypeEntity.Value, new CurrentlySimulated());
                    Debug.Log("Switching to next genotype");
                    break;
                }
            }
            PostUpdateCommands.RemoveComponent<CurrentlySimulatedSystemState>(FinishedGenotype.Entities[i]);
            // jeśli nie ma następnego, to skończyć pokolenie
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
        Debug.Log($"Starting sim of genotype {newGenotypeId}");
        for (int splineIndex = 0; splineIndex < numOfSplines; splineIndex++)
        {
            for (int carIndex = 0; carIndex < carsPerSpline; carIndex++)
            {
                PostUpdateCommands.CreateEntity();
                PostUpdateCommands.AddComponent(new CarSpawn { SplineId = splineIndex });
            }
        }
        TimeSinceSimulationStart.TimeSinceSimulationStart[0] = new TimeSinceSimulationStart(0f, 0);
        #endregion
    }
}
