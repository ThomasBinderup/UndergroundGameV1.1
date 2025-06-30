using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
// each component identifies each camera
[Serializable]
public struct MainEntityCamera : IComponentData
{
}

[Serializable]
public struct OverlayEntityCamera : IComponentData
{
}
