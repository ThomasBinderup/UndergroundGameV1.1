using TMPro;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.Entities;
using Unity.Collections;
using Unity.Entities.UniversalDelegates;

/// <summary>
/// Filter all items being searched for based on an input string. Used on UI elements with search bars.
/// </summary>
public class ListenSearchBar : MonoBehaviour
{
    public TMP_InputField inputField;
    [SerializeField] private InventoryManager inventoryManager;
    public SearchBarType searchBarType = SearchBarType.Inventory;
    EntityManager entityManager;
    void Start()
    {
        inputField.onValueChanged.AddListener(OnTyping);
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
    }
    void OnTyping(string userInput)
    {
        string pattern = $@"\b\w*{Regex.Escape(userInput)}\w*\b";

        if (searchBarType.Equals(SearchBarType.Inventory))
        {
            GameObject inventorySlots = GameObject.FindGameObjectWithTag("InventorySlots");
            
            foreach (Transform inventorySlot in inventorySlots.transform)
            {
                if (userInput.Equals(""))
                {
                    inventorySlot.gameObject.SetActive(true);
                    continue;
                }
                // find name of an inventory item
                var invItem = inventorySlot.GetChild(0);
                var tooltip = InventoryUtils.GetChildWithTag(invItem, "Tooltip");
                if (!tooltip) continue; //! all items should have tooltip, so fix when this happens
                var name = InventoryUtils.GetChildWithTag(tooltip.transform, "ItemName_UI");
                if (!name) continue;
                string itemName = name.GetComponent<TextMeshProUGUI>().text;
                // check if this inventory name matches what the player is searching for
                if (Regex.IsMatch(itemName, pattern, RegexOptions.IgnoreCase))
                {
                    inventorySlot.gameObject.SetActive(true);
                }
                else
                {
                    inventorySlot.gameObject.SetActive(false);
                }
            }
        }
        else if (searchBarType.Equals(searchBarType.Equals(SearchBarType.Crafting)))
        {

        }
    }

    public enum SearchBarType
    {
        Inventory,
        Crafting,
        Workbench,
        Smithy
    }
}


