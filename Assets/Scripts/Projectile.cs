using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float lifetime = 5f;
    private float spawnTime;
    private Rigidbody rb;


    void Start()
    {
        spawnTime = Time.time;
        rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.useGravity = false;
            // Drehe das Projektil in Richtung der Velocity
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
        // Treffe nur Enemies
        if (other.CompareTag("Enemy"))
        {
            EnemyAI enemy = other.GetComponent<EnemyAI>();
            if (enemy != null)
            {
                enemy.Health -= 100f;
                Debug.Log($"Projectile hit enemy! Enemy health: {enemy.Health}");

                if (enemy.Health <= 0)
                {
                    Debug.Log("Enemy defeated!");
                }
            }

            Destroy(gameObject);
        }
    }
}

