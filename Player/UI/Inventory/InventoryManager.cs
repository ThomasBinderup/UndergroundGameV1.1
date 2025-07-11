using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class IdSpritePair
{
    public int key;
    public Sprite value;
}

public struct InventoryItemData_Client
{
    public ItemId ItemId;
    public ItemTypeId ItemTypeId;
}
public class InventoryManager : MonoBehaviour
{
    [HideInInspector] public Dictionary<GameObject, InventoryItemData_Client> UIItemToItemId = new Dictionary<GameObject, InventoryItemData_Client>();
    public List<IdSpritePair> idToSpriteDicList = new List<IdSpritePair>();
    [HideInInspector] public Dictionary<ItemTypeId, Sprite> IdToSpriteDic = new Dictionary<ItemTypeId, Sprite>(); // accessed by scripts
    public GameObject FullInventorySlotPrefab;
    public GameObject FullInventoryItemPrefab;
    void Awake()
    {
        foreach (IdSpritePair pair in idToSpriteDicList)
        {
            Debug.Log("IdToSpriteDic: " + pair.key);
            IdToSpriteDic.Add((ItemTypeId)pair.key, pair.value);
        }
    }
}
