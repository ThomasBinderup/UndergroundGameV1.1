using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using UnityEngine;
using Unity.Mathematics;
/// <summary>
/// Utililty functions for "resource" systems. Note that due to them being accessed from outside a system, they do not have access to the SystemAPI. 
/// This means, only utility methods that do NOT use this API will be listed here. The others will have to be duplicated as private methods
/// in each system for the utility method in question
/// </summary>
[BurstCompile]
public static class ResourceUtils
{
    /// <summary>
    /// Tries to get the static data attached to a specific type of pickable resource
    /// </summary>
    [BurstCompile]
    public static bool TryGetPickableResource(ResourceTypeId resourceTypeId, ref SystemState state, out PickableResource_StaticData pickableResource_StaticData, ref EntityQuery query)
    {
        pickableResource_StaticData = default;
        if (!query.TryGetSingletonBuffer<PickableResource_StaticData>(out DynamicBuffer<PickableResource_StaticData> pickableResourceBuffer)) return false;

        foreach (PickableResource_StaticData pickRes in pickableResourceBuffer)
        {
            if (pickRes.ResourceTypeId.Equals(resourceTypeId))
            {
                pickableResource_StaticData = pickRes;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Tries to destroy resource ghost entity and reflects its this state in its corresponding "Resource" component
    /// </summary>
    [BurstCompile]
    public static bool TryDeactivateResource(ref SystemState state, ref Entity resourceEnt, in ResourceId resourceId, long elapsedTime, ref EntityCommandBuffer ecb, ref EntityQuery resourceQuery, ref EntityQuery serverInfoQuery)
    {
        if (!resourceQuery.TryGetSingletonEntity<Resource>(out Entity resSingleton)) return false;

        DynamicBuffer<Resource> resourceBuffer = state.EntityManager.GetBuffer<Resource>(resSingleton);

        for (int i = 0; i < resourceBuffer.Length; i++)
        {
            if (resourceBuffer[i].ResourceId.Equals(resourceId))
            {
                Resource modified = resourceBuffer[i];
                modified.IsActive = false;

                if (!serverInfoQuery.TryGetSingleton<ServerInfo>(out ServerInfo serverInfo)) return false;

                modified.DestroyedAt = serverInfo.ServerAgeAtBoot + elapsedTime;
                resourceBuffer[i] = modified;
                ecb.DestroyEntity(resourceEnt);
                return true;
            }
        }
        return false;
    }

    [BurstCompile]
    public static void GenerateOutputItemsAmount(in NativeList<int2> outputRanges, ref NativeList<int> outputAmounts)
    {
        Unity.Mathematics.Random rng = new Unity.Mathematics.Random(1);

        for (int i = 0; i < outputRanges.Length; i++)
        {
            int2 range = outputRanges[i];
            outputAmounts.Add(rng.NextInt(range.x, range.y));
        }
    }
}