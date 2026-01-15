using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance;

    [SerializeField]
    private Button findGamesButton;
    [SerializeField]
    private Button createGameButton;
    [SerializeField]
    private Button goBackButton;
    [SerializeField]
    private Button submitCreateGameButton;
    [SerializeField]
    private GameObject actionListPanel;
    [SerializeField]
    private GameObject createGamePanel;
    [SerializeField]
    private TMPro.TMP_InputField gameNameInput;
    [SerializeField]
    private TMPro.TMP_InputField gamePasswordInput;

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
    }

    private void Start()
    {
        findGamesButton.onClick.AddListener(OnFindGamesButtonClicked);
        createGameButton.onClick.AddListener(() =>
        {
            actionListPanel.SetActive(false);
            createGamePanel.SetActive(true);
        });
        goBackButton.onClick.AddListener(() =>
        {
            actionListPanel.SetActive(true);
            createGamePanel.SetActive(false);
        });
        submitCreateGameButton.onClick.AddListener(() =>
        {
            string gameName = gameNameInput.text;
            string gamePassword = gamePasswordInput.text;
            Debug.Log($"Creating game: {gameName} with password: {gamePassword}");
            // Implement game creation logic here
        });
    }

    private void OnFindGamesButtonClicked()
    {
        StartCoroutine(FetchAvailableGames());
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
            Debug.Log($"Game found - IP: {game.ip}, Port: {game.port}, Players: {game.players}");
        }
    }
}
