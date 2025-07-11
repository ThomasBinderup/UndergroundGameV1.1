using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.NetCode;

[GhostComponent(OwnerSendType = SendToOwnerType.SendToOwner)] // Inventory items should only be visible to the player itself
public struct InventoryItem : IComponentData
{
    [GhostField]
    public ItemId ItemId;
    [GhostField]
    public ItemTypeId ItemTypeId;
    [GhostField]
    public int CurrentIndexSlot; // Begins at 0 (top left) to max 300 (bottom right). 
    [GhostField]
    public int Quantity;
    [GhostField]
    public int Durability;
}
public struct MoveInventoryItemRpc : IRpcCommand
    {
        public ItemId ItemId;
        public ItemTypeId ItemTypeId;
        public int CurrentIndexSlot;
        public int NewIndexSlot;
    }

public struct PickResourceRpc : IRpcCommand
{
}

// General information about every InventoryItem no matter the type. All inventory items have this.
public struct InventoryItem_StaticData : IBufferElementData
{
    public ItemTypeId ItemTypeId; // Between 1001-3001
    public int MaxQuantity;
    public int MaxDurability;
    public FixedString128Bytes Description;
    public FixedString32Bytes Name;

}

// Component to track the last index slot of a player. Attached to each character
// Warning! MUST be updated in all places, where an inventory item is added. 
public struct InventorySlotTracker : IComponentData
{
    public int LastIndexSlot;
}

public struct UpdateInventoryUI_RPC : IRpcCommand
{
}

public struct InventoryUIIsLoaded_RPC : IRpcCommand
{
}

[GhostEnabledBit]
[GhostComponent(OwnerSendType = SendToOwnerType.SendToOwner)]
public struct NotLoaded : IComponentData, IEnableableComponent
{
}

/// <summary>
/// Used to prevent ECS systems listening for certain player inputs, when inventory is closed (for instance, you cant walk when inside inventory)
/// </summary>
public struct InventoryClosed : IComponentData
{

}