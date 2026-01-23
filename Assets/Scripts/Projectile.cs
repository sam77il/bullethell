using UnityEngine;
using FishNet.Object;
using FishNet.Connection;

public class Projectile : NetworkBehaviour
{
    public float lifetime = 5f;
    private float spawnTime;
    private Rigidbody rb;
    private Player owner; // Wer hat das Projektil geschossen

    public void SetOwner(Player player)
    {
        owner = player;
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        
        // Nur auf Server die Lifetime verwalten
        if (IsServer)
        {
            spawnTime = Time.time;
        }
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.useGravity = false;
            // Projektil in Richtung der Velocity drehen
            if (rb.linearVelocity.sqrMagnitude > 0)
            {
                transform.rotation = Quaternion.LookRotation(rb.linearVelocity.normalized) * Quaternion.Euler(0, 0, -90);
            }
        }
    }

    void Update()
    {
        // Nur Server entscheidet über Lifetime
        if (IsServer && Time.time - spawnTime > lifetime)
        {
            DespawnProjectile();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Nur auf Server Kollisionen verarbeiten
        if (!IsServer)
            return;

        // Damage on Enemy
        if (other.CompareTag("Enemy"))
        {
            EnemyAI enemy = other.GetComponent<EnemyAI>();
            if (enemy != null)
            {
                // Rufe Server-Methode auf, um Schaden zu verursachen
                enemy.TakeDamage(100f, owner);
                Debug.Log($"Projectile hit enemy!");
            }

            DespawnProjectile();
        }
    }

    [Server]
    private void DespawnProjectile()
    {
        // Despawne über Netzwerk (nicht Destroy!)
        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            ServerManager.Despawn(NetworkObject);
        }
    }
}