using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[Serializable]
public struct Acceleration : IComponentData
{
	public float Value;
	public Acceleration(float value)
	{
		Value = value;
	}
	public static implicit operator float(Acceleration id)
	{
		return id.Value;
	}
}
public class AccelerationWrapper : ComponentDataWrapper<Acceleration> { }

