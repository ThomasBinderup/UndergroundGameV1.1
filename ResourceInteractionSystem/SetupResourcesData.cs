using System;
using System.Collections.Generic;
using SQLite;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
/// <summary>
/// Singleton file responsible for opening a single connection to the SQLite database and adding the individual resource tables added from the "ResourceGameObject.cs" scripts, if
/// it is the first time the server is starting. 
/// </summary>
public class SetupResourcesData : MonoBehaviour // primarily server side script
{
    [HideInInspector] public static bool FirstTimeSetupResourcesInDB = false;
    [HideInInspector] public static List<SQLiteTables.Resource> ResourceTables = new List<SQLiteTables.Resource>();
    private SQLiteConnection db;
    private World world;
    private void Awake()
    {
        world = World.DefaultGameObjectInjectionWorld;

        if (world != null && !world.IsServer()) return;

        db = new SQLiteConnection($"{Application.persistentDataPath}/dedicatedServerDatabase.db");
        bool resourceTableExists = SQLiteUtils.DoesTableExist("Resource", db);
        if (!resourceTableExists)
        {
            FirstTimeSetupResourcesInDB = true;
            db.CreateTable<SQLiteTables.Resource>();
        }
    }
    EntityManager entityManager;
    void Start()
    {
        if (world != null && !world.IsServer()) return;
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
    }

    void Update()
    {
        if (world != null && world.IsServer())
        {
            var resources = FindObjectsByType(typeof(ResourceGameObject), FindObjectsSortMode.None);
            if (resources.Length == 0)
            {
                if (FirstTimeSetupResourcesInDB)
                {
                    for (int i = 0; i < ResourceTables.Count; i++)
                    {
                        SQLiteTables.Resource resourceTable = ResourceTables[i];
                        db.Insert(resourceTable);
                    }
                }
                db.Dispose();

                var entity = entityManager.CreateEntity();
                entityManager.AddComponentData(entity, new AllResourceGMDestroyed());
            }
        }
        Destroy(gameObject);
    }
}
