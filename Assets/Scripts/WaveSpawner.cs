using FishNet.Object;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class WaveSpawner : NetworkBehaviour
{
    public static WaveSpawner Instance;

    public GameObject enemyPrefab;
    public float timeBetweenSpawns = 0.5f;
    public float timeBetweenWaves = 3f;
    public float spawnRadius = 10f;

    private bool started = false;
    private List<EnemyAI> currentWaveEnemies = new List<EnemyAI>();
    private int currentWave = 0;

    // Wave-Konfiguration: Anzahl der Gegner pro Wave
    private int[] waveEnemyCounts = { 5, 10, 10 }; // 3 Waves

    void Awake()
    {
        Instance = this;
    }

    // Wird vom Player aufgerufen
    [Server]
    public void NotifyPlayerSpawned()
    {
        if (started)
            return;

        started = true;
        StartCoroutine(SpawnWaves());
    }

    IEnumerator SpawnWaves()
    {
        for (currentWave = 0; currentWave < waveEnemyCounts.Length; currentWave++)
        {
            currentWaveEnemies.Clear();
            Debug.Log($"Wave {currentWave + 1} started! Enemy count: {waveEnemyCounts[currentWave]}");

            // Spawne normale Gegner dieser Wave
            int enemyCount = waveEnemyCounts[currentWave];
            for (int i = 0; i < enemyCount; i++)
            {
                SpawnEnemy(isBoss: false);
                yield return new WaitForSeconds(timeBetweenSpawns);
            }

            // Spawne Boss nur in der letzten Wave
            if (currentWave == waveEnemyCounts.Length - 1)
            {
                Debug.Log("Boss spawning!");
                SpawnEnemy(isBoss: true);
                yield return new WaitForSeconds(timeBetweenSpawns);
            }

            // Warten bis alle Gegner dieser Wave zerstört sind
            while (currentWaveEnemies.Count > 0)
            {
                // Entferne null-einträge (zerstörte Gegner)
                currentWaveEnemies.RemoveAll(enemy => enemy == null);
                yield return new WaitForSeconds(0.5f);
            }

            // Wave abgeschlossen
            if (currentWave < waveEnemyCounts.Length - 1)
            {
                Debug.Log("Wave completed! Next wave in 3 seconds...");
                yield return new WaitForSeconds(timeBetweenWaves);
            }
        }

        // Get all players
        Player[] players = FindObjectsByType<Player>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            player.RpcGameWon();
        }
        // Alle Waves abgeschlossen
        Debug.Log("All waves completed! Game won!");
    }
    

    void SpawnEnemy(bool isBoss)
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
                if (isBoss)
                {
                    // Boss-Modifikationen
                    enemyAI.transform.localScale *= 2f; // Doppelt so groß
                    enemyAI.Health *= 2f; // Doppelt so viel Leben
                    Debug.Log("Boss spawned with 2x size and 2x health!");
                }

                currentWaveEnemies.Add(enemyAI);
            }
        }
        else
        {
            Debug.LogError("Enemy prefab hat kein NetworkObject!");
        }
    }
}
