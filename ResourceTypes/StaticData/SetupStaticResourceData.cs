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
        var outputItems_5001 = new NativeList<ItemTypeId>(1, Allocator.Persistent) { (ItemTypeId)1001 };
        var outputRangeAmount_5001 = new NativeList<int2>(1, Allocator.Persistent) { new(2, 6) };

        buffer.Add(new PickableResource_StaticData
        {
            ResourceTypeId = (ResourceTypeId)5001,
            OutputItems = outputItems_5001,
            OutputRangeAmount = outputRangeAmount_5001
        });

        // add mushroom static data
        var outputItems_5002 = new NativeList<ItemTypeId>(1, Allocator.Persistent) { (ItemTypeId)1002 };
        var outputRangeAmount_5002 = new NativeList<int2>(1, Allocator.Persistent) { new(1, 2) };

        buffer.Add(new PickableResource_StaticData
        {
            ResourceTypeId = (ResourceTypeId)5002,
            OutputItems = outputItems_5002,
            OutputRangeAmount = outputRangeAmount_5002
        });

        var outputItems_5003 = new NativeList<ItemTypeId>(1, Allocator.Persistent) { (ItemTypeId)1003, (ItemTypeId)1004, (ItemTypeId)1005  };
        var outputRangeAmount_5003 = new NativeList<int2>(1, Allocator.Persistent) { new(1, 4), new(3, 20), new(1, 4) };

        buffer.Add(new PickableResource_StaticData
        {
            ResourceTypeId = (ResourceTypeId)5003,
            OutputItems = outputItems_5003,
            OutputRangeAmount = outputRangeAmount_5003
        });
        
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
