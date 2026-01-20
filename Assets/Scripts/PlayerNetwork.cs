using System.Collections.Generic;
using FishNet;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class ServerManager : NetworkBehaviour
{
    public static ServerManager Instance { get; private set; }

    public readonly SyncVar<int> ConnectedPlayers = new SyncVar<int>(0);

    [ServerRpc]
    public void RegisterPlayer()
    {
        ConnectedPlayers.Value += 1;
    }
}
