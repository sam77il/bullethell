using System.Collections.Generic;
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerNetwork : NetworkBehaviour
{
    private LobbyManager lobbyManager;
    public readonly SyncList<LobbyPlayerData> Players = new SyncList<LobbyPlayerData>(new SyncTypeSettings(1f));

    private void Awake()
    {
        Players.OnChange += OnPlayersChanged;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        UpdatePlayerCount();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        // LobbyManager für ALLE Clients finden, nicht nur für Owner
        lobbyManager = FindFirstObjectByType<LobbyManager>();
            Debug.Log(Owner.ClientId + " " + Owner.CustomData);

        SetNameServerRpc(lobbyManager.localPlayer.name);

        // Initiales Refresh für neue Spieler
        if (lobbyManager != null)
        {
            LobbyPlayerData data = new LobbyPlayerData
            {
                ClientId = Owner.ClientId,
                NetworkObjectId = ObjectId,
                PlayerName = "Player_" + Owner.ClientId
            };
            Players.Add(data);
        }
        test();
    }

    private void OnPlayersChanged(SyncListOperation op, int index, LobbyPlayerData oldItem, LobbyPlayerData newItem, bool asServer)
    {
        if (lobbyManager == null)
        {   
            lobbyManager = FindFirstObjectByType<LobbyManager>();
        }

        lobbyManager?.OnPlayerDataChanged(Players);
    }

    [ServerRpc(RequireOwnership = false)]
    private void test()
    {
        check();
    }

    [Server]
    private void check()
    {
        Debug.Log("aaaaaaa " + InstanceFinder.ServerManager.Clients.Count);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetNameServerRpc(string name)
    {
        Debug.Log("Setting player name to: " + name);
        // PlayerName.Value = name;
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        
        // Spielerliste aktualisieren wenn ein Spieler disconnected
        if (lobbyManager != null)
        {
            // lobbyManager.RefreshLobbyPlayers();
        }
        lobbyManager.UnregisterPlayer(this);
    }


    public override void OnStopServer()
    {
        base.OnStopServer();

        UpdatePlayerCount();
    }

    [Server]
    private void UpdatePlayerCount()
    {
        int playerCount = ServerManager.Clients.Count;
        Debug.Log("Current player count: " + playerCount);

        UpdatePlayerCountObserver(playerCount);
    }

    [ObserversRpc]
    private void UpdatePlayerCountObserver(int playerCount)
    {
        Debug.Log("Updated local player count to: " + playerCount);
    }
}
