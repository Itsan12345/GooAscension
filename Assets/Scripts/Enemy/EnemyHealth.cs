using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 30f;
    private float currentHealth;

    [Header("Death FX")]
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private float destroyDelay = 2f;
    [SerializeField] private float explosionZ = -1f;

    private bool dead;

    private Rigidbody2D rb;
    private Collider2D[] colliders;
    private EnemyAI enemyAI;

    private void Awake()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        colliders = GetComponentsInChildren<Collider2D>(true);
        enemyAI = GetComponent<EnemyAI>();
    }

    public void TakeDamage(float damage)
    {
        if (dead) return;

        currentHealth -= damage;
        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        dead = true;

        // Stop AI
        if (enemyAI != null)
            enemyAI.enabled = false;

        // Stop physics / movement
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        // Disable all colliders so it can't be hit / hit the player
        if (colliders != null)
        {
            foreach (var c in colliders)
                if (c != null) c.enabled = false;
        }

        // Hide visuals (disable all SpriteRenderers)
        var renderers = GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var r in renderers)
            r.enabled = false;

        // Spawn explosion once
        if (explosionPrefab != null)
        {
            Instantiate(
                explosionPrefab,
                new Vector3(transform.position.x, transform.position.y, explosionZ),
                Quaternion.identity
            );
        }

        // Remove enemy after FX time
        Destroy(gameObject, destroyDelay);
    }
}
