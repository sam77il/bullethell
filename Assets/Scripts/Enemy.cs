using FishNet.Object;
using UnityEngine;

public class EnemyAI : NetworkBehaviour
{
    public float speed = 3f;
    public float targetUpdateInterval = 0.5f; // Alle 0.5 Sekunden aktualisieren
    public float minDistanceToOtherEnemies = 1.2f; // Abstand zu anderen Enemies

    private NetworkObject targetPlayer;
    private float targetUpdateTimer = 0f;

    public override void OnStartServer()
    {
        base.OnStartServer();
        // Nächsten Spieler auswählen
        FindClosestPlayer();
    }

    void FindClosestPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        if (players.Length == 0)
            return;

        float closestDistance = float.MaxValue;
        foreach (GameObject player in players)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                targetPlayer = player.GetComponent<NetworkObject>();
            }
        }
    }

    void Update()
    {
        // Nur Server führt Bewegung aus
        if (!IsServerInitialized)
            return;

        // Regelmäßig nächsten Spieler aktualisieren
        targetUpdateTimer -= Time.deltaTime;
        if (targetUpdateTimer <= 0f)
        {
            FindClosestPlayer();
            targetUpdateTimer = targetUpdateInterval;
        }

        if (targetPlayer == null)
            return;

        Vector3 direction = (targetPlayer.transform.position - transform.position).normalized;
        Vector3 newPos = transform.position + direction * speed * Time.deltaTime;
        newPos.y = 0f;
        transform.position = newPos;

        // Body blocke andere Enemies
        BodyBlockOtherEnemies();

        // Position zu allen Clients synchronisieren
        UpdateEnemyPosition(transform.position);
    }

    bool IsTooCloseToOtherEnemies()
    {
        EnemyAI[] allEnemies = FindObjectsOfType<EnemyAI>();
        foreach (EnemyAI enemy in allEnemies)
        {
            if (enemy == this)
                continue;

            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance < minDistanceToOtherEnemies)
            {
                return true;
            }
        }
        return false;
    }

    void BodyBlockOtherEnemies()
    {
        EnemyAI[] allEnemies = FindObjectsOfType<EnemyAI>();
        foreach (EnemyAI enemy in allEnemies)
        {
            if (enemy == this)
                continue;

            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance < minDistanceToOtherEnemies && distance > 0.01f)
            {
                // Push enemy away
                Vector3 awayFromMe = (enemy.transform.position - transform.position).normalized;
                Vector3 newPos = transform.position + awayFromMe * minDistanceToOtherEnemies;
                newPos.y = 0f;
                enemy.transform.position = newPos;
            }
        }
    }

    [ObserversRpc]
    void UpdateEnemyPosition(Vector3 pos)
    {
        if (!IsServerInitialized)
            transform.position = pos;
    }
}
