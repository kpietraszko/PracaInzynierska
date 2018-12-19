using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Assertions;

[UpdateAfter(typeof(UnityEngine.Experimental.PlayerLoop.FixedUpdate))]
[UpdateAfter(typeof(SwitchGenotypeSimSystem))]
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
    struct GeneticConfigData
    {
        public readonly int Length;
        public ComponentDataArray<GeneticConfig> GeneticConfigs;
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
    [Inject] GeneticConfigData GeneticConfig;

    protected override void OnCreateManager()
    {
        File.AppendAllText(Path.Combine(Application.persistentDataPath, "logs", "evolutionLog.txt"), System.DateTime.Now.ToString("MM-dd-yyyy_HH:mm") + System.Environment.NewLine);
    }

    protected override void OnUpdate()
    {
        Assert.IsTrue(NewGeneration.Length <= 1);
        Assert.IsTrue(EndedGeneration.Length <= 1);
        Assert.AreEqual(Config.Length, 1);

        var config = Config.Configs[0];
        var geneticConfig = GeneticConfig.GeneticConfigs[0];

        if (NewGeneration.Length > 0)
        {
            int newGenerationId = NewGeneration.CurrentGenerations[0];
            PostUpdateCommands.AddComponent(NewGeneration.Entities[0], new CurrentGenerationSystemState { GenerationId = newGenerationId });
        }

        if (EndedGeneration.Length > 0)
        {
            Assert.AreEqual(Genotypes.Length, geneticConfig.GenerationPopulation);

            var previousGenerationId = EndedGeneration.SystemStates[0].GenerationId;
            PostUpdateCommands.RemoveComponent<CurrentGenerationSystemState>(EndedGeneration.Entities[0]);
            // znormalizować fitness 
            // wybrac top (mating pool)
            // dla każdego potomka wybrać losowo (ważone znormalizowanym fitnessem) 2 rodziców
            // dla każdego kroku scenariusza potomka wylosować (ważone mutation ratem) czy ma być losowy duration
            // krzyżowanie
            // stworzyć genotypy nowego pokolenia
            var fitnessesSum = 0f;
            float maxDuration = Genotypes.GenotypesSimulationDurations[0];
            for (int genotypeIndex = 0; genotypeIndex < Genotypes.Length; genotypeIndex++)
            {
                var duration = Genotypes.GenotypesSimulationDurations[genotypeIndex];
                if (duration > maxDuration)
                    maxDuration = duration;
            }
            var durations = new float[Genotypes.Length];
            var fitnesses = new float[Genotypes.Length];
            for (int genotypeIndex = 0; genotypeIndex < Genotypes.Length; genotypeIndex++)
            {
                durations[genotypeIndex] = Genotypes.GenotypesSimulationDurations[genotypeIndex];
                // dostosowanie to odwrotnosc czasu trwania symulacji, jako efekt uboczny najdlużej trwający genotyp nie ma szans na potomostwo
                var fitness = maxDuration - Genotypes.GenotypesSimulationDurations[genotypeIndex]; 
                fitnesses[genotypeIndex] = fitness;
                fitnessesSum += fitness;
            }
            var normalizedFitnesses = new float[Genotypes.Length];
            for (int genotypeIndex = 0; genotypeIndex < Genotypes.Length; genotypeIndex++)
            {
                normalizedFitnesses[genotypeIndex] = fitnesses[genotypeIndex] / fitnessesSum;
                PostUpdateCommands.DestroyEntity(Genotypes.Entities[genotypeIndex]);
            }

            // wyglada na to że działa ok
            var matingPoolIndices = Enumerable.Range(0, Genotypes.Length)
                .OrderByDescending(i => normalizedFitnesses[i])
                .Select(i => new GenotypeNormalizedFitness { Index = i, NormalizedFitness = normalizedFitnesses[i]});

            var debugNormalizedFitnessesSum = normalizedFitnesses.Sum();
            var debugMatingPool = matingPoolIndices.ToArray();
            var avg = durations.Average();
            var orderedDurations = durations.OrderBy(x => x);
            var median = orderedDurations.ElementAt(durations.Length / 2 - 1) 
                + orderedDurations.ElementAt(durations.Length / 2) / 2;
            var best = orderedDurations.First();
            var logMessage = $"Avg: {avg} s, Median: {median} s, Best: {best} s";
            Debug.Log(logMessage); // brak postępu po 10 pokoleniach, coś nie tak
            LogToFile(logMessage);

            // wygląda na to że działa ok
            var debugFirstNewGenotype = new List<float>();
            var mutatedCount = 0;
            var fromMother = 0;
            var fromFather = 0;
            for (int genotypeIndex = 0; genotypeIndex < geneticConfig.GenerationPopulation; genotypeIndex++)
            {
                var motherIndex = ChooseOneRandomlyWithWeights(matingPoolIndices);
                var fatherIndex = ChooseOneRandomlyWithWeights(matingPoolIndices, motherIndex);
                for (int stepIndex = 0; stepIndex < config.NumberOfScenarioSteps; stepIndex++)
                {
                    var shouldMutate = Random.Range(0f, 1f) < geneticConfig.MutationRate;
                    float stepDuration;
                    if (shouldMutate)
                    {
                        stepDuration = Random.Range(geneticConfig.MinimumStepDuration, geneticConfig.MaximumStepDuration);
                        mutatedCount++;
                        debugFirstNewGenotype.Add(stepDuration);
                    }
                    else
                    {
                        int genotypeId;
                        var inheritFromMother = Random.Range(0, 2) == 0;
                        var parentIndex = inheritFromMother ? motherIndex : fatherIndex;
                        var _ = inheritFromMother ? fromMother++ : fromFather++;
                        var parentGenotypeId = genotypeId = Genotypes.GenotypeIds[parentIndex];
                        stepDuration = GetStepDuration(parentGenotypeId, stepIndex);
                        if (genotypeIndex == 0)
                        {
                            debugFirstNewGenotype.Add(stepDuration);
                        }
                    }
                    PostUpdateCommands.CreateEntity(); // encja kroku scenariusza
                    PostUpdateCommands.AddComponent(new ScenarioStep { StepId = stepIndex, GenotypeId = genotypeIndex, DurationInS = stepDuration });
                }
                PostUpdateCommands.CreateEntity(); // encja genotypu
                PostUpdateCommands.AddComponent(new GenotypeId(genotypeIndex));
                if (genotypeIndex == 0) // odpalenie pierwszego genotypu pokolenia
                {
                    PostUpdateCommands.AddComponent(new CurrentlySimulated());
                }
            }
            PostUpdateCommands.CreateEntity(); // encja pokolenia
            PostUpdateCommands.AddComponent(new CurrentGeneration(previousGenerationId + 1));
            Debug.Log($"Starting generation {previousGenerationId + 1}");
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
    void LogToFile(string message)
    {
        File.AppendAllText(Path.Combine(Application.persistentDataPath, "logs", "evolutionLog.txt"), message + System.Environment.NewLine);
    }

    struct GenotypeNormalizedFitness
    {
        public int Index;
        public float NormalizedFitness;
    }
}
