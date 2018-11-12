using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Assertions;

public class TimeSinceSimulationStartSystem : ComponentSystem
{
    struct TimeSinceSimulationStartData
    {
        public readonly int Length;
        public ComponentDataArray<TimeSinceSimulationStart> TimeSinceSimulationStart;
    }
    struct StartData
    {
        public readonly int Length;
        public ComponentDataArray<Start> Start;
    }
    [Inject] TimeSinceSimulationStartData TimeSinceSimulationStart;
    [Inject] StartData Start;

    protected override void OnUpdate()
    {
        Assert.IsFalse(TimeSinceSimulationStart.Length == 0);
        float timeSinceSimStart = TimeSinceSimulationStart.TimeSinceSimulationStart[0];
        if (Start.Length > 0)
        {
            timeSinceSimStart = 0f;
        }
        timeSinceSimStart += Time.fixedDeltaTime;
        TimeSinceSimulationStart.TimeSinceSimulationStart[0] = new TimeSinceSimulationStart { Value = timeSinceSimStart };
    }
}
