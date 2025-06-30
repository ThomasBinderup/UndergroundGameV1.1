

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OverlayGameObjectCamera : MonoBehaviour
{
    public static Camera Instance;

    void Awake()
    {
        Instance = GetComponent<UnityEngine.Camera>();
    }
}
