using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using TMPro;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Jobs;
using Unity.NetCode;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
partial struct UpdateInventoryState : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {

    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {

    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}
/// <summary>
/// System running on the client responsible for updating the inventory UI when new items are added, this can either be when a new count or a completely new item is added
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class UpdateInventoryUI_WhenNewItemsAdded : SystemBase
{
    private GameObject invSlots;
    private Dictionary<GameObject, InventoryItemData_Client> uiItemToItemId;
    private Dictionary<ItemTypeId, Sprite> idToSpriteDic;
    private GameObject fullInventorySlotPrefab;
    private GameObject fullInventoryItemPrefab;
    private InventoryManager inventoryManager;
    protected override void OnCreate()
    {
        // we actually dont have to filter notloaded because NotLoaded will always be there despite being disabled (it extends IEnableComponent interface), but
        // despite this the Archetype does not change whether its disabled or enabled, so RequireForUpdate doesnt care whether its enabled or not
        EntityQuery q = new EntityQueryBuilder(Allocator.Temp).WithAll<InventoryItem, NotLoaded>().Build(this);
        RequireForUpdate(q);
    }

    protected override void OnStartRunning()
    {
        invSlots = GameObject.FindGameObjectWithTag("InventorySlots");

        var inventoryManagerGM = GameObject.FindGameObjectWithTag("InventoryManager");
        inventoryManager = inventoryManagerGM.GetComponent<InventoryManager>();

        uiItemToItemId = inventoryManager.UIItemToItemId;
        fullInventorySlotPrefab = inventoryManager.FullInventorySlotPrefab;
        idToSpriteDic = inventoryManager.IdToSpriteDic;
        fullInventoryItemPrefab = inventoryManager.FullInventoryItemPrefab;

    }
    protected override void OnUpdate()
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        bool inventoryLoaded = false;
        int inventoryUISlotsCount = 0;
        foreach (var (inventoryItemGhost, notLoaded) in SystemAPI.Query<RefRO<InventoryItem>, EnabledRefRW<NotLoaded>>())
        {
            inventoryLoaded = true;
            Debug.Log("inventoryItemGhost: " + inventoryItemGhost);
            Debug.Log("Abc");

            inventoryUISlotsCount = invSlots.transform.childCount;

            // get static inventory values
            var invItem_StaDataBuf = SystemAPI.GetSingletonBuffer<InventoryItem_StaticData>();
            int maxQuantity = 1;
            int maxDurability = 0;
            string itemName = "Name";
            foreach (var staticData in invItem_StaDataBuf)
            {
                if (staticData.ItemTypeId.Equals(inventoryItemGhost.ValueRO.ItemTypeId))
                {
                    maxQuantity = staticData.MaxQuantity;
                    maxDurability = staticData.MaxDurability;
                    itemName = staticData.Name.ToString();
                    break;
                }
            }
            Debug.Log("inventoryUISlotsCount: " + inventoryUISlotsCount + " inventoryItemGhost.ValueRO.CurrentIndexSlot + 1: " + inventoryItemGhost.ValueRO.CurrentIndexSlot + 1);
            if (inventoryUISlotsCount >= inventoryItemGhost.ValueRO.CurrentIndexSlot + 1)
            { // refresh item values
                Transform inventorySlot = invSlots.transform.GetChild(inventoryItemGhost.ValueRO.CurrentIndexSlot);
                Transform inventoryItem_UI = inventorySlot.GetChild(0);
                GetUIChildElements(inventoryItem_UI, out Transform quantity_UI, out Transform durability_UI, out Transform tooltip_UI);
                UpdateItemChildElements(inventoryItemGhost.ValueRO, quantity_UI, durability_UI, tooltip_UI, maxQuantity, maxDurability, itemName);

                var itemImage = inventoryItem_UI.GetComponent<UnityEngine.UI.Image>();

                if (!idToSpriteDic.TryGetValue(inventoryItemGhost.ValueRO.ItemTypeId, out Sprite itemSprite)) return;
                itemImage.sprite = itemSprite;
            }
            else
            { // add new UI inventory item from ghost inventory
                var newFullInventorySlot = GameObject.Instantiate(fullInventorySlotPrefab, invSlots.transform).transform;
                var newInventoryItem = newFullInventorySlot.GetChild(0);

                // add easily accessible data inv item data for monobehaviours
                InventoryItemData_Client inventoryItemData_Client = new InventoryItemData_Client
                {
                    ItemId = inventoryItemGhost.ValueRO.ItemId,
                    ItemTypeId = inventoryItemGhost.ValueRO.ItemTypeId
                };

                if (!uiItemToItemId.TryAdd(newInventoryItem.gameObject, inventoryItemData_Client))
                {

                    uiItemToItemId[newInventoryItem.gameObject] = inventoryItemData_Client;
                }

                var itemImage = newInventoryItem.GetComponent<UnityEngine.UI.Image>();

                if (!idToSpriteDic.TryGetValue(inventoryItemGhost.ValueRO.ItemTypeId, out Sprite itemSprite)) return;
                itemImage.sprite = itemSprite;

                GetUIChildElements(newInventoryItem, out Transform quantity_UI, out Transform durability_UI, out Transform tooltip_UI);
                UpdateItemChildElements(inventoryItemGhost.ValueRO, quantity_UI, durability_UI, tooltip_UI, maxQuantity, maxDurability, itemName);

                newFullInventorySlot.SetSiblingIndex(invSlots.transform.childCount - 1);
            }
        }

        // delete client inventory items that have higher slot index than in the ghost inventory
        EntityQuery q = GetEntityQuery(ComponentType.ReadOnly<InventoryItem>());
        int itemCount = q.CalculateEntityCount();
        if (inventoryUISlotsCount > itemCount)
        {
            for (int i = itemCount; i < inventoryUISlotsCount; i++) {
                Transform slotChild = invSlots.transform.GetChild(i);
                GameObject.Destroy(slotChild.gameObject);
            }
        }

        if (inventoryLoaded)
        {
            var rpcEnt = ecb.CreateEntity();
            ecb.AddComponent(rpcEnt, typeof(InventoryUIIsLoaded_RPC));
            ecb.AddComponent(rpcEnt, typeof(SendRpcCommandRequest));
        }

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }

    private void GetUIChildElements(Transform inventoryItem, out Transform quantity_UI, out Transform durability_UI, out Transform tooltip_UI)
    {
        quantity_UI = null;
        durability_UI = null;
        tooltip_UI = null;
        foreach (Transform child_UI in inventoryItem)
        {
            switch (child_UI.tag)
            {
                case "Quantity":
                    quantity_UI = child_UI;
                    break;
                case "Durability":
                    durability_UI = child_UI;
                    break;
                case "Tooltip":
                    tooltip_UI = child_UI;
                    break;
                default:
                    break;
            }
        }
    }

    private void UpdateItemChildElements(InventoryItem ghostInventoryItem, Transform quantity_UI, Transform durability_UI, Transform tooltip_UI, int maxQuantity, int maxDurability, string itemName)
    {
        Debug.Log("r1");
        if (quantity_UI != null)
        {
            Debug.Log("r2");
            var text_UI = quantity_UI.GetComponent<TextMeshProUGUI>();
            if (maxQuantity > 1)
            {
                Debug.Log("ghostInventoryItem.Quantity.ToString();" + ghostInventoryItem.Quantity.ToString());
                text_UI.text = ghostInventoryItem.Quantity.ToString();
            }
            else
            {
                quantity_UI.gameObject.SetActive(false);
            }
        }

        if (durability_UI != null)
        {
            var text_UI = durability_UI.GetComponent<TextMeshProUGUI>();
            if (maxDurability > 0)
            {
                text_UI.text = ghostInventoryItem.Durability.ToString();
            }
            else
            {
                durability_UI.gameObject.SetActive(false);
            }
        }

        if (tooltip_UI != null)
        {
            GameObject name = InventoryUtils.GetChildWithTag(tooltip_UI, "ItemName_UI");
            if (!name) return;
            TextMeshProUGUI nameTextUI = name.GetComponent<TextMeshProUGUI>();
            nameTextUI.text = itemName;
        }
    }

    private partial struct AddRemainingItemsJob : IJobEntity
    {
        void Execute(ref InventoryItem inventoryItem)
        {

        }
    }
}

/// <summary>
/// System listening for client RPCs wanting to switch an item, hereafter validating, if its valid on the server before allowing the action, so ghost inventory
/// and player inventory stay in sync
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
partial struct SwapInventoryItem_RPCHandler : ISystem
{
    private EntityQuery ghostInvItemsQuery;
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SwapInventoryItem_RPC>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        // get all networkIDs from RPC
        var networkIDsFromRPC = new NativeList<int>(Allocator.TempJob);
        foreach (var (swapInventoryItem_RPC, receiveRPC, entity) in SystemAPI.Query<RefRO<SwapInventoryItem_RPC>, RefRO<ReceiveRpcCommandRequest>>().WithEntityAccess())
        {
            int networkId = networkIdByReceiveRpc(ref state, receiveRPC);
            if (networkId.Equals(0)) continue;
            if (!networkIDsFromRPC.Contains(networkId)) networkIDsFromRPC.Add(networkId);
        }
        // get all inventory ghost items linked where each are linked to its respective ghost owner (networkId)
        var networkIdToInventoryItemsList_List = new NativeList<NetworkIdToInventoryItemsList>(Allocator.TempJob);
        var filterOwnedEntitiesJobHandle = new FilterOwnedEntitiesJob
        {
            targetNetworkIds = networkIDsFromRPC,
            networkIdToInventoryItemsList_List = networkIdToInventoryItemsList_List
        }.Schedule(state.Dependency);
        state.Dependency.Complete();
        
        foreach (var (swapInventoryItem_RPC, receiveRPC, entity) in SystemAPI.Query<RefRO<SwapInventoryItem_RPC>, RefRO<ReceiveRpcCommandRequest>>().WithEntityAccess())
        {
            // validate and queue RPC for elimination
            ecb.DestroyEntity(entity);
            bool valid = isSwapInventoryItemRPCValid(swapInventoryItem_RPC);
            if (!valid) break;

            int networkId = networkIdByReceiveRpc(ref state, receiveRPC);

            // rpc data
            int swapFrom = swapInventoryItem_RPC.ValueRO.SwapFrom;
            int swapTo = swapInventoryItem_RPC.ValueRO.SwapTo;
            bool moveItemToLastSlot = swapInventoryItem_RPC.ValueRO.MoveItemToLastSlot;

            foreach (NetworkIdToInventoryItemsList compData in networkIdToInventoryItemsList_List)
            {
                if (!compData.NetworkId.Equals(networkId)) continue;
                // move item (swapFrom_InvItem) to last slot and move ghost "InventoryItem"s after this one by one index to the left (-1)
                if (moveItemToLastSlot)
                {
                    foreach (Entity inventoryItem_Entity in compData.InventoryItemEntitiesList)
                    {
                        RefRW<InventoryItem> inventoryItem = SystemAPI.GetComponentRW<InventoryItem>(inventoryItem_Entity);
                        if (inventoryItem.ValueRO.CurrentIndexSlot.Equals(swapFrom))
                        {
                            int lastIndex = compData.InventoryItemEntitiesList.Length - 1;
                            inventoryItem.ValueRW.CurrentIndexSlot = lastIndex;
                            continue;
                        }
                        if (inventoryItem.ValueRO.CurrentIndexSlot > swapFrom)
                        {
                            inventoryItem.ValueRW.CurrentIndexSlot += -1;
                        }
                    }
                    break;
                }

                // swap two existing inventory items or stack them together if they are of same ItemTypeId and stackable (InventoryItem_StaticData.MaxQuantity > 1)
                RefRW<InventoryItem> swapFrom_InvItem = default;
                Entity swapFromEntity = default;
                bool swapFrom_InvItemAssigned = false;
                RefRW<InventoryItem> swapTo_InvItem = default;
                Entity swapToEntity = default;
                bool swapTo_InvItemAssigned = false;

                foreach (Entity inventoryItem_Entity in compData.InventoryItemEntitiesList)
                {
                    RefRW<InventoryItem> inventoryItem = SystemAPI.GetComponentRW<InventoryItem>(inventoryItem_Entity);

                    if (inventoryItem.ValueRO.CurrentIndexSlot.Equals(swapFrom))
                    {
                        swapFrom_InvItem = inventoryItem;
                        swapFromEntity = inventoryItem_Entity;
                        swapFrom_InvItemAssigned = true;
                    }
                    if (!moveItemToLastSlot)
                    {
                        if (inventoryItem.ValueRO.CurrentIndexSlot.Equals(swapTo))
                        {
                            swapTo_InvItem = inventoryItem;
                            swapToEntity = inventoryItem_Entity;
                            swapTo_InvItemAssigned = true;
                        }
                    }
                }

                if (swapFrom_InvItemAssigned && swapTo_InvItemAssigned)
                {
                    if (swapFrom_InvItem.ValueRO.ItemTypeId.Equals(swapTo_InvItem.ValueRO.ItemTypeId))
                    {
                        // get static data to check max quantity for item
                        var staticItemDataBuf = SystemAPI.GetSingletonBuffer<InventoryItem_StaticData>(true);
                        bool maxQuantityAssigned = false;
                        int maxQuantity = 0;
                        foreach (var data in staticItemDataBuf)
                        {
                            if (data.ItemTypeId.Equals(swapFrom_InvItem.ValueRO.ItemTypeId))
                            {
                                maxQuantity = data.MaxQuantity;
                                maxQuantityAssigned = true;
                                break;
                            }
                        }
                        if (maxQuantityAssigned)
                        {
                            // combine items
                            if (maxQuantity > 1)
                            {
                                var totalQuantity = swapFrom_InvItem.ValueRO.Quantity + swapTo_InvItem.ValueRO.Quantity;
                                if (totalQuantity < maxQuantity) // no spare quantity so swapFrom ghost inv item will get deleted
                                {
                                    swapTo_InvItem.ValueRW.Quantity = totalQuantity;
                                    ecb.DestroyEntity(swapFromEntity);
                                }
                                else
                                {
                                    var leftOverQuantity = -(maxQuantity - totalQuantity);
                                    swapTo_InvItem.ValueRW.Quantity = maxQuantity;
                                    swapFrom_InvItem.ValueRW.Quantity = leftOverQuantity;
                                }
                            } // else: we simply swap the items
                        }
                        else
                        { // invalid RPC so continue
                            continue;
                        }
                    }
                    else
                    {
                        var copyFrom = swapFrom_InvItem.ValueRO;
                        swapFrom_InvItem.ValueRW = swapTo_InvItem.ValueRO;
                        swapTo_InvItem.ValueRW = copyFrom;

                    }


                } // else: RPC is invalid
                break;
            }
        }


        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }

    private int networkIdByReceiveRpc(ref SystemState state, RefRO<ReceiveRpcCommandRequest> receiveRpc)
    {
        Entity networkConnectionEnt = receiveRpc.ValueRO.SourceConnection;
        if (!SystemAPI.HasComponent<NetworkId>(networkConnectionEnt)) return 0;
        NetworkId networkId = SystemAPI.GetComponent<NetworkId>(networkConnectionEnt);
        int networkIdValue = networkId.Value;
        return networkIdValue;
    }
    /// <summary>
    /// Validate swap RPC
    /// </summary>
    private bool isSwapInventoryItemRPCValid(RefRO<SwapInventoryItem_RPC> swapInventoryItem_RPC)
    {
        if (swapInventoryItem_RPC.ValueRO.SwapFrom == swapInventoryItem_RPC.ValueRO.SwapTo) return false;
        if (swapInventoryItem_RPC.ValueRO.SwapFrom < 0 || swapInventoryItem_RPC.ValueRO.SwapFrom > 299) return false;
        if (swapInventoryItem_RPC.ValueRO.SwapTo < 0 || swapInventoryItem_RPC.ValueRO.SwapTo > 300) return false;
        return true;        
    }
    
    [BurstCompile]
    public partial struct FilterOwnedEntitiesJob : IJobEntity
    {
        [ReadOnly] public NativeList<int> targetNetworkIds;
        public NativeList<NetworkIdToInventoryItemsList> networkIdToInventoryItemsList_List;
        public void Execute(Entity entity, in InventoryItem inventoryItem, in GhostOwner ghostOwner)
        {
            foreach (int targetNetworkId in targetNetworkIds)
            {
                if (ghostOwner.NetworkId == targetNetworkId)
                {
                    bool foundList = false;
                    for (int i = 0; i < networkIdToInventoryItemsList_List.Length; i++)
                    {
                        if (networkIdToInventoryItemsList_List[i].NetworkId == targetNetworkId)
                        {
                            networkIdToInventoryItemsList_List[i].InventoryItemEntitiesList.Add(entity);
                            foundList = true;
                        }
                    }
                    if (!foundList)
                    {
                        networkIdToInventoryItemsList_List.Add(new NetworkIdToInventoryItemsList
                        {
                            NetworkId = targetNetworkId,
                            InventoryItemEntitiesList = new NativeList<Entity>(Allocator.TempJob)
                        });
                    }
                    break;
                }
            }
            
        }
    }
}

