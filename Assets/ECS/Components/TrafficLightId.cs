using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct TrafficLightId : IComponentData
{
    public TrafficLightId(int value)
    {
        Value = value;
    }
	public int Value;
	public static implicit operator int(TrafficLightId id)
	{
		return id.Value;
	}
}
public class TraficLightIdWrapper : ComponentDataWrapper<TrafficLightId> { }
