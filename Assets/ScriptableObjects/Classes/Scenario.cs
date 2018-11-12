using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

[CreateAssetMenu(menuName = "ScriptableObject/Scenario")]
public class Scenario : ScriptableObject
{
    [InfoBox(LightIndexInfo)]
    [SerializeField]
    public ScenarioStep[] ScenarioSteps;

    const string LightIndexInfo = 
@"0 - Bottom forward //
1 - Bottom left //
2 - Right forward //
3 - Right left //
4 - Top forward
5 - Top left
6 - Left right //
7 - Left forward";
}

[Serializable]
public class ScenarioStep
{
    [SerializeField]
    public int[] GreenLights;
}