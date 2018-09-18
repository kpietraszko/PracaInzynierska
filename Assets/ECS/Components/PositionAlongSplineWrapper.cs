using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct PositionAlongSpline : IComponentData
{
	public float Value;
	public static implicit operator float(PositionAlongSpline position)
	{
		return position.Value;
	}
}
public class PositionAlongSplineWrapper : ComponentDataWrapper<PositionAlongSpline> { }
