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
            MaxQuantity = 3,
            Description = new FixedString128Bytes("A stone"),
            Name = new FixedString32Bytes("Stone")
        });
        
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}