using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using Unity.VisualScripting;
using UnityEngine;

public class MyServerManager : NetworkBehaviour
{
    public static MyServerManager Instance { get; private set; }

    public readonly SyncVar<int> ConnectedPlayers = new SyncVar<int>(0);
    public readonly SyncList<PlayerData> PlayerList = new SyncList<PlayerData>();

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

        PlayerList.OnChange += OnPlayerListChanged;
    }

    private void OnPlayerListChanged(SyncListOperation op, int index, PlayerData oldItem, PlayerData newItem, bool asServer)
    {
        // Debug.Log("PlayerList changed");
    }

    [Server]
    public void AddPlayer(Player player, string playerName)
    {
        PlayerData playerData = new PlayerData
        {
            ClientId = player.Owner.ClientId,
            PlayerName = playerName,
            Player = player
        };

        if (!PlayerList.Contains(playerData))
        {
            PlayerList.Add(playerData);
            Debug.Log("Added player");
        }
        NotifyAllPlayers();
    }

    [Server]
    public async void NotifyAllPlayers()
    {
        await System.Threading.Tasks.Task.Delay(100);
        
        Debug.Log(Instance.PlayerList.Count + " players to notify.");
        
        List<PlayerData> playersData = new List<PlayerData>();
        
        foreach (var player in PlayerList)
        {
            player.Player.UpdateLobby(player.Player.Owner, PlayerList.ToList());
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (Instance == null)
        {
            Instance = this;
        }
    }

    [Server]
    public void CloseGameServerRpc()
    {
        Debug.Log("Closing game for all players");
        StartCoroutine(CloseGameCoroutine());
    }

    private IEnumerator CloseGameCoroutine()
    {
        List<PlayerData> playersToDisconnect = PlayerList.ToList();
        foreach (var playerData in playersToDisconnect)
        {
            if (playerData.Player != null)
            {
                playerData.Player.Disconnect(playerData.Player.Owner);
            }
        }
        
        PlayerList.Clear();
        
        yield return new WaitForSeconds(1f);
        
        Debug.Log($"Clients still connected: {InstanceFinder.ServerManager.Clients.Count}");
        
        if (InstanceFinder.ServerManager != null)
        {
            Debug.Log("Stopping server...");
            InstanceFinder.ServerManager.StopConnection(false);
        }
    }

    [Server]
    public void LeaveLobby(Player player)
    {
        PlayerData? myPlayer = PlayerList.Find(p => p.ClientId == player.Owner.ClientId);

        if (myPlayer.HasValue)
        {
            PlayerList.Remove(myPlayer.Value);
            myPlayer.Value.Player.Disconnect(myPlayer.Value.Player.Owner);
            NotifyAllPlayers();
        }
    }
}
