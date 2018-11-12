using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct ScenarioStepId : IComponentData, IComparable<ScenarioStepId>
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

    public int CompareTo(ScenarioStepId that)
    {
        return this.Value.CompareTo(that.Value);
    }
}
