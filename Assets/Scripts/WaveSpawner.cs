using FishNet.Object;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class WaveSpawner : NetworkBehaviour
{
    public static WaveSpawner Instance;

    public GameObject enemyPrefab;
    public int initialEnemiesPerWave = 5; // Maximale Anzahl der ersten Wave
    private int enemiesPerWave; // Wird während des Spiels erhöht
    public float timeBetweenSpawns = 0.5f;
    public float timeBetweenWaves = 3f;
    public float spawnRadius = 10f;

    private bool started = false;
    private List<EnemyAI> currentWaveEnemies = new List<EnemyAI>();

    void Awake()
    {
        Instance = this;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        // WARTET jetzt auf Player → nichts starten!
    }

    // Wird vom Player aufgerufen
    [Server]
    public void NotifyPlayerSpawned()
    {
        if (started)
            return;

        started = true;
        enemiesPerWave = initialEnemiesPerWave; // Setze die erste Wave
        StartCoroutine(SpawnWaves());
    }

    IEnumerator SpawnWaves()
    {
        while (true)
        {
            currentWaveEnemies.Clear();

            // Alle Gegner dieser Wave spawnen
            for (int i = 0; i < enemiesPerWave; i++)
            {
                SpawnEnemy();
                yield return new WaitForSeconds(timeBetweenSpawns);
            }

            // Warten bis alle Gegner dieser Wave zerstört sind
            while (currentWaveEnemies.Count > 0)
            {
                // Entferne null-einträge (zerstörte Gegner)
                currentWaveEnemies.RemoveAll(enemy => enemy == null);
                yield return new WaitForSeconds(0.5f);
            }

            // Wave abgeschlossen, zur nächsten Wave übergehen
            enemiesPerWave += 2;
            yield return new WaitForSeconds(timeBetweenWaves);
        }
    }

    void SpawnEnemy()
    {
        Vector3 pos = transform.position + Random.insideUnitSphere * spawnRadius;
        pos.y = 0f;

        GameObject enemy = Instantiate(enemyPrefab, pos, Quaternion.identity);
        NetworkObject netObj = enemy.GetComponent<NetworkObject>();

        if (netObj != null)
        {
            base.NetworkManager.ServerManager.Spawn(netObj);

            EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
            if (enemyAI != null)
            {
                currentWaveEnemies.Add(enemyAI);
            }
        }
        else
        {
            Debug.LogError("Enemy prefab hat kein NetworkObject!");
        }
    }
}
