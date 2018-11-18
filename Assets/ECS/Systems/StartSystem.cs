using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

[UpdateAfter(typeof(UnityEngine.Experimental.PlayerLoop.FixedUpdate))]
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
        if (Start.Length == 0)
            return;
        NativeLeakDetection.Mode = NativeLeakDetectionMode.Disabled;
        PostUpdateCommands.RemoveComponent(Start.Entities[0], typeof(Start));
        //tu rzeczy wykonywane raz przy uruchomieniu
        PostUpdateCommands.CreateEntity(EntityManager.CreateArchetype(typeof(TimeSinceSimulationStart)));
    }
}
