using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct ScenarioStepStartTime : IComponentData
{
    /// <param name="startTimeInMs">Time (in ms) at which scenario step will start</param>
    public ScenarioStepStartTime(long startTimeInMs)
    {
        StartTimeInMs = startTimeInMs;
    }
	public long StartTimeInMs;
	public static implicit operator long(ScenarioStepStartTime startTime)
	{
		return startTime.StartTimeInMs;
	}
}
