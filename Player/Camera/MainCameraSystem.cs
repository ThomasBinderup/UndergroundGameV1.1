using NUnit.Framework.Internal;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class MainCameraSystem : SystemBase
{

    protected override void OnUpdate()
    {
        if ((MainGameObjectCamera.Instance != null && SystemAPI.HasSingleton<MainEntityCamera>()) && (OverlayGameObjectCamera.Instance != null && SystemAPI.HasSingleton<OverlayEntityCamera>()))
        {
            Entity mainEntityCameraEntity = SystemAPI.GetSingletonEntity<MainEntityCamera>(); // get entity by component
            LocalToWorld targetLocalToWorld = SystemAPI.GetComponent<LocalToWorld>(mainEntityCameraEntity); // get "LocalToWorld" component from the camera entity (view gameobject on character prefab)
            MainGameObjectCamera.Instance.transform.SetPositionAndRotation(targetLocalToWorld.Position, targetLocalToWorld.Rotation); // take the actual gameobject camera and set the position and rotation
            OverlayGameObjectCamera.Instance.transform.SetPositionAndRotation(targetLocalToWorld.Position, targetLocalToWorld.Rotation); // do the same for the overlay camera
        }
    }
}