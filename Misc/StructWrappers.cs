using System;
using UnityEngine;

// "Strongly-Typed Wrapper" for IDs

// "ItemId" refers to the unique id for an item
[Serializable]
public struct ItemId : IEquatable<ItemId>
{
    public int Value;
    public ItemId(int value) => Value = value;
    public int ToInt() => Value;
    public bool Equals(ItemId other) => Value == other.Value;

    /* Allows implicit conversion
    ItemId itemId = new ItemId(42);
    int raw = itemId;
    */
    public static implicit operator int(ItemId id) => id.Value;

    /* Allows explicit cast
    int someNumber = 999;
    ItemId itemId = (ItemId)someNumber;
    */
    public static explicit operator ItemId(int value) => new ItemId(value);
}

// "ItemTypeId" refers to the type of item. For example a metal pickaxe or stone
// Allows access to image sprites for inventory UI element and prefab for the item
[Serializable]
public struct ItemTypeId : IEquatable<ItemTypeId> 
{
    public int Value;
    public ItemTypeId(int value) => Value = value;
    public int ToInt() => Value;
    public bool Equals(ItemTypeId other) => Value == other.Value;

    public static implicit operator int(ItemTypeId id) => id.Value;
    public static explicit operator ItemTypeId(int value) => new ItemTypeId(value);
}

// "ResourceId" refers to the unique id for a physical resource like a stone, tree, bush etc. Its ghost entities that can be harvested
[Serializable]
public struct ResourceId : IEquatable<ResourceId>
{
    public int Value;
    public ResourceId(int value) => Value = value;
    public int ToInt() => Value;
    public bool Equals(ResourceId other) => Value == other.Value;

    public static implicit operator int(ResourceId id) => id.Value;
    public static explicit operator ResourceId(int value) => new ResourceId(value);
}

// "ResourceTypeId" refers to the type of resource. Like a stone, tree, bush etc. but not a specific one.
// Allows access to its prefab
[Serializable]
public struct ResourceTypeId : IEquatable<ResourceTypeId>
{
    public int Value;
    public ResourceTypeId(int value) => Value = value;
    public int ToInt() => Value;
    public bool Equals(ResourceTypeId other) => Value == other.Value;

    public static implicit operator int(ResourceTypeId id) => id.Value;
    public static explicit operator ResourceTypeId(int value) => new ResourceTypeId(value);
}






