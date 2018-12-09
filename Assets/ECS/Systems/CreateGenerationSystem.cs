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
    { // póki co tworzy tylko pierwsze pokolenie
        if (Start.Length == 0)
            return;
        PostUpdateCommands.AddComponent(Start.Entities[0], new StartSystemState());
        Debug.Log("Should log once");
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
                PostUpdateCommands.CreateEntity(); // encja kroku scenariusza
                PostUpdateCommands.AddComponent(new ScenarioStep { StepId = j, GenotypeId = i, DurationInS = randomStepDuration} );
            }
            PostUpdateCommands.CreateEntity(); // encja genotypu
            PostUpdateCommands.AddComponent(new GenotypeId(i));
            if (i == 0) // odpalenie pierwszego genotypu pokolenia
            {
                PostUpdateCommands.AddComponent(new CurrentlySimulated());
            }
        }
        PostUpdateCommands.CreateEntity();
        PostUpdateCommands.AddComponent(new CurrentGeneration(0));
    }

}
