using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[Serializable]
public struct SplineFollow : IComponentData
{
}
public class SplineFollowWrapper : ComponentDataWrapper<SplineFollow> { }
