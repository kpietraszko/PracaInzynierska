using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Assertions;

[UpdateAfter(typeof(UnityEngine.Experimental.PlayerLoop.FixedUpdate))]
public class SwitchGenerationSystem : ComponentSystem
{
    struct NewGenerationData
    {
        public readonly int Length;
        public ComponentDataArray<CurrentGeneration> CurrentGenerations;
        public SubtractiveComponent<CurrentGenerationSystemState> NotProcessed;
        public EntityArray Entities;
    }
    struct EndedGenerationData
    {
        public readonly int Length;
        public SubtractiveComponent<CurrentGeneration> Ended;
        public ComponentDataArray<CurrentGenerationSystemState> SystemStates;
        public EntityArray Entities;
    }
    struct GenotypeData
    {
        public readonly int Length;
        public ComponentDataArray<GenotypeId> GenotypeIds;
        public ComponentDataArray<GenotypeSimulationDuration> GenotypesSimulationDurations;
        public EntityArray Entities;
    }
    struct ScenarioStepData
    {
        public readonly int Length;
        public SubtractiveComponent<ScenarioStepForDisplay> NotDisplaySteps;
        public ComponentDataArray<ScenarioStep> ScenarioSteps;
        public EntityArray Entities;
    }
    struct ConfigData
    {
        public readonly int Length;
        public ComponentDataArray<Config> Configs;
    }

    struct CurrentGenerationSystemState : ISystemStateComponentData
    {
        public int GenerationId;
    }

    [Inject] NewGenerationData NewGeneration;
    [Inject] EndedGenerationData EndedGeneration;
    [Inject] GenotypeData Genotypes;
    [Inject] ScenarioStepData ScenarioSteps;
    [Inject] ConfigData Config;

    protected override void OnUpdate()
    {
        Assert.IsTrue(NewGeneration.Length <= 1);
        Assert.IsTrue(EndedGeneration.Length <= 1);
        Assert.AreEqual(Config.Length, 1);

        var config = Config.Configs[0];

        if (NewGeneration.Length > 0)
        {
            int newGenerationId = NewGeneration.CurrentGenerations[0];
            PostUpdateCommands.AddComponent(NewGeneration.Entities[0], new CurrentGenerationSystemState { GenerationId = newGenerationId });
        }

        if (EndedGeneration.Length > 0)
        {
            Assert.AreEqual(Genotypes.Length, config.GenerationPopulation);
            PostUpdateCommands.RemoveComponent<CurrentGenerationSystemState>(EndedGeneration.Entities[0]);
            // znormalizować fitness 
            // wybrac top (mating pool)
            // dla każdego potomka wybrać losowo (ważone znormalizowanym fitnessem) 2 rodziców
            // dla każdego kroku scenariusza potomka wylosować (ważone mutation ratem) czy ma być losowy duration
            // krzyżowanie
            // stworzyć genotypy nowego pokolenia
            var durationsSum = 0f;
            for (int genotypeIndex = 0; genotypeIndex < Genotypes.Length; genotypeIndex++)
            {
                durationsSum += Genotypes.GenotypesSimulationDurations[genotypeIndex];
            }
            var normalizedFitnesses =  new float[Genotypes.Length];
            for (int genotypeIndex = 0; genotypeIndex < Genotypes.Length; genotypeIndex++)
            {
                normalizedFitnesses[genotypeIndex] = Genotypes.GenotypesSimulationDurations[genotypeIndex] / durationsSum;
                PostUpdateCommands.DestroyEntity(Genotypes.Entities[genotypeIndex]);
            }
            var matingPoolIndices = Enumerable.Range(0, Genotypes.Length)
                .OrderByDescending(i => normalizedFitnesses[i])
                .Take(config.MatingPoolSize)
                .Select(i => new GenotypeNormalizedFitness { Index = i, NormalizedFitness = normalizedFitnesses[i]});

            for (int genotypeIndex = 0; genotypeIndex < config.GenerationPopulation; genotypeIndex++)
            {
                var motherIndex = ChooseOneRandomlyWithWeights(matingPoolIndices); // TODO: rename to parent1, parent2 for PC
                var fatherIndex = ChooseOneRandomlyWithWeights(matingPoolIndices, motherIndex);
                for (int stepIndex = 0; stepIndex < config.NumberOfScenarioSteps; stepIndex++)
                {
                    var shouldMutate = Random.Range(0f, 1f) < config.MutationRate;
                    float stepDuration;
                    if (shouldMutate)
                    {
                        stepDuration = Random.Range(config.MinimumStepDuration, config.MaximumStepDuration);
                    }
                    else
                    {
                        int genotypeId;
                        var inheritFromMother = Random.Range(0, 2) == 0;
                        var parentIndex = inheritFromMother ? motherIndex : fatherIndex;
                        var parentGenotypeId = genotypeId = Genotypes.GenotypeIds[parentIndex];
                        // skąd wziąć długości kroków rodziców?
                        stepDuration = GetStepDuration(parentGenotypeId, stepIndex); //stepIndex?
                    }
                }
            }
        }
    }

    int ChooseOneRandomlyWithWeights(IEnumerable<GenotypeNormalizedFitness> genotypesFitnesses, int? except = null) // nieprzetestowane
    {
        while (true)
        {
            var random = Random.Range(0f, 1f);
            var sum = 0f;
            foreach (var genotype in genotypesFitnesses)
            {
                sum += genotype.NormalizedFitness;
                if (sum >= random)
                {
                    var index = genotype.Index;
                    if (index == except)
                    {
                        break;
                    }
                    return genotype.Index;
                }
            }
        }
    }

    float GetStepDuration(int genotypeId, int stepId)
    {
        for (int i = 0; i < ScenarioSteps.Length; i++)
        {
            if (ScenarioSteps.ScenarioSteps[i].GenotypeId == genotypeId &&
                ScenarioSteps.ScenarioSteps[i].StepId == stepId)
            {
                return ScenarioSteps.ScenarioSteps[i].DurationInS;
            }
        }
        throw new System.ArgumentException("Step not found");
    }

    struct GenotypeNormalizedFitness
    {
        public int Index;
        public float NormalizedFitness;
    }
}
