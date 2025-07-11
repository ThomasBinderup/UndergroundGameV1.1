using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using TMPro;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.NetCode;
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

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class UpdateInventoryUI : SystemBase
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
        foreach (var (inventoryItemGhost, notLoaded) in SystemAPI.Query<RefRO<InventoryItem>, EnabledRefRW<NotLoaded>>())
        {
            inventoryLoaded = true;
            Debug.Log("inventoryItemGhost: " + inventoryItemGhost);
            Debug.Log("Abc");

            int inventoryUISlotsCount = invSlots.transform.childCount;

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
            }
            else
            { // add new UI inventory item from ghost inventory
                var newFullInventorySlot = GameObject.Instantiate(fullInventorySlotPrefab, invSlots.transform).transform;
                var newInventoryItem = newFullInventorySlot.GetChild(0);

                // add easily accessible data inv item data to monobehaviours
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

