using System.Collections;
using UnityEngine;

public class MechBossAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform animatorObj;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform meleeAttackPoint;
    [SerializeField] private float meleeAttackPointOffset = 1.8f;
    [SerializeField] private Transform laserFirePoint;
    [SerializeField] private GameObject laserProjectilePrefab;

    private Rigidbody2D rb;
    private Animator anim;
    private Transform player;
    private MechBossHealth bossHealth;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float phase2MoveSpeed = 3.8f;
    [SerializeField] private float stopDistance = 1.8f;

    [Header("Detection")]
    [SerializeField] private float detectionRadius = 12f;
    [SerializeField] private float loseTargetRadius = 18f;

    [Header("Melee Attack")]
    [SerializeField] private float meleeDamage = 20f;
    [SerializeField] private float meleeAttackRange = 2.5f;
    [SerializeField] private float meleeAttackCooldown = 2f;
    [SerializeField] private float meleeAttackRadius = 2f;

    [Header("Laser Attack (Phase 2)")]
    [SerializeField] private float laserDamage = 15f;
    [SerializeField] private float laserAttackRange = 10f;
    [SerializeField] private float laserAttackCooldown = 3f;
    [SerializeField] private float laserProjectileSpeed = 9f;

    [Header("Stomp Attack (Phase 2)")]
    [SerializeField] private float stompDamage = 25f;
    [SerializeField] private float stompRadius = 3f;
    [SerializeField] private float stompCooldown = 6f;

    [Header("Phase 2 Settings")]
    [SerializeField] private float phase2AttackSpeedMultiplier = 1.4f;

    // State
    private bool isChasing = false;
    private bool isAttacking = false;
    private bool facingRight = true;
    private bool isGrounded = false;
    private bool isPhase2 = false;
    private bool isEntering = true;

    private float nextMeleeAttackTime = 0f;
    private float nextLaserAttackTime = 0f;
    private float nextStompTime = 0f;
    private float currentMoveSpeed;

    // Animator parameter hashes (must match your Animator Controller parameter names)
    private static readonly int AnimXVelocity = Animator.StringToHash("xVelocity");
    private static readonly int AnimIsGrounded = Animator.StringToHash("isGrounded");
    private static readonly int AnimMeleeAttack = Animator.StringToHash("meleeAttack");
    private static readonly int AnimLaserAttack = Animator.StringToHash("laserAttack");
    private static readonly int AnimStomp = Animator.StringToHash("stomp");
    private static readonly int AnimPhase2 = Animator.StringToHash("phase2");

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bossHealth = GetComponent<MechBossHealth>();
        currentMoveSpeed = moveSpeed;

        if (animatorObj != null)
            anim = animatorObj.GetComponent<Animator>();

        FindTargetPlayer();
    }

    private void Start()
    {
        StartCoroutine(BossEntrySequence());
    }

    private IEnumerator BossEntrySequence()
    {
        isEntering = true;
        rb.linearVelocity = Vector2.zero;
        yield return new WaitForSeconds(1.5f);
        isEntering = false;
        isChasing = true;
    }

    private void FindTargetPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        player = playerObj != null ? playerObj.transform : null;
    }

    private void Update()
    {
        if (isEntering) return;

        CheckGrounded();
        CheckPlayerDetection();

        if (isChasing && player != null)
            DecideAction();
        else
            StopMoving();

        UpdateAttackPointPosition();
        HandleFlip();
        UpdateAnimations();
    }

    private void DecideAction()
    {
        if (isAttacking) return;

        float dist = Vector2.Distance(transform.position, player.position);

        // Priority: Stomp > Melee > Laser > Chase
        if (isPhase2 && Time.time >= nextStompTime && dist <= stompRadius)
            StartCoroutine(DoStompAttack());
        else if (Time.time >= nextMeleeAttackTime && dist <= meleeAttackRange)
            StartCoroutine(DoMeleeAttack());
        else if (isPhase2 && Time.time >= nextLaserAttackTime && dist <= laserAttackRange)
            StartCoroutine(DoLaserAttack());
        else
            ChasePlayer();
    }

    private void ChasePlayer()
    {
        if (player == null || isAttacking) return;

        float dist = Vector2.Distance(transform.position, player.position);
        if (dist <= stopDistance)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        float dir = Mathf.Sign(player.position.x - transform.position.x);
        rb.linearVelocity = new Vector2(dir * currentMoveSpeed, rb.linearVelocity.y);
    }

    private IEnumerator DoMeleeAttack()
    {
        isAttacking = true;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

        if (anim != null) anim.SetTrigger(AnimMeleeAttack);

        float cooldown = meleeAttackCooldown / (isPhase2 ? phase2AttackSpeedMultiplier : 1f);
        nextMeleeAttackTime = Time.time + cooldown;

        // DamageMelee() is called via animation event at the hit frame
        yield return new WaitForSeconds(cooldown * 0.85f);
        isAttacking = false;
    }

    private IEnumerator DoLaserAttack()
    {
        isAttacking = true;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

        if (anim != null) anim.SetTrigger(AnimLaserAttack);

        float cooldown = laserAttackCooldown / (isPhase2 ? phase2AttackSpeedMultiplier : 1f);
        nextLaserAttackTime = Time.time + cooldown;

        // FireLaser() is called via animation event at the fire frame
        yield return new WaitForSeconds(cooldown * 0.85f);
        isAttacking = false;
    }

    private IEnumerator DoStompAttack()
    {
        isAttacking = true;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

        if (anim != null) anim.SetTrigger(AnimStomp);

        nextStompTime = Time.time + stompCooldown;

        // DamageStomped() is called via animation event at the impact frame
        yield return new WaitForSeconds(stompCooldown * 0.5f);
        isAttacking = false;
    }

    // Called by MechBossAnimationEvents at the melee hit frame
    public void DamageMelee()
    {
        if (player == null) return;

        float dist = Vector2.Distance(meleeAttackPoint != null ? meleeAttackPoint.position : transform.position, player.position);
        if (dist > meleeAttackRadius) return;

        PlayerHealth health = player.GetComponentInParent<PlayerHealth>();
        if (health != null)
        {
            health.TakeDamage(meleeDamage);

            PlayerMovement pm = player.GetComponentInParent<PlayerMovement>();
            if (pm != null)
            {
                Vector2 knockbackDir = (player.position - transform.position).normalized;
                knockbackDir.y += 0.4f;
                pm.ApplyKnockback(knockbackDir.normalized);
            }
        }
    }

    // Called by MechBossAnimationEvents at the laser fire frame
    public void FireLaser()
    {
        if (laserFirePoint == null || laserProjectilePrefab == null || player == null) return;

        Vector2 direction = (player.position - laserFirePoint.position).normalized;
        GameObject laser = Instantiate(laserProjectilePrefab, laserFirePoint.position, Quaternion.identity);

        BossLaserProjectile projectile = laser.GetComponent<BossLaserProjectile>();
        if (projectile != null)
            projectile.Initialize(direction, laserProjectileSpeed, laserDamage);
    }

    // Called by MechBossAnimationEvents at the stomp impact frame
    public void DamageStomped()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, stompRadius);
        foreach (var hit in hits)
        {
            PlayerHealth health = hit.GetComponentInParent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(stompDamage);

                PlayerMovement pm = hit.GetComponentInParent<PlayerMovement>();
                if (pm != null)
                {
                    Vector2 knockbackDir = (hit.transform.position - transform.position).normalized;
                    knockbackDir.y += 0.5f;
                    pm.ApplyKnockback(knockbackDir.normalized);
                }
            }
        }
    }

    // Called by MechBossHealth when health crosses 50%
    public void EnterPhase2()
    {
        if (isPhase2) return;
        isPhase2 = true;
        currentMoveSpeed = phase2MoveSpeed;

        if (anim != null) anim.SetTrigger(AnimPhase2);

        StartCoroutine(Phase2TransitionPause());
    }

    private IEnumerator Phase2TransitionPause()
    {
        isAttacking = true;
        rb.linearVelocity = Vector2.zero;
        yield return new WaitForSeconds(1.5f);
        isAttacking = false;
    }

    public void DisableAI()
    {
        StopAllCoroutines();
        isAttacking = true;
        rb.linearVelocity = Vector2.zero;
        enabled = false;
    }

    private void CheckPlayerDetection()
    {
        if (player == null || !player.gameObject.activeInHierarchy)
        {
            FindTargetPlayer();
            if (player == null) { isChasing = false; return; }
        }

        float dist = Vector2.Distance(transform.position, player.position);
        if (!isChasing && dist <= detectionRadius)
            isChasing = true;
        else if (isChasing && dist > loseTargetRadius)
            isChasing = false;
    }

    private void StopMoving()
    {
        if (!isAttacking)
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
    }

    private void CheckGrounded()
    {
        isGrounded = Physics2D.Raycast(transform.position, Vector2.down, 0.3f, groundLayer);
    }

    private void HandleFlip()
    {
        if (rb.linearVelocity.x > 0.05f && !facingRight)
            Flip();
        else if (rb.linearVelocity.x < -0.05f && facingRight)
            Flip();
        else if (!isAttacking && player != null)
        {
            float dir = player.position.x - transform.position.x;
            if (dir > 0.1f && !facingRight) Flip();
            else if (dir < -0.1f && facingRight) Flip();
        }
    }

    private void Flip()
    {
        facingRight = !facingRight;
        if (animatorObj != null)
            animatorObj.localRotation = facingRight ? Quaternion.Euler(0, 0, 0) : Quaternion.Euler(0, 180, 0);
        UpdateAttackPointPosition();
    }

    private void UpdateAttackPointPosition()
    {
        if (meleeAttackPoint == null) return;
        float offset = facingRight ? meleeAttackPointOffset : -meleeAttackPointOffset;
        meleeAttackPoint.position = transform.position + new Vector3(offset, 0, 0);
    }

    private void UpdateAnimations()
    {
        if (anim == null) return;
        anim.SetFloat(AnimXVelocity, Mathf.Abs(rb.linearVelocity.x));
        anim.SetBool(AnimIsGrounded, isGrounded);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, loseTargetRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, meleeAttackRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, laserAttackRange);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, stompRadius);
    }
}
