using UnityEngine;
using FishNet.Managing;
using FishNet.Transporting;
using System.Collections.Generic;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }
    private NetworkManager _networkManager;

    public class ServerInfo
    {
        public string serverName;
        public string password;
        public string ipAddress;
        public int port;
    }

    public List<ServerInfo> availableServers = new List<ServerInfo>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        _networkManager = FindFirstObjectByType<NetworkManager>();
    }

    public void CreateServer(string serverName, string password)
    {
        ServerInfo newServer = new ServerInfo
        {
            serverName = serverName,
            password = password,
            ipAddress = "localhost",
            port = 7777
        };

        availableServers.Add(newServer);

        _networkManager.ServerManager.StartConnection();
        _networkManager.ClientManager.StartConnection();

        Debug.Log($"Server '{serverName}' created and started.");
    }

    public void JoinServer(ServerInfo serverInfo, string enteredPassword)
    {
        if (serverInfo.password != enteredPassword)
        {
            Debug.LogError("Incorrect password. Cannot join server.");
            return;
        }
        _networkManager.ClientManager.StartConnection(serverInfo.ipAddress, (ushort)serverInfo.port);
        Debug.Log($"Joining server '{serverInfo.serverName}' at {serverInfo.ipAddress}:{serverInfo.port}");
    }

    public void StartGame(string sceneName)
    {
        if (!_networkManager.IsServerStarted)
        {
            Debug.LogError("Only the server can start the game.");
            return;
        }

        // _networkManager.SceneManager.LoadGlobalScenes(sceneName);
    }
}
