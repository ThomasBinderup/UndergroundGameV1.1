using System.Collections.Generic;
using System.Linq;
using SQLite;
using Unity.Burst;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using UnityEngine;

/// <summary>
/// Save the game state to the database periodically to minimize the loss of data on the event of a server crash.
/// Note: This system cannot be burst compiled, due to accessing SQLite db
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
partial struct SaveGameStatePeriodically : ISystem
{
    private double lastUpdateTime;
    private double updateFrequency;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        lastUpdateTime = SystemAPI.Time.ElapsedTime;
        updateFrequency = 10;
    }

    public void OnUpdate(ref SystemState state)
    {
        double elapsedTime = SystemAPI.Time.ElapsedTime;
        
        if (elapsedTime - lastUpdateTime < updateFrequency) return; // saves server information to database every "updateFrequency" to minimize data loss on a server crash
        SQLiteConnection db = new SQLiteConnection($"{Application.persistentDataPath}/dedicatedServerDatabase.db");
        EntityQuery entityQuery = state.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<ServerInfo>());

        updateServerInfoTable(db, entityQuery, ref state);

        lastUpdateTime = elapsedTime;
        db.Dispose();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }

    private void updateServerInfoTable(SQLiteConnection db, EntityQuery entityQuery, ref SystemState state)
    {
        ServerInfo serverInfo = entityQuery.GetSingleton<ServerInfo>();
        long newServerAge = serverInfo.ServerAgeAtBoot + (long)SystemAPI.Time.ElapsedTime;
 
        var serverInfoColumnsToUpdate = new Dictionary<string, object>
        {
            { "ServerAge", newServerAge },
            { "NextAvailableItemId", serverInfo.NextAvailableItemId }
        };
        string whereClause = "PrimaryId = ?";
        object[] whereArgs = { 1 };
        SQLiteUtils.UpdateTableDynamically(db, "ServerInfo", serverInfoColumnsToUpdate, whereClause, whereArgs);
    }
}
