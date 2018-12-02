using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct TimeSinceSimulationStart : IComponentData
{
	public float Value;
    public long StepNumber;
    public TimeSinceSimulationStart(float value, long stepNumber)
    {
        Value = value;
        StepNumber = stepNumber;
    }
	public static implicit operator float(TimeSinceSimulationStart id)
	{
		return id.Value;
	}
}
