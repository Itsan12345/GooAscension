using System.Collections.Generic;
using UnityEngine;

public class SwordArcDamage : MonoBehaviour
{
    [Header("Damage Settings")]
    public float baseDamage = 20f; // Fixed 20 damage for charged sword arc
    public float knockbackForce = 8f;

    [Header("Movement Settings")]
    [SerializeField] private float travelSpeed = 8f; // Speed the arc travels
    [SerializeField] private float travelDistance = 25f; // How far it travels (in Unity units)
    
    [Header("Pierce Settings")]
    [SerializeField] private float pierceCount = 100f; // How many enemies it can pass through (like charged shot)

    private float currentDamage;
    private HashSet<Collider2D> hitTargets = new HashSet<Collider2D>(); // Track colliders like charged shot
    private Vector3 startPosition;
    private Vector3 direction;
    private float traveledDistance = 0f;
    private int enemiesHit = 0; // Track enemy count like charged shot

    public void SetCharge(float charge01)
    {
        // Always use 20 damage for charged sword arc, like charged shot
        currentDamage = baseDamage;
        Debug.Log($"🗡️ Sword arc charge set to {charge01:F2}, damage: {currentDamage}");
    }

    void Start()
    {
        // Initialize with fixed 20 damage if not set
        if (currentDamage == 0)
        {
            currentDamage = baseDamage;
            Debug.Log($"🗡️ Sword arc initialized with default damage: {currentDamage}");
        }
        
        // Verify we have a trigger collider
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            Debug.LogError("🗡️ SwordArcDamage: No Collider2D found! Add a Collider2D and set it as trigger.");
        }
        else if (!col.isTrigger)
        {
            Debug.LogWarning("🗡️ SwordArcDamage: Collider2D should be set as trigger for damage detection.");
        }

        // Initialize movement
        startPosition = transform.position;
        direction = transform.localScale.x > 0 ? Vector3.right : Vector3.left;
        
    }

    void Update()
    {
        // Move the sword arc forward
        float moveStep = travelSpeed * Time.deltaTime;
        Vector3 newPosition = transform.position + (direction * moveStep);
        transform.position = newPosition;
        traveledDistance += moveStep;

        // Destroy if traveled too far
        if (traveledDistance >= travelDistance)
        {
            Destroy(gameObject);
        }
    }

    void OnEnable()
    {
        hitTargets.Clear();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"🗡️ SwordArc trigger hit: {other.name}, tag: {other.tag}, layer: {LayerMask.LayerToName(other.gameObject.layer)}");
        
        // Don't hit the same target multiple times (like charged shot)
        if (hitTargets.Contains(other)) return;
        
        // Check if it's on the Enemy layer (assuming Enemy layer exists)
        bool isEnemyLayer = other.gameObject.layer == LayerMask.NameToLayer("Enemy");
        Debug.Log($"🗡️ Enemy layer check: {isEnemyLayer}");
        
        IDamageable target = other.GetComponentInParent<IDamageable>();
        if (target != null)
        {
            hitTargets.Add(other);
            enemiesHit++;

            Debug.Log($"🗡️ DEALING DAMAGE: {currentDamage} to {other.name} (enemy #{enemiesHit})");

            target.TakeDamage(currentDamage);

            Rigidbody2D enemyRb = other.GetComponentInParent<Rigidbody2D>();
            if (enemyRb != null)
            {
                Vector2 knockbackDirection = (other.transform.position - transform.position).normalized;
                enemyRb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);
            }

            if (enemiesHit >= pierceCount)
            {
                CreateImpactEffect();
                Destroy(gameObject);
                return;
            }

            return;
        }

        // Destroy when hitting ground (like charged shot)
        if (other.CompareTag("Ground"))
        {
            Debug.Log($"🗡️ Sword arc hit ground, destroying");
            CreateImpactEffect();
            Destroy(gameObject);
        }
    }
    
    private void CreateImpactEffect()
    {
        // You can add particle effects or visual feedback here later
        Debug.Log("🗡️ Sword arc impact effect!");
    }
    }

