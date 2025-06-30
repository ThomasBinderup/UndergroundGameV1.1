using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using Unity.Scenes;
using UnityEngine;

public class ConnectionManager : MonoBehaviour
{
    [SerializeField] private string _listenIP = "127.0.0.1";
    [SerializeField] private string _connectIP = "127.0.0.1";
    [SerializeField] private ushort _port = 7979;

    private bool _connecting = false;

    private void Awake()
    {
        Application.runInBackground = true;
    }

    private void Start()
    {
        Connect();
    }
    public void Connect()
    {
        if (_connecting) { return; }
        _connecting = true;
        StartCoroutine(InitializeConnection());
    }

    private IEnumerator InitializeConnection() {
        bool isServer = ClientServerBootstrap.RequestedPlayType == ClientServerBootstrap.PlayType.ClientAndServer || ClientServerBootstrap.RequestedPlayType == ClientServerBootstrap.PlayType.Server;
        bool isClient = ClientServerBootstrap.RequestedPlayType == ClientServerBootstrap.PlayType.ClientAndServer || ClientServerBootstrap.RequestedPlayType == ClientServerBootstrap.PlayType.Client;
        while ((isServer && !ClientServerBootstrap.HasServerWorld) || (isClient && !ClientServerBootstrap.HasClientWorlds)) {
            yield return null;
        }
        if (isServer) {
            using var query = ServerWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
            query.GetSingletonRW<NetworkStreamDriver>().ValueRW.Listen(NetworkEndpoint.Parse(_listenIP, _port));
        }
        if (isClient) {
            using var query = ClientWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
            query.GetSingletonRW<NetworkStreamDriver>().ValueRW.Connect(ClientWorld.EntityManager, NetworkEndpoint.Parse(_connectIP, _port));
        }
        _connecting = false;
    }

    public static World ServerWorld {
        get
        {
            foreach (var world in World.All) {
                if (world.Flags == WorldFlags.GameServer) {
                    return world;
                }
            }
            return null;
        }
    }

    public static World ClientWorld {
        get
        {
            foreach (var world in World.All) {
                if (world.Flags == WorldFlags.GameClient) {
                    return world;
                }
            }
            return null;
        }
    }
}