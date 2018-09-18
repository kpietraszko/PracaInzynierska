using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct Accelerating : IComponentData
{
}
public class AcceleratingWrapper : ComponentDataWrapper<Accelerating> { } //TODO: usunąć
