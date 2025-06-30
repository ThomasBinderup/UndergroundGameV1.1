using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
partial struct SetupStaticResourceData : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        var e = ecb.CreateEntity();
        var buffer = ecb.AddBuffer<PickableResource_StaticData>(e);

        // Add each individual resource item and its static data
        var outputItems_5001 = new NativeList<ItemTypeId>(5, Allocator.Persistent) { (ItemTypeId)1001 };
        var outputRangeAmount_5001 = new NativeList<int2>(5, Allocator.Persistent) { new(2, 6) };

        buffer.Add(new PickableResource_StaticData
        {
            ResourceTypeId = (ResourceTypeId)5001,
            OutputItems = outputItems_5001,
            OutputRangeAmount = outputRangeAmount_5001
        });
        
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
