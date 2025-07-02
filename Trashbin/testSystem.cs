using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
partial struct testSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<TestRpc>();
    }

    // [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        MiscPrefabs miscPrefabs = SystemAPI.GetSingleton<MiscPrefabs>();
        Debug.Log("testRPC");
        var e = new EntityCommandBuffer(Allocator.Temp);
        foreach (var (testRpc, receive, entity) in SystemAPI.Query<RefRO<TestRpc>, RefRO<ReceiveRpcCommandRequest>>().WithEntityAccess())
        {
            var ent = e.Instantiate(miscPrefabs.InventoryItemGhostPrefab);
            e.AddComponent(ent, new InventoryItem
            {
                ItemTypeId = (ItemTypeId)1001,
                CurrentIndexSlot = 1,
                Quantity = 1,
                ItemId = (ItemId)200
            });

            e.AddComponent(ent, new testComp
            {
                test = 1,
            });

            Entity networkConnectionEnt = receive.ValueRO.SourceConnection;
            Debug.Log("networkConnectionEnt: " + networkConnectionEnt);

            if (!SystemAPI.HasComponent<NetworkId>(networkConnectionEnt)) return;
            NetworkId networkId = SystemAPI.GetComponent<NetworkId>(networkConnectionEnt);
            int networkIdValue = networkId.Value;
            Debug.Log("networkIdValue: " + networkIdValue);
            e.AddComponent(ent, new GhostOwner
            {
                NetworkId = networkIdValue
            });

            e.DestroyEntity(entity);
        }
        Debug.Log("tester");

        


        e.Playback(state.EntityManager);
        e.Dispose();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
partial struct testSystem_client : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<InventoryItem>();
    }

    // [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        
        

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}

[GhostComponent]
public struct testComp : IComponentData
{
    [GhostField]
    public int test;
}

