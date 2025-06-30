using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

public static class InventoryUtils
{
    public static bool IsInventoryFull(NetworkId networkId)
    {

        return true;
    }
    
   /* public static void TryDoSomething(ref SystemState state, Entity networkConnection)
{
    foreach ((RefRO<InventoryItem> inv, RefRO<GhostOwner> owner, Entity entity) 
             in SystemAPI.Query<RefRO<InventoryItem>, RefRO<GhostOwner>>().WithEntityAccess())
    {
        if (owner.ValueRO.NetworkId == networkConnection)
        {
            // Do your logic
        }
    }
}*/
}