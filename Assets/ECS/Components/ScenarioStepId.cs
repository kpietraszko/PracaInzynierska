using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct ScenarioStepId : IComponentData
{
    public ScenarioStepId(int value)
    {
        Value = value;
    }
	public int Value;
	public static implicit operator int(ScenarioStepId id)
	{
		return id.Value;
	}
}
