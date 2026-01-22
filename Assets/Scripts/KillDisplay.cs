using UnityEngine;
using TMPro;

public class KillDisplay : MonoBehaviour
{
    public TextMeshProUGUI killText;
    private Player localPlayer;

    void Start()
    {
        // Text am Anfang ausblenden
        if (killText != null)
            killText.enabled = false;

        // Finde den lokalen Spieler
        FindLocalPlayer();
    }

    void Update()
    {
        // Falls noch kein Spieler gefunden wurde, versuche erneut
        if (localPlayer == null)
        {
            FindLocalPlayer();
            return;
        }

        // Aktualisiere Kill-Anzeige
        UpdateKillDisplay();
    }

    void FindLocalPlayer()
    {
        // Finde alle Player-Objekte
        Player[] players = FindObjectsByType<Player>(FindObjectsSortMode.None);

        foreach (Player player in players)
        {
            // Nur der lokale Spieler (IsOwner)
            if (player.IsOwner)
            {
                localPlayer = player;
                Debug.Log("Local player found for kill display!");

                // Text einblenden, sobald Spieler gefunden
                if (killText != null)
                    killText.enabled = true;

                break;
            }
        }
    }

    void UpdateKillDisplay()
    {
        if (killText != null && localPlayer != null)
        {
            killText.text = $"Kills: {localPlayer.Kills}";

            // KillCounter anpassungen pro Kills
            if (localPlayer.Kills >= 25)
                killText.text = "Thanos";
            else if (localPlayer.Kills >= 5)
                killText.color = new Color(1f, 0.84f, 0f);
            else
                killText.color = Color.white;
        }
    }
}
