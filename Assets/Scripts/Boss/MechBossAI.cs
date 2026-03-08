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

    [Header("Water Avoidance")]
    [SerializeField] private LayerMask waterLayer;
    [SerializeField] private float waterCheckDistance = 1.2f;  // How far ahead to scan
    [SerializeField] private float waterCheckHeightOffset = 0f; // Adjust if boss origin isn't at feet

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
    private bool isBlockedByWater = false;

    // Parry / Stagger State
    [Header("Parry Settings")]
    [SerializeField] private float bossStaggerDuration = 1.0f;
    [SerializeField] private float parryKnockbackForce = 12f;
    [SerializeField] private float knockbackFreeTime = 0.2f;    // seconds physics runs freely before braking
    [SerializeField] private float knockbackDecayRate = 5f;     // units/sec deceleration after free phase
    private bool isParryable = false;
    private bool isStaggered = false;
    private float staggerTimer = 0f;
    private Coroutine currentAttackCoroutine;
    public bool IsParryable => isParryable;

    // Cached player components (refreshed when player reference changes)
    private PlayerMovement playerMovement;

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

        Debug.Log($"[MechBossAI] Awake on '{name}'. animatorObj={(animatorObj != null ? animatorObj.name : "null")}, bossHealth={bossHealth}");

        FindTargetPlayer();
    }

    private void Start()
    {
        Debug.Log($"[MechBossAI] Start on '{name}'. Beginning BossEntrySequence.");
        StartCoroutine(BossEntrySequence());
    }

    private IEnumerator BossEntrySequence()
    {
        Debug.Log("[MechBossAI] BossEntrySequence started (intro pause).");
        isEntering = true;
        rb.linearVelocity = Vector2.zero;
        yield return new WaitForSeconds(1.5f);
        isEntering = false;
        isChasing = true;
        Debug.Log("[MechBossAI] BossEntrySequence finished. Boss is now active and chasing.");
    }

    private void FindTargetPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerMovement = playerObj.GetComponent<PlayerMovement>();
        }
        else
        {
            player = null;
            playerMovement = null;
        }
    }

    // Returns the best position to target — uses the active hitbox so both forms are tracked correctly
    private Vector3 GetPlayerTargetPosition()
    {
        if (player == null) return Vector3.zero;

        // If we have PlayerMovement, target whichever form's hitbox is currently active
        if (playerMovement != null)
        {
            // Walk children and return the first active child collider's position
            foreach (Transform child in player)
            {
                if (child.gameObject.activeInHierarchy)
                {
                    Collider2D col = child.GetComponent<Collider2D>();
                    if (col != null) return col.bounds.center;
                }
            }
        }
        return player.position;
    }

    // Returns true when the player is currently in Slime form
    private bool IsPlayerSlime()
    {
        return playerMovement != null && !playerMovement.IsHuman;
    }

    // Reliably finds PlayerHealth regardless of where it sits in the player hierarchy
    private PlayerHealth FindPlayerHealth()
    {
        if (player == null) return null;
        PlayerHealth health = player.GetComponent<PlayerHealth>();
        if (health == null) health = player.GetComponentInParent<PlayerHealth>();
        if (health == null) health = player.GetComponentInChildren<PlayerHealth>();
        return health;
    }

    private void Update()
    {
        if (isEntering) return;

        // Handle stagger from parry
        if (isStaggered)
        {
            staggerTimer -= Time.deltaTime;
            float elapsed = bossStaggerDuration - staggerTimer;

            if (elapsed < knockbackFreeTime)
            {
                // Free-slide phase: boss launches back under full physics — sells its mass
            }
            else
            {
                // Braking phase: heavy, gradual deceleration — boss grinds to a stop
                float decayedX = Mathf.MoveTowards(rb.linearVelocity.x, 0f, knockbackDecayRate * Time.deltaTime);
                rb.linearVelocity = new Vector2(decayedX, rb.linearVelocity.y);
            }

            if (staggerTimer <= 0f)
            {
                isStaggered = false;
                isAttacking = false;
            }
            UpdateAnimations();
            return;
        }

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

        Vector3 targetPos = GetPlayerTargetPosition();
        float dist = Vector2.Distance(transform.position, targetPos);

        // If player is in slime form and in water and boss is blocked — use ranged attack if possible
        bool playerInWater = IsPlayerSlime() && isBlockedByWater;

        // Priority: Stomp > Melee > Laser > Chase
        if (isPhase2 && Time.time >= nextStompTime && dist <= stompRadius && !playerInWater)
            currentAttackCoroutine = StartCoroutine(DoStompAttack());
        else if (Time.time >= nextMeleeAttackTime && dist <= meleeAttackRange && !playerInWater)
            currentAttackCoroutine = StartCoroutine(DoMeleeAttack());
        else if (isPhase2 && Time.time >= nextLaserAttackTime && dist <= laserAttackRange)
            currentAttackCoroutine = StartCoroutine(DoLaserAttack()); // Laser can hit player in water
        else if (!isBlockedByWater)
            ChasePlayer();
        else
            StopMoving(); // Boss waits at water's edge
    }

    private void ChasePlayer()
    {
        if (player == null || isAttacking) return;

        Vector3 targetPos = GetPlayerTargetPosition();
        float dist = Vector2.Distance(transform.position, targetPos);
        if (dist <= stopDistance)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        float dir = Mathf.Sign(targetPos.x - transform.position.x);

        // Stop at water's edge — boss cannot enter water
        if (IsWaterAhead(dir))
        {
            isBlockedByWater = true;
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        isBlockedByWater = false;
        rb.linearVelocity = new Vector2(dir * currentMoveSpeed, rb.linearVelocity.y);
    }

    // Checks for water directly ahead (horizontal) and for water pits at floor level ahead
    private bool IsWaterAhead(float direction)
    {
        if (waterLayer == 0) return false; // Layer not assigned, skip check

        Vector2 bodyOrigin = (Vector2)transform.position + Vector2.up * waterCheckHeightOffset;

        // Horizontal body-level scan
        if (Physics2D.Raycast(bodyOrigin, new Vector2(direction, 0f), waterCheckDistance, waterLayer))
            return true;

        // Floor-level scan: check the ground one step ahead for a water surface
        Vector2 aheadFloor = new Vector2(transform.position.x + direction * waterCheckDistance,
                                          transform.position.y - 0.3f);
        if (Physics2D.OverlapPoint(aheadFloor, waterLayer) != null)
            return true;

        return false;
    }

    // Called when the boss accidentally lands in water (failsafe)
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Water"))
        {
            // Boss is heavy — immediately push it back up and out
            rb.linearVelocity = new Vector2(-rb.linearVelocity.x, 5f);
            isBlockedByWater = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Water"))
            isBlockedByWater = false;
    }

    private IEnumerator DoMeleeAttack()
    {
        isAttacking = true;
        isParryable = true;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

        if (anim != null) anim.SetTrigger(AnimMeleeAttack);

        float cooldown = meleeAttackCooldown / (isPhase2 ? phase2AttackSpeedMultiplier : 1f);
        nextMeleeAttackTime = Time.time + cooldown;

        // Wait for the visual hit frame then apply damage
        yield return new WaitForSeconds(0.4f);
        isParryable = false;
        DamageMelee();

        yield return new WaitForSeconds(cooldown - 0.4f);
        isAttacking = false;
        currentAttackCoroutine = null;
    }

    private IEnumerator DoLaserAttack()
    {
        isAttacking = true;
        isParryable = true;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

        if (anim != null) anim.SetTrigger(AnimLaserAttack);

        float cooldown = laserAttackCooldown / (isPhase2 ? phase2AttackSpeedMultiplier : 1f);
        nextLaserAttackTime = Time.time + cooldown;

        // Wait for the visual fire frame then spawn the laser
        yield return new WaitForSeconds(0.5f);
        isParryable = false;
        FireLaser();

        yield return new WaitForSeconds(cooldown - 0.5f);
        isAttacking = false;
        currentAttackCoroutine = null;
    }

    private IEnumerator DoStompAttack()
    {
        isAttacking = true;
        isParryable = true;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

        if (anim != null) anim.SetTrigger(AnimStomp);

        nextStompTime = Time.time + stompCooldown;

        // Wait for the visual impact frame then deal AoE damage
        yield return new WaitForSeconds(0.5f);
        isParryable = false;
        DamageStomped();

        yield return new WaitForSeconds(stompCooldown * 0.5f);
        isAttacking = false;
        currentAttackCoroutine = null;
    }

    // Called by coroutine (and optionally by MechBossAnimationEvents)
    public void DamageMelee()
    {
        if (player == null) return;

        Vector3 hitOrigin = meleeAttackPoint != null ? meleeAttackPoint.position : transform.position;
        float dist = Vector2.Distance(hitOrigin, GetPlayerTargetPosition());
        if (dist > meleeAttackRadius) return;

        PlayerHealth health = FindPlayerHealth();
        if (health != null)
        {
            health.TakeDamage(meleeDamage);

            PlayerMovement pm = health.GetComponent<PlayerMovement>();
            if (pm != null)
            {
                Vector2 knockbackDir = (player.position - transform.position).normalized;
                knockbackDir.y += 0.4f;
                pm.ApplyKnockback(knockbackDir.normalized);
            }
        }
    }

    // Called by coroutine (and optionally by MechBossAnimationEvents)
    public void FireLaser()
    {
        if (laserFirePoint == null || laserProjectilePrefab == null || player == null) return;

        // Aim at the active hitbox so the laser tracks both slime and human forms correctly
        Vector2 direction = ((Vector2)GetPlayerTargetPosition() - (Vector2)laserFirePoint.position).normalized;
        GameObject laser = Instantiate(laserProjectilePrefab, laserFirePoint.position, Quaternion.identity);

        BossLaserProjectile projectile = laser.GetComponent<BossLaserProjectile>();
        if (projectile != null)
            projectile.Initialize(direction, laserProjectileSpeed, laserDamage);
    }

    // Called by coroutine (and optionally by MechBossAnimationEvents)
    public void DamageStomped()
    {
        PlayerHealth health = FindPlayerHealth();
        if (health == null) return;

        float dist = Vector2.Distance(transform.position, health.transform.position);
        if (dist > stompRadius) return;

        health.TakeDamage(stompDamage);

        PlayerMovement pm = health.GetComponent<PlayerMovement>();
        if (pm != null)
        {
            Vector2 knockbackDir = (health.transform.position - transform.position).normalized;
            knockbackDir.y += 0.5f;
            pm.ApplyKnockback(knockbackDir.normalized);
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
        Debug.Log("[MechBossAI] DisableAI called. Stopping all coroutines and disabling AI.");
        StopAllCoroutines();
        isAttacking = true;
        rb.linearVelocity = Vector2.zero;
        enabled = false;
    }

    public void GetParried(Vector2 knockbackDir)
    {
        if (isStaggered) return;

        if (currentAttackCoroutine != null)
        {
            StopCoroutine(currentAttackCoroutine);
            currentAttackCoroutine = null;
        }

        isAttacking = true;
        isParryable = false;
        isStaggered = true;
        staggerTimer = bossStaggerDuration;

        // Apply knockback impulse — boss lurches back from the deflection
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(knockbackDir * parryKnockbackForce, ForceMode2D.Impulse);

        if (anim != null)
            anim.SetTrigger("stagger");

        Debug.Log($"[MechBossAI] Boss was parried! Knockback applied, staggering for {bossStaggerDuration}s.");
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

        // Water check rays
        Gizmos.color = Color.blue;
        Vector3 origin = transform.position + Vector3.up * waterCheckHeightOffset;
        Gizmos.DrawRay(origin, Vector3.right * waterCheckDistance);
        Gizmos.DrawRay(origin, Vector3.left * waterCheckDistance);
    }
}