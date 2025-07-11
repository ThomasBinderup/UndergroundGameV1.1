using Rukhanka;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.VisualScripting;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
partial struct testSystem_client : ISystem
{
    bool runOnce;
    BufferLookup<AnimatorControllerParameterComponent> paramBufLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        paramBufLookup = state.GetBufferLookup<AnimatorControllerParameterComponent>();
        state.RequireForUpdate<MiscPrefabs>();  
    }

    // [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        FastAnimatorParameter runSpeed = new FastAnimatorParameter("RunSpeed");

        paramBufLookup.Update(ref state);
        // var ecb = new EntityCommandBuffer(Allocator.Temp);
        if (!runOnce)
        {
            runOnce = true;
            var miscPrefabs = SystemAPI.GetSingleton<MiscPrefabs>();
            var e = state.EntityManager.Instantiate(miscPrefabs.TestAnimChar);
            SystemAPI.SetComponent(e, new GhostOwner
            {
                NetworkId = 1
            });

            if (state.EntityManager.HasComponent<AnimatorControllerParameterIndexTableComponent>(e))
            {
            var acpit = SystemAPI.GetComponent<AnimatorControllerParameterIndexTableComponent>(e);
            paramBufLookup.TryGetBuffer(e, out var acpc);
            runSpeed.SetRuntimeParameterData(acpit.value, acpc, new ParameterValue() { floatValue = 2 });
            }
        }
        
        
        Debug.Log("yeah logs");
        
        
        

        //ecb.Playback(state.EntityManager);
       // ecb.Dispose();
        
        
        

        
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}