using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class StartSystem : ComponentSystem
{
	struct StartData
	{
		public ComponentDataArray<Start> Starts;
		public EntityArray Entities;
		public readonly int Length;
	}
	[Inject] StartData Start;
	protected override void OnUpdate()
	{
		if (Start.Length > 0)
		{
			PostUpdateCommands.RemoveComponent(Start.Entities[0], typeof(Start));
			//tu rzeczy wykonywane raz przy uruchomieniu
		}
	}
}
