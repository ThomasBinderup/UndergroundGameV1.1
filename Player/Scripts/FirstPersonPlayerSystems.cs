using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Unity.CharacterController;
using Unity.NetCode;
using UnityEngine.InputSystem;
// this system is executing inside GhostInputSystemGroup and therefore is responsible for listening to player inputs
[UpdateInGroup(typeof(GhostInputSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class FirstPersonPlayerInputsSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<InventoryClosed>();
        RequireForUpdate<NetworkTime>();
        RequireForUpdate(SystemAPI.QueryBuilder().WithAll<FirstPersonPlayer, FirstPersonPlayerInputs>().Build());
    }
    
    protected override void OnUpdate()
    {
        foreach (var (playerInputs, player) in SystemAPI.Query<RefRW<FirstPersonPlayerInputs>, FirstPersonPlayer>().WithAll<GhostOwnerIsLocal>())
        {
            playerInputs.ValueRW.MoveInput = new float2
            {
                x = (Keyboard.current.dKey.isPressed ? 1f : 0f) + (Keyboard.current.aKey.isPressed ? -1f : 0f),
                y = (Keyboard.current.wKey.isPressed ? 1f : 0f) + (Keyboard.current.sKey.isPressed ? -1f : 0f),
            };
            
            InputDeltaUtilities.AddInputDelta(ref playerInputs.ValueRW.LookInput, Mouse.current.delta.ReadValue());

            playerInputs.ValueRW.JumpPressed = default;
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                playerInputs.ValueRW.JumpPressed.Set();
            }
        }
    }
}

/// <summary>
/// Apply inputs that need to be read at a variable rate
/// </summary>
/* 
Why does jitter happen when rotation is updated in fixed step?
Even if fixed update is running at 60 Hz, your rendering frame rate is often much higher (e.g. 120 Hz or more). Here's what happens:
Scenario:
Fixed step group (like your rotation logic in FirstPersonPlayerFixedStepControlSystem) runs once every 1/60 second.
Your screen renders at 1/120 second (or even higher).
So: two rendered frames will show the same rotation value, because it hasn't updated yet!
This causes visible jitter — the rotation stays frozen for a couple frames, then jumps, which looks choppy.
Variable rate system smooths this out
When you put rotation input (like mouse movement) in a variable rate system, here's what changes:
It runs every frame, or more precisely:
Every client-side simulation pass
Often once per rendered frame
You get finer-grained updates that match the framerate.
So your character’s rotation smoothly follows the mouse, without visible jumps.
*/
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[UpdateAfter(typeof(PredictedFixedStepSimulationSystemGroup))]
[BurstCompile]
public partial struct FirstPersonPlayerVariableStepControlSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkTime>();
        state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<FirstPersonPlayer, FirstPersonPlayerInputs>().Build());
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (playerInputs, playerNetworkInput, player) in SystemAPI.Query<FirstPersonPlayerInputs, RefRW<FirstPersonPlayerNetworkInput>, FirstPersonPlayer>().WithAll<Simulate>())
        {
            // Compute input deltas, compared to last known values
            float2 lookInputDelta = InputDeltaUtilities.GetInputDelta(
                playerInputs.LookInput, 
                playerNetworkInput.ValueRO.LastProcessedLookInput);
            playerNetworkInput.ValueRW.LastProcessedLookInput = playerInputs.LookInput;

            if (SystemAPI.HasComponent<FirstPersonCharacterControl>(player.ControlledCharacter))
            {
                FirstPersonCharacterControl characterControl = SystemAPI.GetComponent<FirstPersonCharacterControl>(player.ControlledCharacter);
                characterControl.LookDegreesDelta = lookInputDelta;
                SystemAPI.SetComponent(player.ControlledCharacter, characterControl);
            }
        }
    }
}

/// <summary>
/// Apply inputs that need to be read at a fixed rate.
/// It is necessary to handle this as part of the fixed step group, in case your framerate is lower than the fixed step rate.
/// </summary>
/// my explanation to use this is for a combination of staying in sync with unity physics and get access to latest physics updates
/// Physics updates on FixedUpdate, so for accurate physics
[UpdateInGroup(typeof(PredictedFixedStepSimulationSystemGroup), OrderFirst = true)]
[BurstCompile]
public partial struct FirstPersonPlayerFixedStepControlSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkTime>();
        state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<FirstPersonPlayer, FirstPersonPlayerInputs>().Build());
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (playerInputs, player) in SystemAPI.Query<FirstPersonPlayerInputs, FirstPersonPlayer>().WithAll<Simulate>())
        {
            if (SystemAPI.HasComponent<FirstPersonCharacterControl>(player.ControlledCharacter))
            {
                FirstPersonCharacterControl characterControl = SystemAPI.GetComponent<FirstPersonCharacterControl>(player.ControlledCharacter);
                
                quaternion characterRotation = SystemAPI.GetComponent<LocalTransform>(player.ControlledCharacter).Rotation;

                // Move
                float3 characterForward = MathUtilities.GetForwardFromRotation(characterRotation);
                float3 characterRight = MathUtilities.GetRightFromRotation(characterRotation);
                characterControl.MoveVector = (playerInputs.MoveInput.y * characterForward) + (playerInputs.MoveInput.x * characterRight);
                characterControl.MoveVector = MathUtilities.ClampToMaxLength(characterControl.MoveVector, 1f);

                // Jump
                characterControl.Jump = playerInputs.JumpPressed.IsSet;
            
                SystemAPI.SetComponent(player.ControlledCharacter, characterControl);
            }
        }
    }
}