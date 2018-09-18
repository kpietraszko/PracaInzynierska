using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct ControlPointId : IComponentData
{
	public int Value;
	public static implicit operator int(ControlPointId id)
	{
		return id.Value;
	}
}
