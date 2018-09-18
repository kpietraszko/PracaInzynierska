using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObject/Curve")]
public class Spline : ScriptableObject
{
	[SerializeField]
	Vector2[] ControlPoints;
	[SerializeField]
	float[] _TrafficLights;
	public float[] TrafficLights { get { return _TrafficLights; } }
	public Vector3 this[int index]
	{
		get => new Vector3(ControlPoints[index].x, 0f, ControlPoints[index].y);
		set { ControlPoints[index] = new Vector2(value.x, value.z); }
	}
	public int ControlPointCount => ControlPoints.Length;

	void OnValidate()
	{
		if (ControlPoints.Length < 3)
		{
			var newArray = new Vector2[3];
			Array.Copy(ControlPoints, newArray, ControlPoints.Length);
			ControlPoints = newArray;
		}
		if (ControlPoints.Length % 2 == 0)
		{
			var newArray = new Vector2[ControlPoints.Length+1];
			Array.Copy(ControlPoints, newArray, ControlPoints.Length);
			ControlPoints = newArray;
		}
	}
	public Vector3 GetPointOnSpline(float t)
	{
		var numOfCurves = ControlPointCount / 2;
		int currentCurveStartIndex = (int)(t * numOfCurves) * 2;
		t = frac(t * numOfCurves);
		return Vector3.Lerp(Vector3.Lerp(this[currentCurveStartIndex], this[currentCurveStartIndex + 1], t),
			Vector3.Lerp(this[currentCurveStartIndex + 1], this[currentCurveStartIndex + 2], t), t);
	}
}
