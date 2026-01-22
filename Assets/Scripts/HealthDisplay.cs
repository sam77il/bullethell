using UnityEngine;
using TMPro;

public class HealthDisplay : MonoBehaviour
{
    public TextMeshProUGUI healthText;
    private Player localPlayer;

    void Start()
    {
        // Text am Anfang ausblenden
        if (healthText != null)
            healthText.enabled = false;

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

        // Aktualisiere Health-Anzeige
        UpdateHealthDisplay();
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
                Debug.Log("Local player found for health display!");

                // Text einblenden, sobald Spieler gefunden
                if (healthText != null)
                    healthText.enabled = true;

                break;
            }
        }
    }

    void UpdateHealthDisplay()
    {
        if (healthText != null && localPlayer != null)
        {
            healthText.text = $"Health: {Mathf.Max(0, localPlayer.Health):F0}";

            // Optional: Farbe ändern je nach Health
            if (localPlayer.Health > 60)
                healthText.color = Color.green;
            else if (localPlayer.Health > 30)
                healthText.color = Color.yellow;
            else
                healthText.color = Color.red;
        }
    }
}
