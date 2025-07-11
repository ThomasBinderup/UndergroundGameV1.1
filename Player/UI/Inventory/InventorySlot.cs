#if !UNITY_SERVER
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class InventorySlot : MonoBehaviour, IDropHandler
{
    EntityManager entityManager;
    void Start()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
    }
    public void OnDrop(PointerEventData eventData)
    {
        if (transform.childCount == 0)
        { // put item in new slot
            InventoryItemMono inventoryItemMono = eventData.pointerDrag.GetComponent<InventoryItemMono>();
            inventoryItemMono.parentAfterDrag = transform;
        }
    }
}

#endif