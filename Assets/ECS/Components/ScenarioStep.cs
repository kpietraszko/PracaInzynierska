using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct ScenarioStep : IComponentData, IComparable<ScenarioStep>
{
	public float DurationInS;
    public int StepId;
    public int GenotypeId;
	public static implicit operator float(ScenarioStep duration)
	{
		return duration.DurationInS;
	}

    public int CompareTo(ScenarioStep that)
    {
        return this.DurationInS.CompareTo(that.DurationInS);
    }
}
