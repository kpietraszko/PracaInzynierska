using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Assertions;

[UpdateAfter(typeof(UnityEngine.Experimental.PlayerLoop.FixedUpdate))]
public class CreateGenerationSystem : ComponentSystem
{
    struct StartData
    {
        public readonly int Length;
        public ComponentDataArray<Start> Starts;
        public SubtractiveComponent<StartSystemState> Started;
        public EntityArray Entities;
    }
    struct ConfigData
    {
        public readonly int Length;
        public ComponentDataArray<Config> Configs;
    }
    struct StartSystemState : ISystemStateComponentData { }

    [Inject] StartData Start;
    [Inject] ConfigData Config;

    protected override void OnUpdate()
    {
        if (Start.Length == 0)
            return;
        PostUpdateCommands.AddComponent(Start.Entities[0], new StartSystemState());
        Debug.Log("Should log once"); // Log in several more times.
        Assert.IsFalse(Config.Length == 0);
        var config = Config.Configs[0];
        var generationPopulation = config.GenerationPopulation;
        var stepsInScenario = config.NumberOfScenarioSteps;
        var minStepDuration = config.MinimumStepDuration;
        var maxStepDuration = config.MaximumStepDuration;

        for (int i = 0; i < generationPopulation; i++)
        {
            for (int j = 0; j < stepsInScenario; j++)
            {
                var randomStepDuration = Random.Range(minStepDuration, maxStepDuration);
                Debug.Log("randomStepDuration = " + randomStepDuration);
                PostUpdateCommands.CreateEntity(); // encja kroku scenariusza
                PostUpdateCommands.AddComponent(new ScenarioStepId(j));
                PostUpdateCommands.AddComponent(new ScenarioStepDuration(randomStepDuration));
            }
            PostUpdateCommands.CreateEntity(); // encja genotypu
            PostUpdateCommands.AddComponent(new GenotypeId(i));
            if (i == 0) // odpalenie pierwszego genotypu pierwszego pokolenia
            {
                PostUpdateCommands.AddComponent(new CurrentlySimulated());
            }
        }
    }

}
