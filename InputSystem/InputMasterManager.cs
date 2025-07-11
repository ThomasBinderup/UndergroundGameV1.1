#if !UNITY_SERVER
using UnityEngine;

public class InputMasterManager : MonoBehaviour
{
    [HideInInspector] public static PlayerInputs Controls;
    [SerializeField] private InventoryInputs inventoryInputs;

    void Awake()
    {
        Controls = new PlayerInputs();
    }

    private void OnEnable()
    {
        Controls.Player.TogglePlayerMenu.performed += ctx => inventoryInputs.TogglePlayerMenu(ctx);
        Controls.Player.PickResource.performed += ctx => inventoryInputs.PickResource(ctx);
        Controls.Enable();
    }
    
    private void OnDisable()
    {
        Controls.Disable();
    }

}

#endif