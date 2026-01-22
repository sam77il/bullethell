using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float lifetime = 5f;
    private float spawnTime;
    private Rigidbody rb;
    private Player owner; // Wer hat das Projektil geschossen

    public void SetOwner(Player player)
    {
        owner = player;
    }

    void Start()
    {
        spawnTime = Time.time;
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
        if (Time.time - spawnTime > lifetime)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {
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

            Destroy(gameObject);
        }
    }
}

