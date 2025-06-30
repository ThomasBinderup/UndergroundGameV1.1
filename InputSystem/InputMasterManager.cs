#if !UNITY_SERVER
using UnityEngine;

public class InputMasterManager : MonoBehaviour
{
    [HideInInspector] public static PlayerInputs Controls;

    void Awake()
    {
        Controls = new PlayerInputs();
    }

    private void OnEnable()
    {
        Controls.Enable();
    }
    
    private void OnDisable()
    {
        Controls.Disable();
    }

}

#endif