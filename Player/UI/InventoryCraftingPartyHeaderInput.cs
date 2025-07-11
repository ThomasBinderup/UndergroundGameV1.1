using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
/// <summary>
/// Mono component to handle switching between panels (inventory, crafting, party) inside the player menu
/// </summary>
public class InventoryCraftingPartyHeaderInput : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private GameObject inventory;
    [SerializeField] private GameObject crafting;
    [SerializeField] private GameObject party;
    [SerializeField] private GameObject inventorySearchBar;
    [SerializeField] private ListenSearchBar listenSearchBar;
    [SerializeField] private TMP_InputField inventorySearchBarText;
    [SerializeField] private GameObject inventorySlots;
    public void OnPointerClick(PointerEventData eventData)
    {
        if (gameObject.CompareTag("InventoryHeader"))
        {
            crafting.SetActive(false);
            party.SetActive(false);
            inventory.SetActive(true);
            inventorySearchBar.SetActive(true);
            listenSearchBar.searchBarType = ListenSearchBar.SearchBarType.Inventory;

            if (inventorySearchBarText.text != "")
            {
                inventorySearchBarText.text = "";

                GameObject inventorySlots = GameObject.FindGameObjectWithTag("InventorySlots");

                foreach (Transform inventorySlot in inventorySlots.transform)
                {
                    inventorySlot.gameObject.SetActive(true);
                }
            }
        }
        else if (gameObject.CompareTag("CraftingHeader"))
        {
            crafting.SetActive(true);
            party.SetActive(false);
            inventory.SetActive(false);
            inventorySearchBar.SetActive(true);
            listenSearchBar.searchBarType = ListenSearchBar.SearchBarType.Crafting;

            if (inventorySearchBarText.text != "")
            {
                inventorySearchBarText.text = "";

                foreach (Transform inventorySlot in inventorySlots.transform)
                {
                    inventorySlot.gameObject.SetActive(true);
                }
            }
        }
        else if (gameObject.CompareTag("PartyHeader"))
        {
            crafting.SetActive(false);
            party.SetActive(true);
            inventory.SetActive(false);
            inventorySearchBar.SetActive(false);

            if (inventorySearchBarText.text != "")
            {
                inventorySearchBarText.text = "";

                foreach (Transform inventorySlot in inventorySlots.transform)
                {
                    inventorySlot.gameObject.SetActive(true);
                }
            }
        }
    }
}
