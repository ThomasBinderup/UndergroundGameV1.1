using Unity.Entities;
using Unity.NetCode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class TestInput : MonoBehaviour
{
    EntityManager entityManager;

    void Start()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log("Pressed T once");
            entityManager.CreateEntity(typeof(TestRpc), typeof(SendRpcCommandRequest));
        }
    }
}

public struct TestRpc : IRpcCommand
{
}
