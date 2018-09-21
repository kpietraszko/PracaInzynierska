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
    }
    struct UnusedCarsData
    {
        public ComponentDataArray<Car> Cars;
        public SubtractiveComponent<SplineId> SplineIds;
        public SubtractiveComponent<PositionAlongSpline> PositionsAlongSpline;
        public readonly int Length;
        public EntityArray Entities;
    }
    [Inject] StartData Start;
    [Inject] UnusedCarsData UnusedCars;
    protected override void OnUpdate()
    {
        PostUpdateCommands.RemoveComponent(Start.Entities[0], typeof(Start));
        //tu rzeczy wykonywane raz przy uruchomieniu
    }
}
