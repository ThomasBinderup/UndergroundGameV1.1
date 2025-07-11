using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;

[Serializable]
public struct GameSetup : IComponentData
{
    public Entity CharacterPrefab;
    public Entity PlayerPrefab;
}

public struct MiscPrefabs : IComponentData
{
    public Entity InventoryItemGhostPrefab;
    public Entity TestGhostPrefab;
    public Entity InventoryLoadedPrefab;
    public Entity TestAnimChar;
}

public struct ResourceTypeIdToPrefabEntity : IBufferElementData
{
    public ResourceTypeId ResourceTypeId;
    public Entity PrefabEntity;
}

public struct AllResourceGMDestroyed : IComponentData
{


}

public struct ResourceLootAIdsByAId : IBufferElementData
{
    public int AId;
    public FixedList32Bytes<int> ResourceLootAIds;
}

public struct ServerInfo : IComponentData
{
    public long ServerAgeAtBoot; // The servers age at launch. This added with SystemAPI.Time.ElapsedTime gives the ServerAge
    public int NextAvailableItemId;
}


