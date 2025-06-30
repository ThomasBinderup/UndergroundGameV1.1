#if !UNITY_SERVER
using Unity.Entities;
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
    void Start()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
    }

    private void OnEnable()
    {
        controls = InputMasterManager.Controls;
        controls.Player.PickResource.performed += pickResource;
    }

    private void OnDisable()
    {
        controls.Player.PickResource.performed -= pickResource;
    }

    private void pickResource(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        entityManager.CreateEntity(typeof(PickResourceRpc), typeof(SendRpcCommandRequest));
    }
    
}


#endif