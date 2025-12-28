using System.Collections.Generic;
using UnityEngine;

public class SwordArcDamage : MonoBehaviour
{
    [Header("Damage Settings")]
    public int minDamage = 20;
    public int maxDamage = 20;
    public float knockbackForce = 8f;

    [Header("Movement Settings")]
    [SerializeField] private float travelSpeed = 8f; // Speed the arc travels
    [SerializeField] private float travelDistance = 6f; // How far it travels (in Unity units)

    private int currentDamage;
    private HashSet<GameObject> hitTargets = new HashSet<GameObject>();
    private Vector3 startPosition;
    private Vector3 direction;
    private float traveledDistance = 0f;

    public void SetCharge(float charge01)
    {
        currentDamage = Mathf.RoundToInt(
            Mathf.Lerp(minDamage, maxDamage, charge01)
        );
        Debug.Log($"Sword arc charge set to {charge01:F2}, damage: {currentDamage}");
    }

    void Start()
    {
        // Initialize with minimum damage if not set
        if (currentDamage == 0)
        {
            currentDamage = minDamage;
            Debug.Log($"Sword arc initialized with default damage: {currentDamage}");
        }
        
        // Verify we have a trigger collider
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            Debug.LogError("SwordArcDamage: No Collider2D found! Add a Collider2D and set it as trigger.");
        }
        else if (!col.isTrigger)
        {
            Debug.LogWarning("SwordArcDamage: Collider2D should be set as trigger for damage detection.");
        }
        else
        {
            Debug.Log("SwordArcDamage: Trigger collider properly configured.");
        }

        // Initialize movement
        startPosition = transform.position;
        direction = transform.localScale.x > 0 ? Vector3.right : Vector3.left;
        Debug.Log($"Sword arc starting movement in direction: {direction}");
    }

    void Update()
    {
        // Move the sword arc forward
        float moveStep = travelSpeed * Time.deltaTime;
        transform.position += direction * moveStep;
        traveledDistance += moveStep;

        // Destroy if traveled too far
        if (traveledDistance >= travelDistance)
        {
            Debug.Log("Sword arc reached maximum distance, destroying");
            Destroy(gameObject);
        }
    }

    void OnEnable()
    {
        hitTargets.Clear();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"SwordArc trigger hit: {other.name}, tag: {other.tag}");
        
        if (!other.CompareTag("Enemy"))
        {
            Debug.Log($"Object {other.name} doesn't have Enemy tag, skipping damage");
            return;
        }
        
        if (hitTargets.Contains(other.gameObject))
        {
            Debug.Log($"Enemy {other.name} already hit by this arc, skipping");
            return;
        }

        hitTargets.Add(other.gameObject);
        Debug.Log($"Attempting to damage enemy: {other.name}");

        // Use EnemyHealth instead of IDamageable to match existing system
        EnemyHealth enemyHealth = other.GetComponent<EnemyHealth>();
        if (enemyHealth == null)
        {
            enemyHealth = other.GetComponentInParent<EnemyHealth>();
            Debug.Log($"EnemyHealth found in parent: {enemyHealth != null}");
        }
        else
        {
            Debug.Log($"EnemyHealth found on object: {enemyHealth != null}");
        }
        
        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(currentDamage);
            
            // Apply knockback if the enemy has a Rigidbody2D
            Rigidbody2D enemyRb = other.GetComponent<Rigidbody2D>();
            if (enemyRb != null)
            {
                Vector2 knockbackDirection = (other.transform.position - transform.position).normalized;
                enemyRb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);
            }
            
            Debug.Log($"Sword arc dealt {currentDamage} damage to {other.name}");
        }
        else
        {
            Debug.LogWarning($"No EnemyHealth component found on {other.name} or its parent!");
        }
    }
    }

