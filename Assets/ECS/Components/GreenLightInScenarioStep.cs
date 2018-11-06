using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[InternalBufferCapacity(8)]
public struct GreenLightInScenarioStep : IBufferElementData
{
    public static implicit operator int(GreenLightInScenarioStep l) => l.LightIndex;
    public static implicit operator GreenLightInScenarioStep(int l) => new GreenLightInScenarioStep { LightIndex = l};
    public int LightIndex;
}
