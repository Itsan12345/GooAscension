using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifeTime = 2f;

    private float direction = 1f;
    private float damage = 10f;

    public void SetDirection(float dir)
    {
        direction = Mathf.Sign(dir);
    }

    public void SetDamage(float dmg)
    {
        damage = Mathf.Max(0f, dmg);
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        transform.Translate(Vector2.right * (speed * direction * Time.deltaTime));
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // ✅ Damage enemies
        EnemyHealth eh = other.GetComponentInParent<EnemyHealth>();
        if (eh != null)
        {
            eh.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        // Optional: destroy bullet when hitting ground/walls
        if (other.CompareTag("Ground"))
        {
            Destroy(gameObject);
        }
    }
}
