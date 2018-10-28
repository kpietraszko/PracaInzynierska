using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using static Unity.Mathematics.math;

//grupa Accelerating i Decelerating - ustawić acceleration zależnie od prędkości, przy Accelerating przyspieszenie najwyzsze dla v=0 i rowne 0 dla v=vmax
[UpdateBefore(typeof(VelocityChangeSystem))]
public class AccelerationChangeSystem : ComponentSystem
{
	struct AcceleratingData
	{
		public readonly int Length;
		public ComponentDataArray<Accelerating> Acceleratings;
		public ComponentDataArray<Acceleration> Accelerations;
		public ComponentDataArray<Velocity> Velocities;
		public ComponentDataArray<MaxVelocity> MaxVelocities;
		public EntityArray Entities;
	}
	struct DeceleratingData
	{
		public readonly int Length;
		public ComponentDataArray<Decelerating> Deceleratings;
		public ComponentDataArray<Acceleration> Accelerations;
		public ComponentDataArray<Velocity> Velocities;
		public ComponentDataArray<MaxVelocity> MaxVelocities;
		public EntityArray Entities;
	}
	[Inject] AcceleratingData Accelerating;
	[Inject] DeceleratingData Decelerating;
	protected override void OnUpdate()
	{
		const float peakAcceleration = 30f; //TODO: przenieść do komponentu
		float maxVelocity = 20f; //TODO: jw.
		float breakingAcceleration = -15f; //TODO: jw.
		for (int i = 0; i < Accelerating.Length; i++)
		{
			float velocityToMaxVelocityRatio = Accelerating.Velocities[i] / maxVelocity;//Accelerating.MaxVelocities[i];
			float acceleration = (1 - velocityToMaxVelocityRatio) * peakAcceleration; //max jeśli stoi, 0 jeśli jedzie z maksymalną v
			PostUpdateCommands.SetComponent(Accelerating.Entities[i], new Acceleration(acceleration));
			if (abs(acceleration) < 0.00001)
			{
				PostUpdateCommands.RemoveComponent<Accelerating>(Accelerating.Entities[i]);
			}
		}
		for (int i = 0; i < Decelerating.Length; i++)
		{
			float acceleration =  abs(Decelerating.Velocities[i]) > 0.00001 ? breakingAcceleration : 0f;
			PostUpdateCommands.SetComponent(Decelerating.Entities[i], new Acceleration(acceleration));
			//if (abs(acceleration) < 0.00001)
			//{
			//	PostUpdateCommands.RemoveComponent<Decelerating>(Decelerating.Entities[i]);
			//}
		}
	}
}
