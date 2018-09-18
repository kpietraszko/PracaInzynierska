using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct Obstacle : IComponentData
{
	public int CurveId;
	public float PositionAlongCurve;
	public Vector2 Position;
}
