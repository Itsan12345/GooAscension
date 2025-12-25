using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform animatorObj;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackPointOffset = 1.5f;
    private AttackPointTrigger attackPointTrigger;

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
        if (animatorObj != null)
        {
            anim = animatorObj.GetComponent<Animator>();
        }
        // Get the AttackPointTrigger from the attack point child
        if (attackPoint != null)
        {
            attackPointTrigger = attackPoint.GetComponent<AttackPointTrigger>();
        }
        // Look for the player right at the start
        FindTargetPlayer();
    }

    private void FindTargetPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        player = (playerObj != null) ? playerObj.transform : null;
    }

    private void ChasePlayer()
    {
        if (isAttacking)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        // Ensure valid target first
        if (player == null || !player.gameObject.activeInHierarchy)
        {
            FindTargetPlayer();
            if (player == null || !player.gameObject.activeInHierarchy)
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                return;
            }
        }

        float horizontalDifference = Mathf.Abs(player.position.x - transform.position.x);

        if (player.position.y > transform.position.y && horizontalDifference < horizontalAlignmentThreshold)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

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
        UpdateAttackPointPosition();
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
        UpdateAttackPointPosition();
    }

    private void UpdateAttackPointPosition()
    {
        if (attackPoint == null)
            return;

        // Position the attack point in front of the enemy based on facing direction
        float offset = facingRight ? attackPointOffset : -attackPointOffset;
        Vector3 newPosition = transform.position + new Vector3(offset, 0, 0);
        attackPoint.position = newPosition;
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
        if (anim != null)
        {
            anim.SetFloat("xVelocity", Mathf.Abs(rb.linearVelocity.x));
            anim.SetBool("isGrounded", isGrounded);
        }
    }

    [Header("Combat Settings")]
    [SerializeField] private float damageAmount = 10f;
    [SerializeField] private float attackCooldown = 1.0f;
    private float nextAttackTime;
    private bool canMove = true;
    private bool isAttacking = false;

    public void DamageTarget()
    {
        if (player == null)
            return;

        // Only damage if player is actually in the attack point zone
        if (attackPointTrigger != null && !attackPointTrigger.IsPlayerInZone())
            return;

        PlayerHealth health = player.GetComponentInParent<PlayerHealth>();
        if (health != null && Time.time >= nextAttackTime)
        {
            health.TakeDamage(damageAmount);

            PlayerMovement pm = player.GetComponentInParent<PlayerMovement>();
            if (pm != null)
            {
                Vector2 knockbackDir = (player.position - transform.position).normalized;
                knockbackDir.y += 0.5f;
                pm.ApplyKnockback(knockbackDir.normalized);
            }

            nextAttackTime = Time.time + attackCooldown;
        }
    }

    public void EnableMovementAndJump(bool enable)
    {
        canMove = enable;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if player entered the attack point
        if (collision.GetComponentInParent<PlayerMovement>() != null && !isAttacking && Time.time >= nextAttackTime)
        {
            TriggerAttack();
        }
    }

    public void TriggerAttack()
    {
        if (!isAttacking && Time.time >= nextAttackTime && anim != null)
        {
            isAttacking = true;
            anim.SetTrigger("attack");
            Invoke(nameof(ResetAttack), 2.3f);
        }
    }

    private void ResetAttack()
    {
        isAttacking = false;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        // Damage is only dealt through DamageTarget() when the attack animation is triggered
        // This method now does nothing to prevent constant damage
    }
}
