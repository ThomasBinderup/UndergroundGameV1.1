using Unity.Entities;
using UnityEngine;
public class InventoryLoadedAuthoring : MonoBehaviour
{
        class Baker : Baker<InventoryItemAuthoring>
        {
        public override void Bake(InventoryItemAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
            AddComponent(entity, new InventoryLoaded
            {
            });
        }
        }
}
