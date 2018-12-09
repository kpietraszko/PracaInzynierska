using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct CurrentGeneration : IComponentData
{
    public int Value;

    public CurrentGeneration(int value)
    {
        Value = value;
    }

    public static implicit operator int(CurrentGeneration generation)
    {
        return generation.Value;
    }
}
