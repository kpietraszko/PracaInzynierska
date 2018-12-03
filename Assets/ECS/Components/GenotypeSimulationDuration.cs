using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct GenotypeSimulationDuration : IComponentData
{
    public GenotypeSimulationDuration(float duration)
    {
        Value = duration;
    }
    public float Value;
    public static implicit operator float(GenotypeSimulationDuration duration)
    {
        return duration.Value;
    }
}
