/*using Unity.Entities;
using Unity.NetCode;
using Unity.Scenes;

public class AutoConnectBootstrap : ClientServerBootstrap
{
    public override bool Initialize(string defaultWorldName)
    {
        AutoConnectPort = 7979;
        CreateDefaultClientServerWorlds();
        return true;
    }
}*/

using Unity.NetCode;
using UnityEngine.Scripting;

[Preserve]
public class AutoConnectBootstrap : ClientServerBootstrap
{
    public override bool Initialize(string defaultWorldName)
    {
        AutoConnectPort = 0;
        return base.Initialize(defaultWorldName);
    }
}
