using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine.Serialization;

[GhostComponent] // "GhostComponents are synchronized from the server to the clients"
public struct FirstPersonPlayer : IComponentData
{
    [GhostField]
    public Entity ControlledCharacter;
    [FormerlySerializedAs("LookRotationSpeed")] public float LookInputSensitivity;
}

[Serializable]
public struct FirstPersonPlayerInputs : IInputComponentData
{
    public float2 MoveInput;
    public float2 LookInput;
    public InputEvent JumpPressed;
}

[Serializable]
[GhostComponent(SendTypeOptimization = GhostSendType.OnlyPredictedClients)]
public struct FirstPersonPlayerNetworkInput : IComponentData
{
    [GhostField()]
    public float2 LastProcessedLookInput;
}