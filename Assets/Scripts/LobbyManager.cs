using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using FishNet.Managing;
using FishNet.Transporting;
using System.Collections.Generic;
using FishNet.Demo.AdditiveScenes;
using FishNet.Object.Synchronizing;
using FishNet;
using System.Threading.Tasks;
using UnityEditor.PackageManager;

public class LobbyManager : MonoBehaviour
{
    public Game connectedGame { get; private set; }
    private NetworkManager networkManager;

    public string enteredName = "";
    public int ClientId;

    [SerializeField] private Button findGamesButton;
    [SerializeField] private Button createGameButton;
    [SerializeField] private Button goBackCreateGameButton;
    [SerializeField] private Button goBackFindGamesButton;
    [SerializeField] private Button submitCreateGameButton;
    [SerializeField] private Button enterNameQuitButton;
    [SerializeField] private Button enterNameSubmitButton;
    [SerializeField] private Button leaveLobbyButton;
    [SerializeField] private Button startGameButton;

    [SerializeField] private GameObject actionListPanel;
    [SerializeField] public GameObject createGamePanel;
    [SerializeField] public GameObject lobbyPanel;
    [SerializeField] public GameObject findGamesPanel;
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
            enteredName = enterNameInput.text;
            Debug.Log("Entered Name: " + enteredName);
            namePanel.SetActive(false);
            actionListPanel.SetActive(true);
        });
    }

    public void LeaveLobby()
    {
        Player localPlayer = FindLocalPlayer(ClientId);
        if (ClientId == 0)
        {
            localPlayer.RequestServerClose();
        } else
        {
            localPlayer.LeaveLobby();
        }
    }

    private Player FindLocalPlayer(int theId)
    {
        Player[] players = FindObjectsByType<Player>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            if (player.Owner.ClientId == theId)
            {
                return player;
            }
        }
        return null;
    }

    public void GoToLobby()
    {
        lobbyPanel.SetActive(false);
        actionListPanel.SetActive(true);
    }

    private async void CreateGame()
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
    }

    public void RefreshLobbyUI(List<PlayerData> playersData)
    {
        foreach (Transform child in lobbyPlayerListContainer)
        {
            Destroy(child.gameObject);
        }
                
        foreach (var playerData in playersData)
        {
            GameObject playerItem = Instantiate(lobbyPlayerItemPrefab, lobbyPlayerListContainer);
            LobbyPlayerItem itemScript = playerItem.GetComponent<LobbyPlayerItem>();
                        
            if (ClientId == 0 && playerData.ClientId != ClientId)
            {
                itemScript.SetPlayerInfo(playerData.PlayerName, true, () => KickPlayer(playerData.ClientId));
            }
            else
            {
                itemScript.SetPlayerInfo(playerData.PlayerName, false, () => KickPlayer(playerData.ClientId));
            }

            Debug.Log("Players in Server: " + playerData.PlayerName + " (ID: " + playerData.ClientId + ")");
        }

        if (ClientId == 0)
        {
            startGameButton.gameObject.SetActive(true);
        } else
        {
            startGameButton.gameObject.SetActive(false);
        }
    }

    private void KickPlayer(int cId)
    {
        if (ClientId == 0)
        {
            Player player = FindLocalPlayer(cId);

            player.LeaveLobby();
        }
    }
}
