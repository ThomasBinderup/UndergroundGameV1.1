using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
partial struct SetupStaticInventoryItemData : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        var e = ecb.CreateEntity();
        var buffer = ecb.AddBuffer<InventoryItem_StaticData>(e);

        // Add each individual inventory item and its static data
        buffer.Add(new InventoryItem_StaticData
        {
            ItemTypeId = (ItemTypeId)1001,
            MaxQuantity = 100,
            Description = new FixedString128Bytes("A stone"),
            Name = new FixedString32Bytes("Stone")
        });

        buffer.Add(new InventoryItem_StaticData
        {
            ItemTypeId = (ItemTypeId)1002,
            MaxQuantity = 10,
            Description = new FixedString128Bytes("A mushroom"),
            Name = new FixedString32Bytes("Beech Mushroom")
        });

        buffer.Add(new InventoryItem_StaticData
        {
            ItemTypeId = (ItemTypeId)1003,
            MaxQuantity = 100,
            Description = new FixedString128Bytes("A green berry"),
            Name = new FixedString32Bytes("Green Berry")
        });

        buffer.Add(new InventoryItem_StaticData
        {
            ItemTypeId = (ItemTypeId)1004,
            MaxQuantity = 100,
            Description = new FixedString128Bytes("A plant thread"),
            Name = new FixedString32Bytes("Plant Thread")
        });

        buffer.Add(new InventoryItem_StaticData
        {
            ItemTypeId = (ItemTypeId)1005,
            MaxQuantity = 100,
            Description = new FixedString128Bytes("A flarry berry"),
            Name = new FixedString32Bytes("Flarry Berry")
        });
        
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}