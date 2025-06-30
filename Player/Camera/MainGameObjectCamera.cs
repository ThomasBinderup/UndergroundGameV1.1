

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// save the camera component in Instance so it can easily be accessed from SystemBase (MainCameraSystem.cs)
public class MainGameObjectCamera : MonoBehaviour
{
    public static Camera Instance;

    void Awake()
    {
        Instance = GetComponent<UnityEngine.Camera>();
    }
}