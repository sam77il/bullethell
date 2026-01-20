using System.Collections;
using System.Collections.Generic;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using UnityEngine;
using UnityEngine.tvOS;

public class MyServerManager : NetworkBehaviour
{
    public static MyServerManager Instance { get; private set; }

    public readonly SyncVar<int> ConnectedPlayers = new SyncVar<int>(0);

    private void Awake()
    {
        // Singleton Pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (Instance == null)
        {
            Instance = this;
        }

        if (ServerManager != null)
        {
            ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
        }
    }

    private void OnRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args)
    {
        if (args.ConnectionState == RemoteConnectionState.Started)
        {
            ConnectedPlayers.Value++;

            StartCoroutine(WaitForPlayerSpawn(conn));
        }
    }

    private IEnumerator WaitForPlayerSpawn(NetworkConnection conn)
    {
        yield return new WaitForSeconds(1f); // Warte eine Sekunde, um sicherzustellen, dass der Spieler gespawnt ist

        foreach (var networkObject in conn.Objects)
        {
            if (networkObject.TryGetComponent<Player>(out Player player))
            {
                player.OnRegisteredTargetRpc(conn);
                break;
            }
        }
    }
}
