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
        
        // Flip sprite based on direction
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * direction;
        transform.localScale = scale;
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
        IDamageable target = other.GetComponentInParent<IDamageable>();
        if (target != null)
        {
            target.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        if (other.CompareTag("Ground"))
            Destroy(gameObject);
    }
}
