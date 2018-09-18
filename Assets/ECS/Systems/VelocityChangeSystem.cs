using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

//może ustalić execution order
public class VelocityChangeSystem : ComponentSystem
{
	struct Data
	{
		public readonly int Length;
		public ComponentDataArray<Acceleration> Accelerations;
		public ComponentDataArray<Velocity> Velocities;
		public EntityArray Entities;
	}
	[Inject] Data Moveables;
	protected override void OnUpdate()
	{
		for (int i = 0; i < Moveables.Length; i++)
		{
			float newVelocity = Moveables.Velocities[i] + Moveables.Accelerations[i] * Time.deltaTime; // TODO: sprawdzić czy ma być tu dt
			PostUpdateCommands.SetComponent(Moveables.Entities[i], new Velocity(newVelocity));
		}
	}
}
