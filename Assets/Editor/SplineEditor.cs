using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

[CustomEditor(typeof(Spline))]
public class SplineEditor : Editor
{
	void OnEnable()
	{
		SceneView.onSceneGUIDelegate += OnSceneGUI;
	}
	void OnDisable()
	{
		SceneView.onSceneGUIDelegate -= OnSceneGUI;
	}
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();
	}
	void OnSceneGUI(SceneView sceneView)
	{
		var spline = Selection.activeObject as Spline;
		if (spline == null)
		{
			return;
		}
		int segments = 10 * spline.ControlPointCount;
		if (spline != null && spline.ControlPointCount >= 3)
		{
			float t = 0f;
			var points = new List<Vector3>(segments);
			for (int i = 0; i < segments; i++)
			{
				points.Add(spline.GetPointOnSpline(t));
				t += 1f / segments;
			}
			points.Add(spline[spline.ControlPointCount - 1]); //ostatni
            var initColor = Handles.color;
            Handles.color = Color.green;
			Handles.DrawAAPolyLine(points.ToArray());
            Handles.color = initColor;
            var trafficLightDrawPosition = spline.GetPointOnSpline(Mathf.Clamp01(spline.TrafficLights[0]));
            //Handles.DrawSolidDisc(trafficLightDrawPosition, Vector3.up, 1f);
			for (int i = 0; i < spline.ControlPointCount; i++)
			{
				EditorGUI.BeginChangeCheck();
				var pos = Handles.DoPositionHandle(spline[i], Quaternion.identity);
				if (EditorGUI.EndChangeCheck())
				{
					Undo.RecordObject(spline, "Move control point");
					EditorUtility.SetDirty(spline);
					spline[i] = pos;
				}
			}
		}
	}

}
