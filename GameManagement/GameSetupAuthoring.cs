using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using System;
using Unity.Entities.UniversalDelegates;
/// <summary>
/// file converting GameObject prefabs to entity prefabs
/// </summary>
public class GameSetupAuthoring : MonoBehaviour
{
    [SerializeField]
    private GameObject characterPrefab;
    [SerializeField]
    private GameObject playerPrefab;
    [SerializeField]
    private GameObject inventoryItemGhostPrefab;
    [SerializeField]
    private GameObject testGhostPrefab;
    [SerializeField]
    private GameObject testAnimChar;

    // Lists for linking ResourceTypeId to its associated prefab
    [SerializeField]
    private List<int> resourceTypeIds_Key = new List<int>();
    [SerializeField]
    private List<GameObject> resourcePrefabs_Value = new List<GameObject>();
    
    class Baker : Baker<GameSetupAuthoring>
    {
        public override void Bake(GameSetupAuthoring a)
        {
            var entity = GetEntity(a, TransformUsageFlags.None);
            var buffer = AddBuffer<ResourceTypeIdToPrefabEntity>(entity);

            // Dynamic buffer linking a ResourceTypeId to its associated prefab entity
            for (int i = 0; i < Mathf.Min(a.resourceTypeIds_Key.Count, a.resourcePrefabs_Value.Count); i++)
            {
                buffer.Add(new ResourceTypeIdToPrefabEntity
                {
                    ResourceTypeId = (ResourceTypeId)a.resourceTypeIds_Key[i],
                    PrefabEntity = GetEntity(a.resourcePrefabs_Value[i], TransformUsageFlags.None),
                });
            }

            AddComponent(entity, new GameSetup
            {
                CharacterPrefab = GetEntity(a.characterPrefab, TransformUsageFlags.None),
                PlayerPrefab = GetEntity(a.playerPrefab, TransformUsageFlags.None)
            });

            


            AddComponent(entity, new MiscPrefabs
            {
                InventoryItemGhostPrefab = GetEntity(a.inventoryItemGhostPrefab, TransformUsageFlags.None),
                TestGhostPrefab = GetEntity(a.testGhostPrefab, TransformUsageFlags.None),
                TestAnimChar = GetEntity(a.testAnimChar, TransformUsageFlags.Dynamic)
            });

        }
    }
}