using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;
using System;

[Serializable]
public struct Position2D : IComponentData
{
	public float2 Value;

    public Position2D(float2 value)
    {
        Value = value;
    }
    public Position2D(float x, float y)
    {
        Value = new float2(x, y);
    }

	public static implicit operator float2(Position2D position)
	{
		return position.Value;
	}
    public static implicit operator Position2D(float2 position)
    {
        return new Position2D { Value = position };
    }
}
public class Position2DWrapper : ComponentDataWrapper<Position2D> { }
