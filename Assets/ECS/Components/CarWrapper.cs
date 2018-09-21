using System;
using Unity.Entities;

public struct Car : IComponentData { }
[Serializable]
public class CarWrapper : ComponentDataWrapper<Car> { }