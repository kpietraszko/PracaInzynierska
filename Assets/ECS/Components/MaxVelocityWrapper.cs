using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[Serializable]
public struct MaxVelocity : IComponentData
{
	public float Value;
	public MaxVelocity(float value)
	{
		Value = value;
	}
	public static implicit operator float(MaxVelocity id)
	{
		return id.Value;
	}
}
public class MaxVelocityWrapper : ComponentDataWrapper<MaxVelocity> { }

