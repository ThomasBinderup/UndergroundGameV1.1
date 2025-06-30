using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct PickableResource_StaticData : IBufferElementData
{
    public ResourceTypeId ResourceTypeId;
    public NativeList<ItemTypeId> OutputItems;
    public NativeList<int2> OutputRangeAmount;
}

public struct HarvestableResource_StaticData : IComponentData
{

}


