using System.Collections;
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
    [SerializeField] private Transform animatorObj;
    [SerializeField] private float deathAnimationDuration = 1f;

    [Header("Damage Flash")]
    [Tooltip("The color the sprite flashes when hit.")]
    [SerializeField] private Color flashColor = Color.red;
    [Tooltip("How long the flash lasts in seconds.")]
    [SerializeField] private float flashDuration = 0.1f;

    private bool dead;
    private Coroutine flashCoroutine;
    private bool isFlashing = false;

    private Rigidbody2D rb;
    private Collider2D[] colliders;
    private EnemyAI enemyAI;
    private Animator anim;
    private SpriteRenderer[] spriteRenderers;
    private Color[] originalColors;

    private void Awake()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        colliders = GetComponentsInChildren<Collider2D>(true);
        enemyAI = GetComponent<EnemyAI>();
        
        if (animatorObj != null)
        {
            anim = animatorObj.GetComponent<Animator>();
        }
        
        // Cache sprite renderers and original colors
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        if (spriteRenderers != null && spriteRenderers.Length > 0)
        {
            originalColors = new Color[spriteRenderers.Length];
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                originalColors[i] = spriteRenderers[i].color;
            }
        }
    }

    public void TakeDamage(float damage)
    {
        if (dead) return;

        currentHealth -= damage;
        
        // Trigger flash effect when damaged
        Flash();
        
        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Flash()
    {
        // Don't start a new flash if already flashing
        if (isFlashing) return;
        
        // Stop any existing coroutine
        if (flashCoroutine != null) 
        {
            StopCoroutine(flashCoroutine);
            RestoreOriginalColors(); // Make sure colors are restored before starting new flash
        }
        
        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private void RestoreOriginalColors()
    {
        if (spriteRenderers != null && originalColors != null)
        {
            for (int i = 0; i < spriteRenderers.Length && i < originalColors.Length; i++)
            {
                if (spriteRenderers[i] != null)
                    spriteRenderers[i].color = originalColors[i];
            }
        }
        isFlashing = false;
    }

    // The routine that handles changing colors over time
    private IEnumerator FlashRoutine()
    {
        if (spriteRenderers == null || spriteRenderers.Length == 0) yield break;
        
        isFlashing = true;

        // Change to flash color
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
                spriteRenderers[i].color = flashColor;
        }

        // Wait for flash duration
        yield return new WaitForSeconds(flashDuration);

        // Restore original colors
        RestoreOriginalColors();
        
        flashCoroutine = null;
    }

    private void Die()
    {
        dead = true;

        // Stop any active flash coroutine and restore colors before death
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }
        RestoreOriginalColors();

        // Stop AI
        if (enemyAI != null)
            enemyAI.enabled = false;

        // Stop physics / movement
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        // Disable attack/damage colliders but keep main body collider for ground collision
        if (colliders != null)
        {
            foreach (var c in colliders)
            {
                if (c != null && c.gameObject != gameObject) // Don't disable main body collider
                {
                    c.enabled = false;
                }
            }
        }

        // Disable FollowHitbox component to stop position following
        FollowHitbox followHitbox = GetComponentInChildren<FollowHitbox>();
        if (followHitbox != null)
        {
            followHitbox.enabled = false;
        }

        // Trigger death animation first
        if (anim != null)
        {
            anim.SetTrigger("die");
        }
        
        // Wait for death animation to complete, then show explosion and destroy
        Invoke(nameof(OnDeathAnimationComplete), deathAnimationDuration);
    }
    
    private void OnDeathAnimationComplete()
    {
        // Hide visuals (disable all SpriteRenderers) after animation
        var renderers = GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var r in renderers)
            r.enabled = false;

        // Spawn explosion after death animation
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
