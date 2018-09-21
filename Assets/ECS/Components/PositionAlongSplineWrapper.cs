using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[Serializable]
public struct PositionAlongSpline : IComponentData
{
	public float Value;
    public PositionAlongSpline(float value)
    {
        Value = value;
    }
	public static implicit operator float(PositionAlongSpline position)
	{
		return position.Value;
	}
}
public class PositionAlongSplineWrapper : ComponentDataWrapper<PositionAlongSpline> { }
