using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FishNet;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;

public class MyServerManager : NetworkBehaviour
{
    public static MyServerManager Instance { get; private set; }
    private SceneManager sceneManager;

    public readonly SyncList<PlayerData> PlayerList = new SyncList<PlayerData>();
    private string gamePassword = "";

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
        sceneManager = FindFirstObjectByType<SceneManager>();
    }

    private void OnPlayerListChanged(SyncListOperation op, int index, PlayerData oldItem, PlayerData newItem, bool asServer)
    {
        // Debug.Log("PlayerList changed");
    }

    [Server]
    public void AddPlayer(Player player, string playerName)
    {
        if (PlayerList.Count >= 2)
        {
            player.Disconnect(player.Owner);
            return;
        }
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
                
        foreach (var player in PlayerList)
        {
            player.Player.UpdateLobby(player.Player.Owner, PlayerList.ToList());
        }
        StartCoroutine(UpdateServerPlayerCount());
    }

    [Server]
    public IEnumerator UpdateServerPlayerCount()
    {
        int port = InstanceFinder.TransportManager.Transport.GetPort();
        string url = "localhost:3000/" + port;
        WWWForm form = new WWWForm();
        form.AddField("players", PlayerList.Count.ToString());

        UnityWebRequest request = UnityWebRequest.Post(url, form);
        yield return request.SendWebRequest();
    }

    [ServerRpc(RequireOwnership = false)]
    public void CheckPassword(Player player, string password)
    {
        StartCoroutine(CheckPasswordRoutine(player, password));
    }

    private IEnumerator CheckPasswordRoutine(Player player, string password)
    {
        // Passwort vom Broadcast holen
        yield return StartCoroutine(FetchPasswordFromBroadcast());

        // Optionales Delay (z.B. 0.5 Sekunden)
        yield return new WaitForSeconds(0.5f);

        Debug.Log("Checking password... " + password + " against '" + gamePassword + "'");

        if (password == gamePassword)
        {
            Debug.Log("Password correct. Allowing connection.");
        }
        else
        {
            Debug.Log("Incorrect password. Disconnecting client.");
            // Client trennen
            LeaveLobby(player);
        }
    }

    private IEnumerator FetchPasswordFromBroadcast()
    {
        int port = InstanceFinder.TransportManager.Transport.GetPort();
        string url = "localhost:3000/" + port;
        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error fetching game password: " + request.error);
            yield break;
        }
        string json = request.downloadHandler.text;

        Game gameInfo = JsonUtility.FromJson<Game>(json);
        gamePassword = gameInfo.password;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (Instance == null)
        {
            Instance = this;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetGamePassword(string password)
    {
        gamePassword = password;
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
            StartCoroutine(CloseGameInBroadcast());
        }
    }

    [Server]
    private IEnumerator CloseGameInBroadcast()
    {
        int port = InstanceFinder.TransportManager.Transport.GetPort();
        string url = "localhost:3000/" + port;
        UnityWebRequest request = UnityWebRequest.Delete(url);
        yield return request.SendWebRequest();
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

    [Server]
    public void StartGame()
    {
        SceneLookupData lookupData = new SceneLookupData("Furkan");
        SceneLoadData loadData = new SceneLoadData(lookupData);

        loadData.ReplaceScenes = ReplaceOption.All;

        InstanceFinder.SceneManager.LoadGlobalScenes(loadData);
    }
}
