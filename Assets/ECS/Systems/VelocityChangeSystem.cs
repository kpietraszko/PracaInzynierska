using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using static Unity.Mathematics.math;

[UpdateAfter(typeof(UnityEngine.Experimental.PlayerLoop.FixedUpdate))]
[UpdateBefore(typeof(SplineFollowSystem))]
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
			float newVelocity = Moveables.Velocities[i] + Moveables.Accelerations[i] * (1/30f)/*Time.fixedDeltaTime*/; // TODO: sprawdzić czy ma być tu dt
            newVelocity = max(newVelocity, 0f);
			PostUpdateCommands.SetComponent(Moveables.Entities[i], new Velocity(newVelocity));
		}
	}
}
