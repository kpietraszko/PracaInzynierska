using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct SplineId : ISharedComponentData
{
	public int Value;
	public SplineId(int value)
	{
		Value = value;
	}
	public static implicit operator int(SplineId id)
	{
		return id.Value;
	}
}
public class SplineIdWrapper : SharedComponentDataWrapper<SplineId> { }
