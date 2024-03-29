﻿using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public struct Obstacle : IComponentData
{
	public int SplineId;
    public float PositionAlongSpline;
	public float2 Position;
}
public class ObstacleWrapper : ComponentDataWrapper<Obstacle> { }
