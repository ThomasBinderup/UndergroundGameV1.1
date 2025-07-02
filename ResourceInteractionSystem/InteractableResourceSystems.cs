using System;
using System.Linq;
using System.Resources;
using Newtonsoft.Json.Linq;
using TMPro;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

// Systems responsible for handling resource actions RPC requests sent from clients

/// <summary>
/// System responsible for handling picking up pickable resources like small stones, mushrooms, sticks, pearls etc. by using hand. Players can simply
/// press "E" (default key) to pick up items in a certain radius without having to look directly at the item as long as it is within distance.
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
partial struct OnPickResource_RpcHandler : ISystem
{
    private ComponentLookup<LocalToWorld> localToWorldLookup;
    private ComponentLookup<CharacterBySourceConnection> characterBySourceConnectionLookup;
    private ComponentLookup<PickableResource> pickableResourceLookup;
    private ComponentLookup<InventorySlotTracker> inventorySlotTrackerLookup;
    private EntityQuery query1;
    private EntityQuery resourceQuery;
    private EntityQuery serverInfoQuery;
    private Unity.Mathematics.Random rng;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        localToWorldLookup = state.GetComponentLookup<LocalToWorld>(true);
        characterBySourceConnectionLookup = state.GetComponentLookup<CharacterBySourceConnection>(true);
        pickableResourceLookup = state.GetComponentLookup<PickableResource>(true);
        inventorySlotTrackerLookup = state.GetComponentLookup<InventorySlotTracker>(false);
        state.RequireForUpdate<PickResourceRpc>();
        state.RequireForUpdate<ReceiveRpcCommandRequest>();

        var componentTypes = new NativeArray<ComponentType>(1, Allocator.Temp)
        {
            [0] = ComponentType.ReadOnly<PickableResource_StaticData>()
        };

        query1 = state.GetEntityQuery(componentTypes);

        var resourceComponentType = new NativeArray<ComponentType>(1, Allocator.Temp)
        {
            [0] = ComponentType.ReadWrite<Resource>()
        };

        resourceQuery = state.GetEntityQuery(resourceComponentType);

        var serverInfoComponentType = new NativeArray<ComponentType>(1, Allocator.Temp)
        {
            [0] = ComponentType.ReadWrite<ServerInfo>()
        };
        serverInfoQuery = state.GetEntityQuery(serverInfoComponentType);

        rng = new Unity.Mathematics.Random(32823929);
    }

    // [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        state.CompleteDependency(); // no idea what this does but it fixes a bug
        characterBySourceConnectionLookup.Update(ref state);
        localToWorldLookup.Update(ref state);
        pickableResourceLookup.Update(ref state);
        inventorySlotTrackerLookup.Update(ref state);

        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        try
        {
            foreach ((RefRO<PickResourceRpc> pickResourceRpc, RefRO<ReceiveRpcCommandRequest> receiveRpcCommandRequest, Entity entity) in SystemAPI.Query<RefRO<PickResourceRpc>, RefRO<ReceiveRpcCommandRequest>>().WithEntityAccess())
            {
                ecb.DestroyEntity(entity);

                Entity networkConnectionEnt = receiveRpcCommandRequest.ValueRO.SourceConnection;

                if (!SystemAPI.HasComponent<NetworkId>(networkConnectionEnt)) return;
                NetworkId networkId = SystemAPI.GetComponent<NetworkId>(networkConnectionEnt);
                int networkIdValue = networkId.Value;

                if (!characterBySourceConnectionLookup.TryGetComponent(networkConnectionEnt, out var characterBySourceConnection)) return;

                Entity characterEnt = characterBySourceConnection.characterEntity;
                if (!TryGetClosestPickableResource(characterEnt, ref state, out Entity closestPickableResourceEnt)) return;
                AddPickableResourceToInventory(characterEnt, ref state, ref closestPickableResourceEnt, ref ecb, networkIdValue, query1);


            }

        }
        finally
        {
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }

    // Private methods
    private bool TryGetClosestPickableResource(Entity characterEnt, ref SystemState state, out Entity closestPickableResourceEnt)
    {
        closestPickableResourceEnt = Entity.Null;
        if (!SystemAPI.HasComponent<LocalToWorld>(characterEnt)) return false;

        LocalToWorld localToWorldCharacter = SystemAPI.GetComponent<LocalToWorld>(characterEnt);

        PhysicsWorld physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;
        NativeList<DistanceHit> outHits = new NativeList<DistanceHit>(Allocator.Temp);

        float radius = 5;
        CollisionFilter filter = new CollisionFilter
        {
            BelongsTo = 1 << 0,
            CollidesWith = 1 << 8,
        };
        physicsWorld.OverlapSphere(localToWorldCharacter.Position, radius, ref outHits, filter);
        
        Entity pickableResourceEnt = Entity.Null;
        float minDistance = 5;
        foreach (var hit in outHits)
        {
            Debug.Log("hitent: " + hit.Entity);
            Entity hitEnt = hit.Entity;
            if (pickableResourceLookup.HasComponent(hitEnt))
            {
                float dis = hit.Distance;

                if (minDistance > dis)
                {
                    pickableResourceEnt = hitEnt;
                    minDistance = dis;
                }
            }
        }
        Debug.Log("closestPickableResourceEnt: " + closestPickableResourceEnt);
        outHits.Dispose();
        if (pickableResourceEnt.Equals(Entity.Null)) return false;
        closestPickableResourceEnt = pickableResourceEnt;
        return true;
    }

    private void AddPickableResourceToInventory(Entity characterEnt, ref SystemState state, ref Entity pickableResourceEnt, ref EntityCommandBuffer ecb, int networkId, EntityQuery query1)
    {
        if (!SystemAPI.HasComponent<PickableResource>(pickableResourceEnt)) return;
        PickableResource pickableResource = SystemAPI.GetComponent<PickableResource>(pickableResourceEnt);
        ResourceId resourceId = pickableResource.ResourceId;
        ResourceTypeId resourceTypeId = pickableResource.ResourceTypeId;
        long elapsedTime = (long)SystemAPI.Time.ElapsedTime;
        if (!ResourceUtils.TryGetPickableResource(resourceTypeId, ref state, out PickableResource_StaticData pickableResource_StaticData, ref query1)) return;
        if (!ResourceUtils.TryDeactivateResource(ref state, ref pickableResourceEnt, in resourceId, elapsedTime, ref ecb, ref resourceQuery, ref serverInfoQuery)) return;
        
        var outputAmounts = new NativeList<int>(pickableResource_StaticData.OutputRangeAmount.Length, Allocator.TempJob);
        ResourceUtils.GenerateOutputItemsAmount(in pickableResource_StaticData.OutputRangeAmount, ref outputAmounts, ref rng);
        NativeList<InventoryItem_StaticData> inventoryItem_StaticDataList = new NativeList<InventoryItem_StaticData>(pickableResource_StaticData.OutputRangeAmount.Length, Allocator.TempJob);
        JobHandle getInventoryItemStaticDataJobHandle = new GetInventoryItemStaticDataJob
        {
            inventoryItem_StaticDataList = inventoryItem_StaticDataList,
            pickableResource_StaticData = pickableResource_StaticData
        }.Schedule(state.Dependency);
        NativeList<int> currentOutputAmountsAdded = new NativeList<int>(pickableResource_StaticData.OutputRangeAmount.Length, Allocator.TempJob);
        for (int i = 0; i < pickableResource_StaticData.OutputRangeAmount.Length; i++) currentOutputAmountsAdded.Add(0);
        MiscPrefabs miscPrefabs = SystemAPI.GetSingleton<MiscPrefabs>();
        JobHandle addPickResToExistingItemsInGhostInvJobHandle = new AddPickResToExistingItemsInGhostInvJob
        {
            inventoryItem_StaticDataList = inventoryItem_StaticDataList,
            ecb = ecb,
            networkId = networkId,
            pickableResource_StaticData = pickableResource_StaticData,
            outputAmounts = outputAmounts,
            currentOutputAmountsAdded = currentOutputAmountsAdded,
        }.Schedule(getInventoryItemStaticDataJobHandle);

        ref var serverInfo = ref SystemAPI.GetSingletonRW<ServerInfo>().ValueRW;
        int newId = serverInfo.NextAvailableItemId;
        serverInfo.NextAvailableItemId += 1;
        JobHandle addRemainingItemsHandle = new AddRemainingItemsJob
        {
            outputAmounts = outputAmounts,
            currentOutputAmountsAdded = currentOutputAmountsAdded,
            ecb = ecb,
            inventoryItem_StaticDataList = inventoryItem_StaticDataList,
            outputItems = pickableResource_StaticData.OutputItems,
            characterEnt = characterEnt,
            inventorySlotTrackerLookup = inventorySlotTrackerLookup,
            networkId = networkId,
            miscPrefabs = miscPrefabs,
            intItemId = newId
        }.Schedule(addPickResToExistingItemsInGhostInvJobHandle);
        state.Dependency = addRemainingItemsHandle;

        state.Dependency.Complete();

        outputAmounts.Dispose();
        currentOutputAmountsAdded.Dispose();
        inventoryItem_StaticDataList.Dispose();
    }

    // Jobs
    /// <summary>
    /// Job adding new inventory items to ghost inventory
    /// </summary>
    private partial struct AddRemainingItemsJob : IJobEntity
    {
        [ReadOnly] public NativeList<int> outputAmounts; // what the player should receive
        public NativeList<int> currentOutputAmountsAdded; // to keep track of how much we have added so far
        public EntityCommandBuffer ecb;
        [ReadOnly] public NativeList<InventoryItem_StaticData> inventoryItem_StaticDataList;
        [ReadOnly] public NativeList<ItemTypeId> outputItems;
        [ReadOnly] public Entity characterEnt;
        [ReadOnly] public int networkId;
        [ReadOnly] public MiscPrefabs miscPrefabs;
        [ReadOnly] public int intItemId;
        public ComponentLookup<InventorySlotTracker> inventorySlotTrackerLookup;
        void Execute(Entity entity, ref DynamicBuffer<InventoryItem_StaticData> inventoryItem_StaticData)
        {
            for (int i = 0; i < inventoryItem_StaticDataList.Length; i++)
            { // each element in the inventoryItem_StaticDataList represents static data attached to an inventory item,
              // by which the inventory item is received as an output item from when picking a resource
                if (currentOutputAmountsAdded[i] < outputAmounts[i]) // only if theres an amount to add left we will continue
                {
                    int remaining = outputAmounts[i] - currentOutputAmountsAdded[i]; // find exact amount left
                    int maxQuantity = inventoryItem_StaticDataList[i].MaxQuantity; // stack size of inv type

                    int newSlotsAmount = (int)math.ceil((float)remaining / maxQuantity); // rounds up to find amount of new entities inv items to create 

                    for (int e = 0; e < newSlotsAmount; e++)
                    {
                        remaining = outputAmounts[i] - currentOutputAmountsAdded[i]; // recalculate on new slot add
                        var ent = ecb.Instantiate(miscPrefabs.InventoryItemGhostPrefab);

                        if (!inventorySlotTrackerLookup.TryGetComponent(characterEnt, out InventorySlotTracker inventorySlotTracker)) return; // get last inventory index in use

                        int quantity = math.min(remaining, maxQuantity);
                        int newIndexSlot = inventorySlotTracker.LastIndexSlot;
                        
                        inventorySlotTracker.LastIndexSlot = newIndexSlot;
                        inventorySlotTrackerLookup[characterEnt] = inventorySlotTracker; // reassign new last index slot in use

                        ecb.AddComponent(ent, new InventoryItem
                        {
                            ItemTypeId = outputItems[i],
                            CurrentIndexSlot = newIndexSlot,
                            Quantity = quantity,
                            ItemId = (ItemId)intItemId
                        });
                        // NotLoaded component should already be instantiated in the prefab and enabled
                        inventorySlotTracker.LastIndexSlot = inventorySlotTracker.LastIndexSlot + 1;
                        inventorySlotTrackerLookup[characterEnt] = inventorySlotTracker;

                        ecb.AddComponent(ent, new GhostOwner
                        {
                            NetworkId = networkId
                        });
                        currentOutputAmountsAdded[i] += quantity;
                    }

                }

            }
        }
    }

    /// <summary>
    /// Get the "InventoryItem_StaticData" for each inventory item received from a pickableResource as output from picking it up
    /// </summary>
    // [BurstCompile]
    private partial struct GetInventoryItemStaticDataJob : IJobEntity
    {
        public NativeList<InventoryItem_StaticData> inventoryItem_StaticDataList;
        [ReadOnly] public PickableResource_StaticData pickableResource_StaticData;
        void Execute(Entity entity, ref DynamicBuffer<InventoryItem_StaticData> inventoryItem_StaticData)
        {
            foreach (InventoryItem_StaticData i in inventoryItem_StaticData)
            {
                foreach (ItemTypeId itemType in pickableResource_StaticData.OutputItems)
                {
                    if (itemType.Equals(i.ItemTypeId))
                    {
                        inventoryItem_StaticDataList.Add(i);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Adds a pickable resources equivalent output items and its amounts if theres already such an inventory item existing, so items become stackable
    /// </summary>
    //[BurstCompile]
    private partial struct AddPickResToExistingItemsInGhostInvJob : IJobEntity
    {
        // Note that outputAmounts, currentOutputAmountsAdded, inventoryItem_StaticDataList and outputItems lists indexes corresponds to each other
        [ReadOnly] public NativeList<InventoryItem_StaticData> inventoryItem_StaticDataList;
        public EntityCommandBuffer ecb;
        [ReadOnly] public int networkId;
        [ReadOnly] public PickableResource_StaticData pickableResource_StaticData;
        public NativeList<int> outputAmounts; // what the player should receive
        public NativeList<int> currentOutputAmountsAdded; // to keep track of how much we have added so far
        void Execute(Entity entity, ref InventoryItem inventoryItem, in GhostOwner ghostOwner)
        {
            if (!ghostOwner.NetworkId.Equals(networkId)) return; // goes through only items according to a specific player
            NativeList<ItemTypeId> outputItems = pickableResource_StaticData.OutputItems; // inventory items to get
            for (int i = 0; i < outputItems.Length; i++)
            {
                ItemTypeId itemTypeId = outputItems[i];

                if (itemTypeId.Equals(inventoryItem.ItemTypeId))
                {
                    if (currentOutputAmountsAdded[i] < outputAmounts[i])
                    {
                        ecb.SetComponentEnabled<NotLoaded>(entity, true);
                        int remaining = outputAmounts[i] - currentOutputAmountsAdded[i];

                        // get quantity and max stack size
                        int currentQuantity = inventoryItem.Quantity;
                        int maxQuantity = inventoryItem_StaticDataList[i].MaxQuantity;

                        int spaceAvailableInSlot = maxQuantity - currentQuantity;
                        int newRemaining = remaining - spaceAvailableInSlot;

                        if (newRemaining <= 0) // more space or exactly enough space than needed
                        {
                            inventoryItem.Quantity += remaining;
                            currentOutputAmountsAdded[i] = outputAmounts[i];
                        }
                        else
                        { // not enough space for all of it, so we add only what is available
                            inventoryItem.Quantity += spaceAvailableInSlot;
                            outputAmounts[i] = newRemaining;
                        }
                    }
                }
            }
        }
    }
}

partial struct OnClientUIIsLoaded_RpcHandler : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        var q = state.GetEntityQuery(typeof(InventoryUIIsLoaded_RPC), typeof(ReceiveRpcCommandRequest));
        state.RequireForUpdate(q);
    }
    public void OnUpdate(ref SystemState state)
    {
    var ecb = new EntityCommandBuffer(Allocator.Temp);
    int temporaryPlayerCount = 150; //! should be changed in the future

    NativeParallelHashSet<int> networkIds = new NativeParallelHashSet<int>(temporaryPlayerCount, Allocator.Temp); // unique values
    foreach (var (invUILoadedRpc, receiveRpc, entity) in SystemAPI.Query<RefRO<InventoryUIIsLoaded_RPC>, RefRO<ReceiveRpcCommandRequest>>().WithEntityAccess())
    {
        ecb.DestroyEntity(entity);
        int networkId = networkIdByReceiveRpc(ref state, receiveRpc);
        if (networkId.Equals(0)) continue;
        networkIds.Add(networkId);
    }

    foreach (var (inventoryItem, ghostOwner, notLoaded, entity) in SystemAPI.Query<RefRW<InventoryItem>, RefRO<GhostOwner>, EnabledRefRW<NotLoaded>>().WithEntityAccess())
    {
        int networkIdForInvItem = ghostOwner.ValueRO.NetworkId;
        if (!networkIds.Contains(networkIdForInvItem)) return;
        notLoaded.ValueRW = false;
    }

    networkIds.Dispose();
    ecb.Playback(state.EntityManager);
    ecb.Dispose();
}

    private int networkIdByReceiveRpc(ref SystemState state, RefRO<ReceiveRpcCommandRequest> receiveRpc)
    {
        Entity networkConnectionEnt = receiveRpc.ValueRO.SourceConnection;
        if (!SystemAPI.HasComponent<NetworkId>(networkConnectionEnt)) return 0;
        NetworkId networkId = SystemAPI.GetComponent<NetworkId>(networkConnectionEnt);
        int networkIdValue = networkId.Value;
        return networkIdValue;
    }
}

