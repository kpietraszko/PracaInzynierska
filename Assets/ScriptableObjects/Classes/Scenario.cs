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
@"0 - Bottom right
1 - Bottom left";
}

[Serializable]
public class ScenarioStep
{
    [SerializeField]
    public int[] GreenLights;
}