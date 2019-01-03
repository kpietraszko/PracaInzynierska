using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct GeneticConfig : IComponentData
{
    public int NumberOfGenerations;
    public int GenerationPopulation;
    public float MinimumStepDuration;
    public float MaximumStepDuration;
}
