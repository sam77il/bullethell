using FishNet.Object;
using UnityEngine;

public class EnemyAI : NetworkBehaviour
{
    public float speed = 3f;
    public float targetUpdateInterval = 0.5f; // Alle 0.5 Sekunden aktualisieren
    public float minDistanceToOtherEnemies = 1.2f; // Abstand zu anderen Enemies
    public float Health = 100f;
    public float damageInterval = 0.2f; // Schaden alle 0.2 Sekunden

    private NetworkObject targetPlayer;
    private float targetUpdateTimer = 0f;
    private Player contactPlayer = null;
    private float damageTimer = 0f;

    // Server-Methode, um Schaden zu nehmen
    [ServerRpc(RequireOwnership = false)]
    public void TakeDamage(float damage, Player attacker = null)
    {
        Health -= damage;
        Debug.Log($"Enemy took {damage} damage! Health: {Health}");

        if (Health <= 0)
        {
            Debug.Log("Enemy defeated - destroying!");

            // Gib dem Spieler einen Kill
            if (attacker != null)
            {
                attacker.AddKill();
            }

            Destroy(gameObject);
        }
    }

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

        // Schaden-Timer wenn Spieler in Kontakt ist
        if (contactPlayer != null)
        {
            damageTimer -= Time.deltaTime;
            if (damageTimer <= 0f)
            {
                // Prüfe Spawnschutz bevor Schaden verursacht wird
                if (Time.time - contactPlayer.GetSpawnTime() >= contactPlayer.spawnProtectionTime)
                {
                    contactPlayer.TakeDamage(20f);
                }
                damageTimer = damageInterval;
            }
        }

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
        EnemyAI[] allEnemies = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
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
        EnemyAI[] allEnemies = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
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

    void OnTriggerEnter(Collider other)
    {
        // Nur Server führt Damage aus
        if (!IsServerInitialized)
            return;

        if (other.CompareTag("Player"))
        {
            Player player = other.GetComponent<Player>();
            if (player != null)
            {
                contactPlayer = player;
                damageTimer = damageInterval; // Warte erst ein Intervall, bevor Schaden kommt
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        // Nur Server
        if (!IsServerInitialized)
            return;

        if (other.CompareTag("Player"))
        {
            contactPlayer = null;
        }
    }
}
