using UnityEngine;

public class BossLaserProjectile : MonoBehaviour
{
    private Vector2 direction;
    private float speed;
    private float damage;

    [SerializeField] private float lifetime = 5f;

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

        // Destroy on ground/walls
        if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
            Destroy(gameObject);
    }
}
