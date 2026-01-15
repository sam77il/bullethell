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
    private Vector2 moveInput;
    private Vector2 currentVelocity;
    private Vector3 lastValidPosition;

    public void Move(InputAction.CallbackContext context)
    {
        // Nur der lokale Spieler darf Input setzen
        if (!IsOwner)
            return;

        moveInput = context.ReadValue<Vector2>();
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

        if (WaveSpawner.Instance != null)
            WaveSpawner.Instance.NotifyPlayerSpawned();
    }

    void Update()
    {
        if (!IsOwner)
            return;

        // Speicher letzte gültige Position
        lastValidPosition = transform.position;

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
        EnemyAI[] enemies = FindObjectsOfType<EnemyAI>();
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

