using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Mathematics;
using Unity.Transforms;
using System;

public struct ClientJoinRequest : IRpcCommand
{ }

public struct LocalInitialized : IComponentData
{ }

public struct CharacterBySourceConnection : IComponentData
{
    public Entity characterEntity;
}

[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct ServerGameSetupSystem : ISystem
{
    private Unity.Mathematics.Random _random;

    [BurstCompile]
    void OnCreate(ref SystemState state)
    {
        _random = Unity.Mathematics.Random.CreateFromIndex(0);
    }

    [BurstCompile]
    void OnUpdate(ref SystemState state)
    {
        if (SystemAPI.HasSingleton<GameSetup>())
        {
            EntityCommandBuffer ecb = SystemAPI.GetSingletonRW<EndSimulationEntityCommandBufferSystem.Singleton>().ValueRW.CreateCommandBuffer(state.WorldUnmanaged);

            // Get our GameSetup singleton, which contains the prefabs we'll spawn
            GameSetup gameSetup = SystemAPI.GetSingleton<GameSetup>();
            // When a client wants to join, spawn and setup a character for them
            foreach (var (recieveRPC, joinRequest, entity) in SystemAPI.Query<ReceiveRpcCommandRequest, ClientJoinRequest>().WithEntityAccess())
            {                
                // Spawn character and player ghost prefabs
                Entity characterEntity = ecb.Instantiate(gameSetup.CharacterPrefab);
                Entity playerEntity = ecb.Instantiate(gameSetup.PlayerPrefab);
                    
                // Add spawned prefabs to the connection entity's linked entities, so they get destroyed along with it
                ecb.AppendToBuffer(recieveRPC.SourceConnection, new LinkedEntityGroup { Value = characterEntity });
                ecb.AppendToBuffer(recieveRPC.SourceConnection, new LinkedEntityGroup { Value = playerEntity });

                // Setup the owners of the ghost prefabs (which are all owner-predicted) 
                // The owner is the client connection that sent the join request
                int clientConnectionId = SystemAPI.GetComponent<NetworkId>(recieveRPC.SourceConnection).Value;
                ecb.SetComponent(characterEntity, new GhostOwner { NetworkId = clientConnectionId });
                ecb.SetComponent(playerEntity, new GhostOwner { NetworkId = clientConnectionId });

                // Setup links between the prefabs
                FirstPersonPlayer player = SystemAPI.GetComponent<FirstPersonPlayer>(gameSetup.PlayerPrefab);
                player.ControlledCharacter = characterEntity;
                ecb.SetComponent(playerEntity, player);
                
                // Place character 
                ecb.SetComponent(characterEntity, LocalTransform.FromPosition(new float3(0f, 0f, 0f)));
                
                // Allow this client to stream in game
                ecb.AddComponent<NetworkStreamInGame>(recieveRPC.SourceConnection);

                // Allow access to character by source connection
                Entity connectionEntity = recieveRPC.SourceConnection;
                ecb.AddComponent(connectionEntity, new CharacterBySourceConnection {
                    characterEntity = characterEntity
                });
                    
                // Destroy the RPC since we've processed it
                ecb.DestroyEntity(entity);
            }
        }
    }
}

[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
public partial struct ClientGameSetupSystem : ISystem
{
    [BurstCompile]
    void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = SystemAPI.GetSingletonRW<EndSimulationEntityCommandBufferSystem.Singleton>().ValueRW.CreateCommandBuffer(state.WorldUnmanaged);
        
        // Send a join request to the server if we haven't done so yet
        foreach (var (netId, entity) in SystemAPI.Query<NetworkId>().WithNone<NetworkStreamInGame>().WithEntityAccess())
        {
            // Mark our connection as ready to go in game
            ecb.AddComponent(entity, new NetworkStreamInGame()); 
            
            // Send an RPC that asks the server if we can join
            Entity joinRPC = ecb.CreateEntity();
            ecb.AddComponent(joinRPC, new ClientJoinRequest());
            ecb.AddComponent(joinRPC, new SendRpcCommandRequest { TargetConnection = entity });
        }
        
        // Handle initialization for our local character (mark main camera entity)
        foreach (var (character, entity) in SystemAPI.Query<FirstPersonCharacterComponent>().WithAll<GhostOwnerIsLocal>().WithNone<LocalInitialized>().WithEntityAccess())
        {
            ecb.AddComponent(character.ViewEntity, new MainEntityCamera());
            ecb.AddComponent(character.ViewEntity, new OverlayEntityCamera());
            ecb.AddComponent(entity, new LocalInitialized());
        }
    }
}
