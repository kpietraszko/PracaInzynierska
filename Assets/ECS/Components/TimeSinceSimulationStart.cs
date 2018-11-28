using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct TimeSinceSimulationStart : IComponentData
{
	public float Value;
    public TimeSinceSimulationStart(float value)
    {
        Value = value;
    }
	public static implicit operator float(TimeSinceSimulationStart id)
	{
		return id.Value;
	}
}
