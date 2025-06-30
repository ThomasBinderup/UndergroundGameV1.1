using System;
using System.Linq;
using SQLite;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// the "SetupServer" system is responsible for fetching the state of the resources from the database, and then assemble a Resource buffer, that contains
/// the state/information about each individual resource. This allows us to write/read from this Resource buffer at run time,
/// rather than using the SQLite database at run time so that we benefit from ECS performance (burst compilation and quick data fetching from components).
/// The system also instantiates ghost entities of the resource prefabs if the resources should exist on the client among other things that happens at start up.
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
partial struct SetupServer : ISystem
{
    bool run;
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<AllResourceGMDestroyed>();
    }

    [BurstCompile]
    public void OnStartRunning(ref SystemState state)
    { 
    }

    public void OnUpdate(ref SystemState state)
    {
        if (run) return;
        run = true;

        var ecb = new EntityCommandBuffer(Allocator.Temp);

        SQLiteConnection db = new SQLiteConnection($"{Application.persistentDataPath}/dedicatedServerDatabase.db");
        TableQuery<SQLiteTables.Resource> resources = db.Table<SQLiteTables.Resource>();

        setupResourcesBuffer(ecb, resources);
        setupResourceGhostEntities(ecb, resources, ref state);
        setupServerInfo(ecb, db);

        foreach ((RefRW<AllResourceGMDestroyed> allResourceGMDestroyed, Entity entity) in SystemAPI.Query<RefRW<AllResourceGMDestroyed>>().WithEntityAccess())
        {
            ecb.DestroyEntity(entity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
        db.Dispose();
    }

    private void setupResourcesBuffer(EntityCommandBuffer ecb, TableQuery<SQLiteTables.Resource> resources)
    {
        var e = ecb.CreateEntity();
        DynamicBuffer<Resource> resourceBuffer = ecb.AddBuffer<Resource>(e);

        foreach (SQLiteTables.Resource resource in resources)
        {
            resourceBuffer.Add(new Resource
            {
                ResourceId = (ResourceId)resource.ResourceId,
                ResourceTypeId = (ResourceTypeId)resource.ResourceTypeId,
                WorldPositionX = resource.WorldPositionX,
                WorldPositionY = resource.WorldPositionY,
                WorldPositionZ = resource.WorldPositionZ,
                RotationX = resource.RotationX,
                RotationY = resource.RotationY,
                RotationZ = resource.RotationZ,
                RotationW = resource.RotationW,
                Scale = resource.Scale,
                IsActive = resource.IsActive,
                DestroyedAt = resource.DestroyedAt,
                RespawnTime_Seconds = resource.RespawnTime_Seconds
            });
        }
    }

    private void setupResourceGhostEntities(EntityCommandBuffer ecb, TableQuery<SQLiteTables.Resource> resources, ref SystemState state)
    {
        var prefabsEntity = SystemAPI.GetSingletonEntity<ResourceTypeIdToPrefabEntity>();
        var resourceTypeIdToPrefabEntityBuffer = SystemAPI.GetBuffer<ResourceTypeIdToPrefabEntity>(prefabsEntity);

        foreach (SQLiteTables.Resource resource in resources)
        {
            if (!resource.IsActive) continue;

            ResourceTypeId targetResourceTypeId = (ResourceTypeId)resource.ResourceTypeId;
            Entity foundPrefab = Entity.Null;

            for (int i = 0; i < resourceTypeIdToPrefabEntityBuffer.Length; i++)
            {
                if (resourceTypeIdToPrefabEntityBuffer[i].ResourceTypeId == targetResourceTypeId)
                {
                    foundPrefab = resourceTypeIdToPrefabEntityBuffer[i].PrefabEntity;
                    break;
                }
            }

            if (foundPrefab != Entity.Null)
            {
                Entity resourceEnt = ecb.Instantiate(foundPrefab);
                ecb.AddComponent(resourceEnt, new LocalTransform
                {
                    Position = new float3(resource.WorldPositionX, resource.WorldPositionY, resource.WorldPositionZ),
                    Rotation = new quaternion(resource.RotationX, resource.RotationY, resource.RotationZ, resource.RotationW),
                    Scale = resource.Scale
                });
                ecb.AddComponent(resourceEnt, new PickableResource
                {
                    ResourceId = (ResourceId)resource.ResourceId,
                    ResourceTypeId = (ResourceTypeId)resource.ResourceTypeId
                });
            }
        }
    }

    /// <summary>
    /// Sets up the "ServerInfo" table for the server itself, if missing. Then it sets up a singleton component corresponding to the "ServerInfo" table,
    /// so we don't have to access the database all the time at run time to benefit from burst
    /// </summary>
    private void setupServerInfo(EntityCommandBuffer ecb, SQLiteConnection db)
    {
        bool exists = SQLiteUtils.DoesTableExist("ServerInfo", db);
        if (!exists)
        {
            db.CreateTable<SQLiteTables.ServerInfo>();
            var newServerInfoTable = new SQLiteTables.ServerInfo
            {
                ServerAge = 0,
                NextAvailableItemId = 1
            };

            db.Insert(newServerInfoTable);
        }

        var query = db.Table<SQLiteTables.ServerInfo>().Where(p => p.PrimaryId == 1);
        SQLiteTables.ServerInfo serverInfoTable = query.Single();

        Entity e = ecb.CreateEntity();
        var serverInfo = new ServerInfo
        {
            ServerAgeAtBoot = serverInfoTable.ServerAge, // assigns initial server age at boot
            NextAvailableItemId = serverInfoTable.NextAvailableItemId
        };
        ecb.AddComponent<ServerInfo>(e);
        ecb.SetComponent(e, serverInfo);
    }
}
