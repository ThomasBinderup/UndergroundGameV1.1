    using Unity.Entities;
    using UnityEngine;

    public class InventoryItemAuthoring : MonoBehaviour
    {
        class Baker : Baker<InventoryItemAuthoring>
        {
        public override void Bake(InventoryItemAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
            AddComponent(entity, new InventoryItem
            {
            });
            AddComponent(entity, new NotLoaded { });
        }
    }
    }


