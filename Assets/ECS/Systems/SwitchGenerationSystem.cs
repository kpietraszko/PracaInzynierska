using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Assertions;
using static Unity.Mathematics.math;

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
    struct UiInfoData
    {
        [ReadOnly]
        public SharedComponentDataArray<UiInfo> UiInfos;
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
    [Inject] UiInfoData UiInfo;

    StreamWriter _performanceStream;
    float _previousBest;
    int stalledCount;

    protected override void OnCreateManager()
    {
        File.AppendAllText(Path.Combine(Application.persistentDataPath, "logs", "evolutionLog.csv"),
            $"{System.Environment.NewLine}{System.DateTime.Now.ToString("yyyy-MM-dd HH:mm")}{System.Environment.NewLine}" +
            $"Generation,Average,Median,Best{System.Environment.NewLine}");

        File.AppendAllText(Path.Combine(Application.persistentDataPath, "logs", "bestDurationsLog.csv"),
            $"{System.Environment.NewLine}{System.DateTime.Now.ToString("yyyy-MM-dd HH:mm")}{System.Environment.NewLine}" +
            $"Generation,Step 1,Step 2,Step 3,Step 4{System.Environment.NewLine}");
    }

    protected override void OnUpdate()
    {
        Assert.IsTrue(NewGeneration.Length <= 1);
        Assert.IsTrue(EndedGeneration.Length <= 1);
        Assert.AreEqual(Config.Length, 1);

        var config = Config.Configs[0];
        var geneticConfig = GeneticConfig.GeneticConfigs[0];
        var numberOfGenerations = GeneticConfig.GeneticConfigs[0].NumberOfGenerations;

        if (NewGeneration.Length > 0)
        {
            int newGenerationId = NewGeneration.CurrentGenerations[0];
            PostUpdateCommands.AddComponent(NewGeneration.Entities[0], new CurrentGenerationSystemState { GenerationId = newGenerationId });
        }

        if (EndedGeneration.Length > 0)
        {
            Assert.AreEqual(Genotypes.Length, geneticConfig.GenerationPopulation);

            DeleteAllScenarioSteps();

            var previousGenerationId = EndedGeneration.SystemStates[0].GenerationId;
            PostUpdateCommands.RemoveComponent<CurrentGenerationSystemState>(EndedGeneration.Entities[0]);

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
            var bestIndex = 0;
            for (int genotypeIndex = 0; genotypeIndex < Genotypes.Length; genotypeIndex++)
            {
                durations[genotypeIndex] = Genotypes.GenotypesSimulationDurations[genotypeIndex];
                // dostosowanie to odwrotnosc czasu trwania symulacji
                var fitness = maxDuration - Genotypes.GenotypesSimulationDurations[genotypeIndex];
                if (fitness > fitnesses[bestIndex])
                {
                    bestIndex = genotypeIndex;
                }
                fitnesses[genotypeIndex] = fitness;
                fitnessesSum += fitness;
            }
            for (int genotypeIndex = 0; genotypeIndex < Genotypes.Length; genotypeIndex++)
            {
                PostUpdateCommands.DestroyEntity(Genotypes.Entities[genotypeIndex]);
            }

            var avg = durations.Average();
            var orderedDurations = durations.OrderBy(x => x);
            var median = (orderedDurations.ElementAt(durations.Length / 2 - 1)
                + orderedDurations.ElementAt(durations.Length / 2)) / 2;
            float best = Genotypes.GenotypesSimulationDurations[bestIndex];
            var targetNumberOfGenerations = numberOfGenerations;

            // połączenie sinusoidy z sigmoid
            var mutationRate = (sin(previousGenerationId * (1 / ((targetNumberOfGenerations/1000f) + (previousGenerationId / 100)))) + 1) / 2f / 1f;

            var logMessage = $"{previousGenerationId},{avg},{median},{best}";
            Debug.Log(logMessage);
            LogToFile(logMessage, "evolutionLog.csv");

            var bestGenotypeStepsDurations = $"{previousGenerationId},";
            for (int stepIndex = 0; stepIndex < config.NumberOfScenarioSteps; stepIndex++)
            {
                bestGenotypeStepsDurations += GetStepDuration(Genotypes.GenotypeIds[bestIndex], stepIndex) + ",";
            }
            LogToFile(bestGenotypeStepsDurations, "bestDurationsLog.csv");

            if (previousGenerationId + 1 == numberOfGenerations)
            {
                float[] bestGenotypeScenarioStepsDuration = Enumerable.Range(0, config.NumberOfScenarioSteps)
                    .Select(x => GetStepDuration(Genotypes.GenotypeIds[bestIndex], x))
                    .ToArray();

                SetFinishUiInfo(best, numberOfGenerations, bestGenotypeScenarioStepsDuration);
            }
            else
            {
                var debugFirstNewGenotype = new List<float>();
                var intermediatePopulation = new List<KeyValuePair<int, float>>();
                // turnieje
                for (int genotypeIndex = 0; genotypeIndex < geneticConfig.GenerationPopulation * 2; genotypeIndex++)
                {
                    const int numberOfContestants = 3;
                    var contestantsIndices = Enumerable.Range(0, numberOfContestants).Select(_ => Random.Range(0, geneticConfig.GenerationPopulation)).ToArray();
                    var contestantsFitnesses = contestantsIndices.Select(i => new KeyValuePair<int, float>(i, fitnesses[i])).ToArray();
                    var winner = contestantsFitnesses.Aggregate((agg, next) => next.Value > agg.Value ? next : agg);
                    intermediatePopulation.Add(winner);
                }
                var matingPool = intermediatePopulation
                    .OrderByDescending(x => x.Value)
                    .Take(geneticConfig.GenerationPopulation)
                    .Select(x => x.Key)
                    .ToArray();

                for (int genotypeIndex = 0; genotypeIndex < geneticConfig.GenerationPopulation; genotypeIndex++)
                {
                    var shouldCrossover = Random.Range(0f, 1f) > mutationRate;
                    var shouldMutate = false;
                    int? crossoverWith = null;
                    if (shouldCrossover)
                    {
                        var selectableParents = Enumerable.Range(0, geneticConfig.GenerationPopulation).Where(x => x != genotypeIndex).ToArray();
                        crossoverWith = selectableParents.ElementAt(Random.Range(0, selectableParents.Length));
                    }
                    else
                    {
                        shouldMutate = true;
                    }

                    for (int stepIndex = 0; stepIndex < config.NumberOfScenarioSteps; stepIndex++)
                    {
                        var parentIndex = shouldCrossover ? new[]{ matingPool[genotypeIndex], matingPool[crossoverWith.Value]}.ElementAt(stepIndex % 2) 
                            : matingPool[genotypeIndex];
                        if (genotypeIndex == 0)
                        {
                            shouldMutate = false;
                            parentIndex = bestIndex;
                        }
                        float stepDuration;
                        stepDuration = GetStepDuration(Genotypes.GenotypeIds[parentIndex], stepIndex);
                        if (shouldMutate)
                        {
                            stepDuration += Random.Range(1f, 20f) * (Random.Range(0, 2) * 2 - 1f); //razy losowy znak -1 lub 1
                            stepDuration = clamp(stepDuration, geneticConfig.MinimumStepDuration, geneticConfig.MaximumStepDuration);
                        }
                        if (genotypeIndex == 0)
                        {
                            debugFirstNewGenotype.Add(stepDuration);
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
                var newGenerationId = previousGenerationId + 1;
                PostUpdateCommands.AddComponent(new CurrentGeneration(newGenerationId));
                SetCurrentGenerationUiInfo(newGenerationId + 1);
                SetPreviousGenerationUiInfo(best);
                Debug.Log($"Starting generation {newGenerationId}");
            }
        }
    }

    protected override void OnDestroyManager()
    {
        File.AppendAllText(Path.Combine(Application.persistentDataPath, "logs", "evolutionLog.csv"),
            $"Finished at {System.DateTime.Now.ToString("yyyy-MM-dd HH:mm")}{System.Environment.NewLine}");
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

    void DeleteAllScenarioSteps()
    {
        for (int i = 0; i < ScenarioSteps.Length; i++)
        {
            PostUpdateCommands.DestroyEntity(ScenarioSteps.Entities[i]);
        }
    }

    void LogToFile(string message, string fileName)
    {
        File.AppendAllText(Path.Combine(Application.persistentDataPath, "logs", fileName), message + System.Environment.NewLine);
    }

    void SetCurrentGenerationUiInfo(float generationNumber)
    {
        UiInfo.UiInfos[0].CurrentGenerationInfo.text = $"<b>Obecne pokolenie: {generationNumber}</b>";
    }

    void SetPreviousGenerationUiInfo(float bestDuration)
    {
        UiInfo.UiInfos[0].PrevGenerationInfo.enabled = true;
        UiInfo.UiInfos[0].PrevGenerationInfo.text = $"<b>Poprzednie pokolenie</b>\nCzas symulacji najlepszego genotypu: {bestDuration:F0} s";
    }

    void SetFinishUiInfo(float bestDuration, int numberOfGenerations, float[] scenarioStepsDurations)
    {
        var uiInfo = UiInfo.UiInfos[0];
        uiInfo.CurrentGenerationInfo.enabled = false;
        uiInfo.PrevGenerationInfo.enabled = false;
        uiInfo.FinishInfo.enabled = true;
        uiInfo.FinishScreen.SetActive(true);
        var text = $"Po <b>{numberOfGenerations}</b> pokoleniach czas symulacji najlepszego genotypu to {bestDuration:F0} s.\n\n\nCzasy trwania kroków scenariusza najlepszego genotypu:\n";

        for (int i = 0; i < scenarioStepsDurations.Length; i++)
        {
            text += $"Krok {i + 1}: {scenarioStepsDurations[i],6:F0} s\n";
        }
        uiInfo.FinishInfo.text = text;
    }

    struct GenotypeNormalizedFitness
    {
        public int Index;
        public float NormalizedFitness;
    }

    float Squared(float a) => a * a;
}
