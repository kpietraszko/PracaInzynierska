using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct GenotypeId : IComponentData
{
	public int Value;

    public GenotypeId(int value)
    {
        Value = value;
    }

	public static implicit operator int(GenotypeId id)
	{
		return id.Value;
	}
}
