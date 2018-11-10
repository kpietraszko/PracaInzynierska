using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct Heading : IComponentData
{
	public float Value;
	public static implicit operator float(Heading heading)
	{
		return heading.Value;
	}
}
