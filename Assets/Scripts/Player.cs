using UnityEngine;
using UnityEngine.InputSystem;
using FishNet.Object;
using FishNet.Connection;
using FishNet.Object.Synchronizing;
using System.Threading.Tasks;
using System.Collections.Generic;
using FishNet.Object.Synchronizing;

public class Player : NetworkBehaviour
{
    [Header("Movement")]
    public Camera playerCamera;
    public float maxSpeed = 5f;
    public float acceleration = 20f;
    public float deceleration = 25f;
    public float collisionDistance = 1.2f; // Abstand zu Enemies

    public float Health = 100f;
    public int Kills = 0; // Killcounter
    private bool isDead = false;

    public float spawnProtectionTime = 3f; // Spawnschutz in Sekunden
    private Vector2 moveInput;
    private Vector2 currentVelocity;
    private LobbyManager lobbyManager;
    public string PlayerName;
    private Vector3 lastValidPosition;
    private float spawnTime;

    // Öffentliche Methode, um Spawn-Zeit abzufragen
    public float GetSpawnTime()
    {
        return spawnTime;
    }

    // Dash
    private bool isDashing = false;
    private float dashSpeed = 25f;
    private float dashDuration = 0.2f;
    private float dashCooldown = 1f;
    private float lastDashTime = -10f;
    private Vector3 dashDirection;

    // Shooting
    public GameObject projectilePrefab;
    public float shootCooldown = 0.3f;
    private float lastShootTime = -10f;

    public void Move(InputAction.CallbackContext context)
    {
        if (!IsOwner || isDead)
            return;

        moveInput = context.ReadValue<Vector2>();
    }

    public void Dash(InputAction.CallbackContext context)
    {
        if (!IsOwner || isDead)
            return;

        if (context.performed && Time.time - lastDashTime >= dashCooldown)
        {
            if (moveInput.sqrMagnitude > 0.01f)
            {
                isDashing = true;
                dashDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
                lastDashTime = Time.time;
                Invoke(nameof(EndDash), dashDuration);
            }
        }
    }

    void EndDash()
    {
        isDashing = false;
    }

    public void Shoot(InputAction.CallbackContext context)
    {
        if (!IsOwner || isDead)
            return;

        if (context.performed && Time.time - lastShootTime >= shootCooldown)
        {
            lastShootTime = Time.time;

            if (projectilePrefab == null)
            {
                Debug.LogError("Projectile Prefab not assigned!");
                return;
            }

            // Raycast vom Player durch Mauszeiger
            Ray ray = playerCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            Vector3 shootDirection = ray.direction.normalized;

            // Entferne Y-Komponente - nur X und Z verwenden (2D)
            shootDirection.y = 0f;
            shootDirection = shootDirection.normalized;

            Vector3 spawnPos = transform.position + shootDirection * 0.2f;
            spawnPos.y = transform.position.y; // Gleiche Höhe wie Player

            // Spawne Projektil lokal
            GameObject projectile = Instantiate(projectilePrefab, spawnPos, Quaternion.LookRotation(shootDirection));

            // Setze Owner für Killcounter
            Projectile projectileScript = projectile.GetComponent<Projectile>();
            if (projectileScript != null)
            {
                projectileScript.SetOwner(this);
            }

            // Gib Velocity mit
            Rigidbody rb = projectile.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = shootDirection * 10f;
            }

            Debug.Log($"Shot fired in direction: {shootDirection}");
        }
    }

    [Server]
    public void TakeDamage(float damage)
    {
        // Spawnschutz prüfen
        if (Time.time - spawnTime < spawnProtectionTime)
        {
            Debug.Log("Spawn protection active - no damage taken!");
            return;
        }

        Health -= damage;
        Debug.Log($"Player took {damage} damage! Health: {Health}");

        // Synchronisiere Health zu allen Clients
        UpdateHealthRpc(Health);

        if (Health <= 0)
        {
            Debug.Log("Player died!");
            isDead = true;
            SetDeadRpc();
        }
    }

    [ObserversRpc]
    private void UpdateHealthRpc(float newHealth)
    {
        Health = newHealth;
    }

    [Server]
    public void AddKill()
    {
        Kills++;
        Debug.Log($"Player got a kill! Total kills: {Kills}");

        // Synchronisiere Kills zu allen Clients
        UpdateKillsRpc(Kills);
    }

    [ObserversRpc]
    private void UpdateKillsRpc(int newKills)
    {
        Kills = newKills;
    }

    [ObserversRpc]
    private void SetDeadRpc()
    {
        isDead = true;

        // Optional: Visuelles Feedback für Tod
        Renderer renderer = GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            Color color = renderer.material.color;
            color.a = 0.5f; // Halbtransparent
            renderer.material.color = color;
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (IsOwner)
        {
            lobbyManager = FindFirstObjectByType<LobbyManager>();
            if (lobbyManager != null)
            {    
                Debug.Log("Registering player on server: " + Owner.ClientId);
                lobbyManager.lobbyPanel.SetActive(true);
                lobbyManager.createGamePanel.SetActive(false);
                lobbyManager.findGamesPanel.SetActive(false);

                lobbyManager.ClientId = Owner.ClientId;
                PlayerName = lobbyManager.enteredName;
                RegisterPlayerServerRpc(lobbyManager.enteredName);
            }
        }

        SpawnPlayer();
    }

    public void SpawnPlayer()
    {
        if (!IsOwner)  // Prüft, ob das der lokale Spieler ist
        {
            playerCamera.gameObject.SetActive(false);
        }
        else
        {
            playerCamera.gameObject.SetActive(true);
        }

        // Falls du PlayerInput benutzt:
        // Nur für den Owner aktivieren
        PlayerInput input = GetComponent<PlayerInput>();

        if (input != null)
        {   
            input.enabled = IsOwner;
        }
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        
        if (IsOwner && lobbyManager != null)
        {
            lobbyManager.GoToLobby();
        }
    }

    public void CheckPassword(string password)
    {
        Debug.Log("Testing password: " + password);
        MyServerManager.Instance.CheckPassword(this, password);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestServerClose()
    {
        Debug.Log("Requesting Close");
        MyServerManager.Instance.CloseGameServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestStartGame()
    {
        MyServerManager.Instance.StartGame();
    }

    [TargetRpc]
    public void Disconnect(NetworkConnection conn)
    {
        if (lobbyManager != null)
        {
            lobbyManager.GoToLobby();
            ClientManager.StopConnection();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void LeaveLobby()
    {
        MyServerManager.Instance.LeaveLobby(this);
    }

    [ServerRpc]
    private void RegisterPlayerServerRpc(string playerName)
    {
        MyServerManager.Instance.AddPlayer(this, playerName);
    }

    [TargetRpc]
    public void UpdateLobby(NetworkConnection conn, List<PlayerData> playersData)
    {
        lobbyManager.RefreshLobbyUI(playersData);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        spawnTime = Time.time;

        if (WaveSpawner.Instance != null)
            WaveSpawner.Instance.NotifyPlayerSpawned();
    }

    void Update()
    {
        if (!IsOwner)
            return;

        if (!IsOwner || isDead)
            return;

        // Speicher letzte gültige Position
        lastValidPosition = transform.position;

        // Dash hat Priorität
        if (isDashing)
        {
            transform.Translate(dashDirection * dashSpeed * Time.deltaTime, Space.World);
            CheckCollisions();
            return;
        }

        Vector2 targetVelocity = moveInput * maxSpeed;

        float accelRate = (moveInput.sqrMagnitude > 0.01f)
            ? acceleration
            : deceleration;

        currentVelocity = Vector2.MoveTowards(
            currentVelocity,
            targetVelocity,
            accelRate * Time.deltaTime
        );

        Vector3 movement = new Vector3(
            currentVelocity.x,
            0f,
            currentVelocity.y
        );

        transform.Translate(movement * Time.deltaTime, Space.World);

        // Kollisionserkennung
        CheckCollisions();
    }

    void CheckCollisions()
    {
        EnemyAI[] enemies = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
        foreach (EnemyAI enemy in enemies)
        {
            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance < collisionDistance)
            {
                // Enemy weg vom Player drücken
                Vector3 awayFromPlayer = (enemy.transform.position - transform.position).normalized;
                enemy.transform.position = transform.position + awayFromPlayer * collisionDistance;
            }
        }
    }

    void OnCollisionStay(Collision collision)
    {
        if (!IsOwner)
            return;

        // Wenn Player mit Enemy kollidiert, abstoßen
        if (collision.gameObject.CompareTag("Enemy"))
        {
            Vector3 awayFromEnemy = (transform.position - collision.transform.position).normalized;
            transform.position += awayFromEnemy * 0.5f;
        }
    }
}
