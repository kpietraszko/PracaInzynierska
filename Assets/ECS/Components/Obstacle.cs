using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct Obstacle : IComponentData
{
	public int CurveId;
	public float PositionAlongCurve;
	public float2 Position;
}
