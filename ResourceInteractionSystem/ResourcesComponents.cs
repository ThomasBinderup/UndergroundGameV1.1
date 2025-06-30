using System;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;

// All pickable resources must have this component
public struct PickableResource : IComponentData
{
    public ResourceId ResourceId { get; set; }
    public ResourceTypeId ResourceTypeId { get; set; }
}

// The resource component is stored in a buffer, so that all resources can be accessed using a singleton
public struct Resource : IBufferElementData
{
    public ResourceId ResourceId { get; set; }
    public ResourceTypeId ResourceTypeId { get; set; }
    public float WorldPositionX { get; set; }
    public float WorldPositionY { get; set; }
    public float WorldPositionZ { get; set; }
    public float RotationX { get; set; }
    public float RotationY { get; set; }
    public float RotationZ { get; set; }
    public float RotationW { get; set; }
    public float Scale { get; set; }
    public bool IsActive { get; set; }
    public long DestroyedAt { get; set; } // in seconds (ServerAge)
    public float RespawnTime_Seconds { get; set; }
}
