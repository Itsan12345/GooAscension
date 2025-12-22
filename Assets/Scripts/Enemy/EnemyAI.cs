using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform animatorObj;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody2D rb;
    private Animator anim;
    private Transform player;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float stopDistance = 0.5f;
    [SerializeField] private float horizontalAlignmentThreshold = 0.2f;

    private bool facingRight = true;
    private bool isGrounded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = animatorObj.GetComponent<Animator>();
        // Look for the player right at the start
        FindTargetPlayer();
    }

    private void FindTargetPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }

    private void ChasePlayer()
    {
        float horizontalDifference = Mathf.Abs(player.position.x - transform.position.x);

        // If the player is directly above the enemy, the enemy stops to wait/look up
        if (player.position.y > transform.position.y && horizontalDifference < horizontalAlignmentThreshold)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }
        // 1. If we lost the player reference, try to find it again immediately
        if (player == null)
        {
            FindTargetPlayer();
            if (player == null) return;
        }

        // 2. SAFETY CHECK: Only stop if the player is dead (inactive)
        if (!player.gameObject.activeInHierarchy)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        // 3. Movement Logic (Following the parent's position)
        float distance = Vector2.Distance(transform.position, player.position);
        if (distance <= stopDistance)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        float dir = Mathf.Sign(player.position.x - transform.position.x);
        rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);
    }

    private void Update()
    {
        CheckGrounded();
        ChasePlayer();
        HandleFlip();
        UpdateAnimations();
    }

   

    private void HandleFlip()
    {
        if (rb.linearVelocity.x > 0 && !facingRight)
            Flip();
        else if (rb.linearVelocity.x < 0 && facingRight)
            Flip();
    }

    private void Flip()
    {
        facingRight = !facingRight;
        animatorObj.localRotation = facingRight
            ? Quaternion.Euler(0, 0, 0)
            : Quaternion.Euler(0, 180, 0);
    }

    private void CheckGrounded()
    {
        isGrounded = Physics2D.Raycast(
            transform.position,
            Vector2.down,
            0.2f,
            groundLayer
        );
    }

    private void UpdateAnimations()
    {
        anim.SetFloat("xVelocity", Mathf.Abs(rb.linearVelocity.x));
        anim.SetBool("isGrounded", isGrounded);
    }
    [Header("Combat Settings")]
    [SerializeField] private float damageAmount = 10f;
    [SerializeField] private float attackCooldown = 1.0f;
    private float nextAttackTime;

    private void OnCollisionStay2D(Collision2D collision)
    {
        PlayerHealth health = collision.gameObject.GetComponentInParent<PlayerHealth>();

        if (health != null && Time.time >= nextAttackTime)
        {
            health.TakeDamage(damageAmount);

            // --- ADD KNOCKBACK CALL HERE ---
            PlayerMovement pm = health.GetComponent<PlayerMovement>();
            if (pm != null)
            {
                // Calculate direction: from enemy to player
                Vector2 knockbackDir = (collision.transform.position - transform.position).normalized;
                // Add a little bit of upward lift to the knockback
                knockbackDir.y += 0.5f;
                pm.ApplyKnockback(knockbackDir.normalized);
            }

            nextAttackTime = Time.time + attackCooldown;
        }
    }
}
