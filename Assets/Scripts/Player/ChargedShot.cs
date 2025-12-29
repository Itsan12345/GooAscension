using UnityEngine;
using System.Collections.Generic;

public class ChargedShot : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float baseSpeed = 15f;
    [SerializeField] private float chargedSpeedMultiplier = 2f;
    [SerializeField] private float lifeTime = 3f;

    [Header("Damage")]
    [SerializeField] private float baseDamage = 20f;
    [SerializeField] private float maxChargeDamage = 20f;

    [Header("Effects")]
    [SerializeField] private float knockbackForce = 500f;
    [SerializeField] private float pierceCount = 100f; // How many enemies it can pass through

    private float direction = 1f;
    private float chargeLevel = 0f;
    private float currentDamage;
    private float currentSpeed;
    private int enemiesHit = 0;
    private HashSet<Collider2D> hitTargets = new HashSet<Collider2D>();

    public void SetDirection(float dir)
    {
        direction = Mathf.Sign(dir);
        // Flip sprite if moving left
        if (direction < 0)
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }
    }

    public void SetCharge(float charge01)
    {
        chargeLevel = Mathf.Clamp01(charge01);
        
        // Calculate damage based on charge level
        currentDamage = Mathf.Lerp(baseDamage, maxChargeDamage, chargeLevel);
        
        // Calculate speed based on charge level
        currentSpeed = baseSpeed * (1f + (chargedSpeedMultiplier - 1f) * chargeLevel);
        
        Debug.Log($"ChargedShot created with charge: {chargeLevel:F2}, damage: {currentDamage}, speed: {currentSpeed}");
        
        // Scale the projectile based on charge level (visual feedback)
        float scale = 1f + (chargeLevel * 0.5f); // 50% larger when fully charged
        transform.localScale = new Vector3(transform.localScale.x, scale, scale);
    }

    private void Start()
    {
        // Destroy after lifetime
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        // Move the charged shot forward
        transform.Translate(Vector2.right * (currentSpeed * direction * Time.deltaTime));
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Don't hit the same target multiple times
        if (hitTargets.Contains(other)) return;

        // Check for enemy collision
        EnemyHealth enemyHealth = other.GetComponentInParent<EnemyHealth>();
        if (enemyHealth != null)
        {
            hitTargets.Add(other);
            enemiesHit++;
            
            // Deal damage
            enemyHealth.TakeDamage(currentDamage);
            Debug.Log($"ChargedShot dealt {currentDamage} damage to {other.name}");
            
            // Apply knockback
            Rigidbody2D enemyRb = other.GetComponentInParent<Rigidbody2D>();
            if (enemyRb != null)
            {
                Vector2 knockbackDirection = new Vector2(direction, 0.2f).normalized;
                enemyRb.AddForce(knockbackDirection * knockbackForce * (1f + chargeLevel));
            }
            
            // Destroy shot after hitting max enemies (unless fully charged - then pierce more)
            float maxPierce = pierceCount * (1f + chargeLevel);
            if (enemiesHit >= maxPierce)
            {
                // Create a small explosion effect at destruction point
                CreateImpactEffect();
                Destroy(gameObject);
            }
            
            return;
        }

        // Destroy when hitting ground (remove Wall tag since it's not defined)
        if (other.CompareTag("Ground"))
        {
            CreateImpactEffect();
            Destroy(gameObject);
        }
    }

    private void CreateImpactEffect()
    {
        // You can add particle effects or visual feedback here later
        Debug.Log("ChargedShot impact effect!");
    }
}