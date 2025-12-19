using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float speed = 15f;
    [SerializeField] private float lifeTime = 2f;
    private float direction;

    public void SetDirection(float dir)
    {
        direction = dir;
        transform.localScale = new Vector3(dir, 1, 1);
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        transform.Translate(Vector2.right * direction * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy") || other.CompareTag("Ground"))
            Destroy(gameObject);
    }
}
