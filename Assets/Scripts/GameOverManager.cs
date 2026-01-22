using UnityEngine;
using TMPro;

public class GameOverManager : MonoBehaviour
{
    public GameObject gameOverPanel; // Schwarzes Panel
    public TextMeshProUGUI gameOverText; // "Game Over" Text
    public TextMeshProUGUI killsText; // Zeigt finale Kills an

    private static GameOverManager instance;
    private Player localPlayer;

    public static GameOverManager Instance
    {
        get { return instance; }
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Game Over Screen am Anfang ausblenden
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        FindLocalPlayer();
    }

    void Update()
    {
        // Falls lokaler Spieler noch nicht gefunden
        if (localPlayer == null)
        {
            FindLocalPlayer();
            return;
        }

        // Prüfe ob Spieler gestorben ist
        if (localPlayer.Health <= 0 && gameOverPanel != null && !gameOverPanel.activeSelf)
        {
            ShowGameOver();
        }
    }

    void FindLocalPlayer()
    {
        Player[] players = FindObjectsByType<Player>(FindObjectsSortMode.None);

        foreach (Player player in players)
        {
            if (player.IsOwner)
            {
                localPlayer = player;
                break;
            }
        }
    }

    public void ShowGameOver()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        // Zeige finale Kills an
        if (killsText != null && localPlayer != null)
        {
            killsText.text = $"Final Kills: {localPlayer.Kills}";
        }

        // Optional: Cursor freigeben
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("Game Over!");
    }
}
