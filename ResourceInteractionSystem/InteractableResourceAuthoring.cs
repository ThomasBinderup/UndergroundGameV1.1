using Unity.Entities;
using UnityEngine;

class InteractableResourceAuthoring : MonoBehaviour
{
    
}

class InteractableResourceBaker : Baker<InteractableResourceAuthoring>
{
    public override void Bake(InteractableResourceAuthoring authoring)
    {
        AddComponent(GetEntity(authoring, TransformUsageFlags.None), new InteractableResource
        {

        });
    }
}

public struct InteractableResource : IComponentData
{
    int RespawnTime; // in minutes

}

public struct ActiveResource : IComponentData
{
    
}
