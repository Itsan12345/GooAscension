using UnityEngine;
using System.Collections.Generic;

public class ChargedShot : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float baseSpeed = 15f;
    [SerializeField] private float chargedSpeedMultiplier = 2f;

    [Header("Damage")]
    [SerializeField] private float baseDamage = 20f;
    [SerializeField] private float maxChargeDamage = 20f;

    [Header("Effects")]
    [SerializeField] private float knockbackForce = 500f;

    private float direction = 1f;
    private float chargeLevel = 0f;
    private float currentDamage;
    private float currentSpeed;
    
    private Animator animator;
    private bool animationFinished;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

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
    }

    private void Update()
    {
        // Destroy automatically once the non-looping animation has finished
        if (!animationFinished && animator != null)
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.normalizedTime >= 1f)
            {
                animationFinished = true;
                Destroy(gameObject);
                return;
            }
        }

        // Do not move; charged shot remains static at its spawn position
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        IDamageable target = other.GetComponentInParent<IDamageable>();
        if (target != null)
        {
            // Initial burst of damage on first contact
            target.TakeDamage(currentDamage);
            Debug.Log($"ChargedShot initial hit dealt {currentDamage} damage to {other.name}");

            Rigidbody2D enemyRb = other.GetComponentInParent<Rigidbody2D>();
            if (enemyRb != null)
            {
                Vector2 knockbackDirection = new Vector2(direction, 0.2f).normalized;
                enemyRb.AddForce(knockbackDirection * knockbackForce * (1f + chargeLevel));
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

    private void OnTriggerStay2D(Collider2D other)
    {
        // Continuously damage any enemies that remain inside the charged shot area.
        // This will hit multiple enemies at once as long as they are touching
        // the charged shot animation.
        IDamageable target = other.GetComponentInParent<IDamageable>();
        if (target != null)
        {
            float damageThisFrame = currentDamage * Time.deltaTime;
            target.TakeDamage(damageThisFrame);
        }
    }

    private void CreateImpactEffect()
    {
        // You can add particle effects or visual feedback here later
        Debug.Log("ChargedShot impact effect!");
    }
}