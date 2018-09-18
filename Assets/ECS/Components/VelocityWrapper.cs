using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct Velocity : IComponentData
{
	public float Value;
	public Velocity(float value)
	{
		Value = value;
	}
	public static implicit operator float(Velocity id) => id.Value;// żeby nie pisać wszędzie velocity.Value
}
public class VelocityWrapper : ComponentDataWrapper<Velocity> { }

