using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using FishNet.Managing;
using FishNet.Transporting;
using System.Collections.Generic;
using FishNet.Demo.AdditiveScenes;
using FishNet.Object.Synchronizing;

public class LobbyManager : MonoBehaviour
{
    public Game connectedGame { get; private set; }
    public MyPlayer localPlayer { get; private set; } = new MyPlayer();

    private NetworkManager networkManager;
    private List<PlayerNetwork> registeredPlayers = new List<PlayerNetwork>();

    [SerializeField] private Button findGamesButton;
    [SerializeField] private Button createGameButton;
    [SerializeField] private Button goBackCreateGameButton;
    [SerializeField] private Button goBackFindGamesButton;
    [SerializeField] private Button submitCreateGameButton;
    [SerializeField] private Button enterNameQuitButton;
    [SerializeField] private Button enterNameSubmitButton;
    [SerializeField] private Button leaveLobbyButton;

    [SerializeField] private GameObject actionListPanel;
    [SerializeField] private GameObject createGamePanel;
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private GameObject findGamesPanel;
    [SerializeField] private GameObject namePanel;

    [SerializeField] private TMPro.TMP_InputField gameNameInput;
    [SerializeField] private TMPro.TMP_InputField gamePasswordInput;
    [SerializeField] private TMPro.TMP_InputField enterNameInput;
    [SerializeField] private Transform gamesListContainer;
    [SerializeField] private GameObject findGameItemPrefab;
    [SerializeField] private Transform lobbyPlayerListContainer;
    [SerializeField] private GameObject lobbyPlayerItemPrefab;

    private void Awake()
    {
        networkManager = FindFirstObjectByType<NetworkManager>();
    }

    private void Start()
    {
        actionListPanel.SetActive(false);
        createGamePanel.SetActive(false);
        lobbyPanel.SetActive(false);
        findGamesPanel.SetActive(false);
        namePanel.SetActive(true);
        
        findGamesButton.onClick.AddListener(OnFindGamesButtonClicked);
        createGameButton.onClick.AddListener(() =>
        {
            actionListPanel.SetActive(false);
            createGamePanel.SetActive(true);
        });
        goBackCreateGameButton.onClick.AddListener(() =>
        {
            actionListPanel.SetActive(true);
            createGamePanel.SetActive(false);
        });
        goBackFindGamesButton.onClick.AddListener(() =>
        {
            actionListPanel.SetActive(true);
            findGamesPanel.SetActive(false);
        });
        submitCreateGameButton.onClick.AddListener(CreateGame);
        leaveLobbyButton.onClick.AddListener(LeaveLobby);

        enterNameSubmitButton.onClick.AddListener(() =>
        {
            namePanel.SetActive(false);
            actionListPanel.SetActive(true);

            localPlayer.name = enterNameInput.text;            
        });

        networkManager.ServerManager.OnServerConnectionState += OnServerConnectionState;
        networkManager.ClientManager.OnClientConnectionState += OnClientConnectionState;
    }

    private void OnDestroy()
    {
        if (networkManager != null)
        {
            networkManager.ServerManager.OnServerConnectionState -= OnServerConnectionState;
            networkManager.ClientManager.OnClientConnectionState -= OnClientConnectionState;
        }
    }

    private void OnServerConnectionState(ServerConnectionStateArgs args)
    {
    }

    private void OnClientConnectionState(ClientConnectionStateArgs args)
    {        
        if (args.ConnectionState == LocalConnectionState.Started)
        {
            // Erfolgreich verbunden
            lobbyPanel.SetActive(true);
            findGamesPanel.SetActive(false);
        }
        else if (args.ConnectionState == LocalConnectionState.Stopped)
        {
            Debug.LogWarning("Connection failed or disconnected");
        }
    }

    public void RegisterPlayer(PlayerNetwork player)
    {
        if (!registeredPlayers.Contains(player))
        {
            registeredPlayers.Add(player);
            RefreshLobbyPlayers();
        }
    }

    public void UnregisterPlayer(PlayerNetwork player)
    {
        if (registeredPlayers.Remove(player))
        {
            RefreshLobbyPlayers();
        }
    }

    public void LeaveLobby()
    {
        lobbyPanel.SetActive(false);
        actionListPanel.SetActive(true);
        connectedGame = null;
    }

    private void CreateGame()
    {
        string gameName = gameNameInput.text;
        string gamePassword = gamePasswordInput.text;
        ushort port = (ushort)Random.Range(1000, 9999);

        StartCoroutine(CreateGameOnServer(gameName, gamePassword, port));

        gameNameInput.text = "";
        gamePasswordInput.text = "";
        networkManager.TransportManager.Transport.SetPort(port);

        networkManager.ServerManager.StartConnection();
        networkManager.ClientManager.StartConnection();
        connectedGame = new Game
        {
            name = gameName,
            port = port,
            password = gamePassword,
            players = networkManager.ServerManager.Clients.Count
        };

        lobbyPanel.SetActive(true);
        createGamePanel.SetActive(false);
        Debug.Log("Created and connected to game: " + port + " - " + gameName);
    }

    private void LoadGamesIntoList(GameList gameList)
    {
        foreach (Transform child in gamesListContainer)
        {
            Destroy(child.gameObject);
        }

        // Example data for demonstration purposes
        foreach (Game game in gameList.games)
        {
            InitFindGamesItem(game.name, game.port, game.players);
        }
    }

    private void InitFindGamesItem(string name, int port, int players)
    {
        GameObject gameItem = Instantiate(findGameItemPrefab, gamesListContainer);
        FindGameItem itemScript = gameItem.GetComponent<FindGameItem>();
        itemScript.SetGameInfo(name, port, players);
        gameItem.GetComponent<Button>().onClick.AddListener(() =>
        {
            ConnectToGame(port, name);
        });
    }

    private void OnFindGamesButtonClicked()
    {
        actionListPanel.SetActive(false);
        findGamesPanel.SetActive(true);
        StartCoroutine(FetchAvailableGames());
    }

    IEnumerator CreateGameOnServer(string name, string password, ushort port)
    {
        string url = "localhost:3000";
        WWWForm form = new WWWForm();
        form.AddField("name", name);
        form.AddField("port", port.ToString());
        form.AddField("players", "1");
        form.AddField("password", password);

        UnityWebRequest request = UnityWebRequest.Post(url, form);
        yield return request.SendWebRequest();
    }

    IEnumerator FetchAvailableGames()
    {
        string url = "localhost:3000";
        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error fetching games: " + request.error);
            yield break;
        }
        string json = request.downloadHandler.text;

        GameList gameList = JsonUtility.FromJson<GameList>(json);
        LoadGamesIntoList(gameList);
    }

    public void ConnectToGame(int port, string name)
    {
        networkManager.TransportManager.Transport.SetPort((ushort)port);
        networkManager.ClientManager.StartConnection();
        Debug.Log("Connecting to game: port " + port + " - " + name);
    }

    public void OnPlayerDataChanged(IReadOnlyList<LobbyPlayerData> players)
    {
        // RefreshLobbyPlayers();
        Debug.Log(players.Count);
    }

    private void RefreshLobbyPlayers()
    {
        Debug.Log(registeredPlayers.Count + " players in lobby.");
        foreach (Transform child in lobbyPlayerListContainer)
            Destroy(child.gameObject);

        // Nutze die registrierte Liste statt FindObjectsByType
        foreach (PlayerNetwork player in registeredPlayers)
        {
            if (player != null)
            {
                GameObject item = Instantiate(lobbyPlayerItemPrefab, lobbyPlayerListContainer);
                LobbyPlayerItem itemScript = item.GetComponent<LobbyPlayerItem>();
                bool isHost = player.IsHostStarted;
                // itemScript.SetPlayerInfo(player.PlayerName.Value, isHost);
            }
        }
    }
}
