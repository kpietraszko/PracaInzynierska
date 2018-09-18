using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct SplineFollow : IComponentData
{
}
public class SplineFollowWrapper : ComponentDataWrapper<SplineFollow> { }
