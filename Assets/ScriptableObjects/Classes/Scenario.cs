using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObject/Scenario")]
public class Scenario : ScriptableObject
{
    [SerializeField]
    public ScenarioStep[] ScenarioSteps;
}

[Serializable]
public class ScenarioStep
{
    [SerializeField]
    public TrafficLightDir[] GreenLights;
    public enum TrafficLightDir
    {
        BottomForwardOrRight = 1,
        BottomLeft = 2,
        RightForwardOrRight = 4,
        RightLeft = 5,
        TopForwardOrRight = 7,
        TopLeft = 8,
        LeftForwardOrRight = 10,
        LeftLeft = 11
    }
}