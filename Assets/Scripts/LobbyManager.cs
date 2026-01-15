using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using FishNet.Managing;
using FishNet.Object;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance;
    public Game connectedGame { get; private set; }
    public MyPlayer localPlayer { get; private set; } = new MyPlayer();

    private NetworkManager _networkManager;

    [SerializeField]
    private Button findGamesButton;

    [SerializeField]
    private Button createGameButton;

    [SerializeField]
    private Button goBackCreateGameButton;
    
    [SerializeField]
    private Button goBackFindGamesButton;

    [SerializeField]
    private Button submitCreateGameButton;

    [SerializeField]
    private Button enterNameQuitButton;

    [SerializeField]
    private Button enterNameSubmitButton;


    [SerializeField]
    private Button leaveLobbyButton;

    [SerializeField]
    private GameObject actionListPanel;

    [SerializeField]
    private GameObject createGamePanel;

    [SerializeField]
    private GameObject lobbyPanel;

    [SerializeField]
    private GameObject findGamesPanel;

    [SerializeField]
    private GameObject namePanel;

    [SerializeField]
    private TMPro.TMP_InputField gameNameInput;

    [SerializeField]
    private TMPro.TMP_InputField gamePasswordInput;

    [SerializeField]
    private TMPro.TMP_InputField enterNameInput;

    [SerializeField]
    private Transform gamesListContainer;

    [SerializeField]
    private GameObject findGameItemPrefab;

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
    }

    private void LeaveLobby()
    {
        lobbyPanel.SetActive(false);
        actionListPanel.SetActive(true);
        connectedGame = null;
    }

    private void CreateGame()
    {
        string gameName = gameNameInput.text;
        string gamePassword = gamePasswordInput.text;

        StartCoroutine(CreateGameOnServer(gameName, gamePassword));

        gameNameInput.text = "";
        gamePasswordInput.text = "";
        ushort port = (ushort)Random.Range(1000, 9999);
        _networkManager.TransportManager.Transport.SetPort(port);

        _networkManager.ServerManager.StartConnection();
        _networkManager.ClientManager.StartConnection();
        connectedGame = new Game
        {
            name = gameName,
            port = port,
            password = gamePassword,
            players = _networkManager.ServerManager.Clients.Count
        };

        lobbyPanel.SetActive(true);
        createGamePanel.SetActive(false);
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
    }

    private void OnFindGamesButtonClicked()
    {
        actionListPanel.SetActive(false);
        findGamesPanel.SetActive(true);
        StartCoroutine(FetchAvailableGames());
    }

    IEnumerator CreateGameOnServer(string name, string password)
    {
        string url = "localhost:3000";
        // random 4 digit port for example purposes
        int port = Random.Range(1000, 9999);
        WWWForm form = new WWWForm();
        form.AddField("name", name);
        form.AddField("port", port.ToString());
        form.AddField("players", "1");
        form.AddField("password", password);

        UnityWebRequest request = UnityWebRequest.Post(url, form);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error creating game: " + request.error);
        }
        else
        {
            Debug.Log("Game created successfully!");
        }
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
        Debug.Log("Received game data: " + json);

        GameList gameList = JsonUtility.FromJson<GameList>(json);
        foreach (Game game in gameList.games)
        {
            Debug.Log($"Game: {game.name}, Port: {game.port}, Players: {game.players}");
        }
        LoadGamesIntoList(gameList);
    }
}
