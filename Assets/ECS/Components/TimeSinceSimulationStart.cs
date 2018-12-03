using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct TimeSinceSimulationStart : IComponentData
{
	public float Seconds;
    public long StepNumber;
    public TimeSinceSimulationStart(float value, long stepNumber)
    {
        Seconds = value;
        StepNumber = stepNumber;
    }
}
