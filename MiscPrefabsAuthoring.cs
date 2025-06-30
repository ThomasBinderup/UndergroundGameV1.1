using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using System;
using Unity.Entities.UniversalDelegates;
/// <summary>
/// file converting GameObject prefabs to entity prefabs
/// </summary>
public class MiscPrefabsAuthoring : MonoBehaviour
{
    [SerializeField]
    private GameObject inventoryItemGhostPrefab;

    
    class Baker : Baker<MiscPrefabsAuthoring>
    {
        public override void Bake(MiscPrefabsAuthoring a)
        {
            var entity = GetEntity(a, TransformUsageFlags.None);

            AddComponent(entity, new MiscPrefabs
            {
                InventoryItemGhostPrefab = GetEntity(a.inventoryItemGhostPrefab, TransformUsageFlags.Dynamic)
            });

        }
    }
}