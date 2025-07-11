#if !UNITY_SERVER
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
/// <summary>
/// "InventoryInputs" is responsible for listening to some of the inputs effecting the inventory state that uses the new input system, and then
/// sends state changes to the server using RPCs.
/// </summary>
public class InventoryInputs : MonoBehaviour
{
    public GameObject PlayerInventorySlotsContainer;
    private PlayerInputs controls;
    private EntityManager entityManager;
    private bool playerMenuOpen;
    [SerializeField] private GameObject playerMenu;
    void Start()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        var e = entityManager.CreateEntity();
        entityManager.AddComponent<InventoryClosed>(e);
    }

    private void OnEnable()
    {


    }

    private void OnDisable()
    {

    }

    public void PickResource(InputAction.CallbackContext ctx)
    {
        entityManager.CreateEntity(typeof(PickResourceRpc), typeof(SendRpcCommandRequest));
    }

    public void TogglePlayerMenu(InputAction.CallbackContext ctx)
    {
        playerMenuOpen = !playerMenuOpen;
        playerMenu.SetActive(playerMenuOpen);

        // disable inputs and enable inputs when toggling the player menu by introducing the "InventoryClosed" component
        EntityQuery q = entityManager.CreateEntityQuery(ComponentType.ReadWrite<InventoryClosed>());
        int count = q.CalculateEntityCount();

        if (playerMenuOpen)
        {
            if (count > 0)
            {
                entityManager.DestroyEntity(q);
            }
        }
        else
        {
            if (count > 1)
            {
                entityManager.DestroyEntity(q);
            }

            if (count == 0)
            {
                Entity e = entityManager.CreateEntity();
                entityManager.AddComponent<InventoryClosed>(e);
            }
        }
    }
    
}


#endif