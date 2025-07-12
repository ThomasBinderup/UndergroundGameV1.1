#if !UNITY_SERVER
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class InventorySlot_Mono : MonoBehaviour, IDropHandler
{
    EntityManager entityManager;
    void Start()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
    }
    public void OnDrop(PointerEventData eventData) // runs right before "OnEndDrag" inside InventoryItem_Mono
    {
        InventoryItem_Mono inventoryItemMono = eventData.pointerDrag.GetComponent<InventoryItem_Mono>();
        if (transform.childCount == 0)
        { // put item in new slot
            inventoryItemMono.parentAfterDrag = transform;
        } if (transform.childCount == 1)
        { // switch items
            GameObject inventoryItem = InventoryUtils.GetChildWithTag(transform, "InventoryItem");
            inventoryItem.transform.SetParent(inventoryItemMono.parentAfterDrag);
            inventoryItemMono.parentAfterDrag = transform;
        }
    }
}

#endif