using UnityEngine;
using UnityEngine.InputSystem;
using FishNet.Object;

public class Player : NetworkBehaviour
{
    [Header("Movement")]
    public Camera playerCamera;
    public float maxSpeed = 5f;
    public float acceleration = 20f;
    public float deceleration = 25f;
    public float collisionDistance = 1.2f; // Abstand zu Enemies
    public float Health = 100f;
    public float spawnProtectionTime = 3f; // Spawnschutz in Sekunden
    private Vector2 moveInput;
    private Vector2 currentVelocity;
    private Vector3 lastValidPosition;
    private float spawnTime;

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
        // Nur der lokale Spieler darf Input setzen
        if (!IsOwner)
            return;

        moveInput = context.ReadValue<Vector2>();
    }

    public void Dash(InputAction.CallbackContext context)
    {
        if (!IsOwner)
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
        if (!IsOwner)
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

        if (Health <= 0)
        {
            Debug.Log("Player died!");
            Destroy(gameObject);
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

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
            input.enabled = IsOwner;
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
