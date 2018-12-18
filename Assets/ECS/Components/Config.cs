using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct Config : IComponentData
{
    public int CarsToSpawnPerSpline;
    public int NumberOfSplines;
    public int NumberOfScenarioSteps;
}
