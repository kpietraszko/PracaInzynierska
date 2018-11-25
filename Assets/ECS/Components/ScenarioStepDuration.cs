using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct ScenarioStepDuration : IComponentData, IComparable<ScenarioStepDuration>
{
    /// <param name="durationInS">Time (in s) scenario step will take</param>
    public ScenarioStepDuration(float durationInS)
    {
        DurationInS = durationInS;
    }
	public float DurationInS;
	public static implicit operator float(ScenarioStepDuration duration)
	{
		return duration.DurationInS;
	}

    public int CompareTo(ScenarioStepDuration that)
    {
        return this.DurationInS.CompareTo(that.DurationInS);
    }
}
