    using Unity.Entities;
    using UnityEngine;

    public class InventoryItemAuthoring : MonoBehaviour
    {
        public int itemId;
        public int itemTypeId;
        public int quantity;
        public int currentIndexSlot;

        class Baker : Baker<InventoryItemAuthoring>
        {
        public override void Bake(InventoryItemAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
            AddComponent(entity, new InventoryItem
            {
            });
        }
        }
    }


