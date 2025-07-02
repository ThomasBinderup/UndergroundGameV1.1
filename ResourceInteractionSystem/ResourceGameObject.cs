using System;
using System.Data.SqlTypes;
using SQLite;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;
/// <summary>
/// Make a SQLite table for each resource gameobject, if this server is starting up for the first time. Otherwise use the
/// existing database to allow a persistent game server state where resources don't just respawn on a server crash or a shutdown
/// </summary>
public class ResourceGameObject : MonoBehaviour // primarily server side script
{
    [HideInInspector] private int respawnTime_Seconds = 60; //! should be changed in future
    void Start()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        if (world != null && !world.IsServer())
        {
            Destroy(gameObject);
            return;
        }
        if (SetupResourcesData.FirstTimeSetupResourcesInDB)
        {
            ResourceTypeId resourceTypeId = (ResourceTypeId)Int32.Parse(gameObject.name.Substring(gameObject.name.Length - 4));
            gameObject.transform.GetPositionAndRotation(out Vector3 worldPosition, out Quaternion rotation);
            float scale = gameObject.transform.lossyScale.x;

            SQLiteTables.Resource resource = new SQLiteTables.Resource
            {
                ResourceTypeId = resourceTypeId.ToInt(),
                WorldPositionX = worldPosition.x,
                WorldPositionY = worldPosition.y,
                WorldPositionZ = worldPosition.z,
                RotationX = rotation.x,
                RotationY = rotation.y,
                RotationZ = rotation.z,
                RotationW = rotation.w,
                Scale = scale,
                IsActive = true,
                DestroyedAt = 0,
                RespawnTime_Seconds = respawnTime_Seconds
            };

            SetupResourcesData.ResourceTables.Add(resource);
        }
        Destroy(gameObject);
    }
}


