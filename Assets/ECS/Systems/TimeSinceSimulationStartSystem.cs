using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Assertions;
using static Unity.Mathematics.math;

[UpdateAfter(typeof(UnityEngine.Experimental.PlayerLoop.FixedUpdate))]
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
        Assert.IsTrue(TimeSinceSimulationStart.Length == 1);
        var timeSinceSimStart = TimeSinceSimulationStart.TimeSinceSimulationStart[0];
        //if (Start.Length > 0)
        //{
        //    timeSinceSimStart = 0f;
        //}
        var timeToAdd = 1/30f;//min(Time.fixedUnscaledDeltaTime, Time.maximumDeltaTime);
        float timeSinceSimStartSec = timeSinceSimStart.Seconds + timeToAdd; // chyba ok
        var stepNumber = timeSinceSimStart.StepNumber + 1;
        TimeSinceSimulationStart.TimeSinceSimulationStart[0] = new TimeSinceSimulationStart(timeSinceSimStartSec, stepNumber);
        //Debug.Log(1/30f / Time.fixedUnscaledDeltaTime + "x");
    }
}
