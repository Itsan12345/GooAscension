using UnityEngine;

public class BossLaserProjectile : MonoBehaviour
{
    private Vector2 direction;
    private float speed;
    private float damage;

    [SerializeField] private float lifetime = 5f;

    private void Awake()
    {
        // Ensure the collider is always a trigger so the laser never physically
        // blocks or pushes the player via collision resolution.
        foreach (Collider2D col in GetComponentsInChildren<Collider2D>(true))
            col.isTrigger = true;

        // The laser moves via Transform.Translate (not physics). Unity only fires
        // trigger callbacks on moving objects that have a Rigidbody2D. Add one if
        // the prefab doesn't have it so OnTriggerEnter2D fires correctly.
        if (GetComponent<Rigidbody2D>() == null)
        {
            Rigidbody2D rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
        }
    }

    public void Initialize(Vector2 dir, float spd, float dmg)
    {
        direction = dir.normalized;
        speed = spd;
        damage = dmg;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        transform.Translate(Vector2.right * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Ignore the boss and its own children
        if (other.GetComponentInParent<MechBossAI>() != null) return;
        // Ignore other laser projectiles
        if (other.GetComponent<BossLaserProjectile>() != null) return;

        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
        if (health != null)
        {
            health.TakeDamage(damage);

            PlayerMovement pm = other.GetComponentInParent<PlayerMovement>();
            if (pm != null)
                pm.ApplyKnockback(direction.normalized);

            Destroy(gameObject);
            return;
        }

        if (other.CompareTag("Ground") || other.gameObject.layer == LayerMask.NameToLayer("Ground"))
            Destroy(gameObject);
    }
}
