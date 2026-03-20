using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PlayerMovement : MonoBehaviour
{
    // =========================================================
    // References
    // =========================================================
    private Animator anim;
    private Rigidbody2D rb;
    private PlayerEnergy playerEnergy;
    // --- Read-only state for other scripts (PlayerCombat, etc.) ---
    public bool IsHuman => isHuman;
    public bool IsGrounded => isGrounded;
    public bool FacingRight => facingRight;
    public Animator CurrentAnimator => anim;

    // =========================================================
    // Movement
    // =========================================================
    [Header("Movement Settings")]
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float moveSpeed = 5f;

    private float xInput;
    private bool facingRight = true;
    private bool canMove = true;
    private bool canJump = true;

    // =========================================================
    // Dash
    // =========================================================
    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;

    private bool isDashing;
    private bool canDash = true;
    private float dashTimer;
    private float dashCooldownTimer;
    private int dashDirection;
    private bool isAttackingOrCharging = false; // Track if currently attacking/charging

    // =========================================================
    // Jump / Ground
    // =========================================================
    [Header("Jump Settings")]
    [SerializeField] private int maxJumpsHuman = 2;
    [SerializeField] private int maxJumpsSlime = 1;

    private int jumpsRemaining;
    private bool wasGrounded;

    [Header("Collision Detection")]
    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private float ventCheckDistance = 0.6f;
    [SerializeField] private LayerMask whatIsGround;

    private bool isGrounded;

    // =========================================================
    // Forms / Hitboxes / Animators
    // =========================================================
    [Header("Hitbox & Animator")]
    [SerializeField] private GameObject slimeHitbox;
    [SerializeField] private GameObject slimeAnimator;
    [SerializeField] private GameObject humanHitbox;
    [SerializeField] private GameObject humanAnimator;

    private bool isHuman;                 // Start as Slime (false)
    private bool canTransform;            // Enable after Code Fragment
    private bool isDead;                  // Set on death; blocks all input and transformation
    private Vector2 preservedVelocity;

    private const string KEY_PENDING_DEATH_RELOAD = "PendingDeathReload";

    [Header("Flip Visuals")]
    [Tooltip("If a Light2D is a child of the player, flip it when turning.")]
    [SerializeField] private bool flipLight2D = true;

    private Light2D[] light2DsToFlip;
    private Vector3[] light2DBaseLocalEuler;

    [Header("Transformation")]
    [SerializeField] private float transformCost = 25f;
    [SerializeField] private int transformBlinkCount = 8;
    [SerializeField] private float transformBlinkInterval = 0.07f;

    [Header("Level / Dev Settings")]
    [Tooltip("Tick ON for Level 1. Resets the transform-unlock flag so the player always starts locked, " +
             "regardless of what was saved in PlayerPrefs from a previous session.")]
    [SerializeField] private bool lockTransformInThisScene = false;

    private bool isTransforming = false;



    // =========================================================
    // Water Physics
    // =========================================================
    [Header("Water Physics")]
    [SerializeField] private float slimeBuoyancy = 15f; // upward force for slime
    [SerializeField] private float humanWeight = 5f;    // downward clamp for human
    [SerializeField] private float waterDrag = 2f;

    private bool isInWater;

    // =========================================================
    // Knockback
    // =========================================================
    [Header("Knockback Settings")]
    [SerializeField] private float knockbackForce = 10f;

    // =========================================================
    // Unity lifecycle
    // =========================================================
    private void Awake()
    {
        playerEnergy = GetComponent<PlayerEnergy>();

        if (flipLight2D)
        {
            light2DsToFlip = GetComponentsInChildren<Light2D>(true);
            light2DBaseLocalEuler = new Vector3[light2DsToFlip.Length];

            for (int i = 0; i < light2DsToFlip.Length; i++)
            {
                var l = light2DsToFlip[i];
                if (l != null)
                    light2DBaseLocalEuler[i] = l.transform.localEulerAngles;
            }
        }

        // Level 1 dev mode: lock transformation on initial entry,
        // but DO NOT wipe unlock data on death reloads.
        bool isDeathReload = PlayerPrefs.GetInt(KEY_PENDING_DEATH_RELOAD, 0) == 1;

        if (lockTransformInThisScene && !isDeathReload)
        {
            PlayerPrefs.SetInt("TransformUnlocked", 0);
            PlayerPrefs.SetInt("PlayerIsHuman", 0);
            PlayerPrefs.Save();
        }

        bool transformUnlocked = PlayerPrefs.GetInt("TransformUnlocked", 0) == 1;
        bool wasHuman          = PlayerPrefs.GetInt("PlayerIsHuman", 0) == 1;

        // Restore current form + ability from PlayerPrefs.
        // If Level 1 cleared the prefs above, this will start as locked slime.
        SetForm(transformUnlocked && wasHuman);
        canTransform = transformUnlocked;

        // Clear the marker after we used it, so the next (non-death) Level 1 entry locks again.
        if (isDeathReload)
        {
            PlayerPrefs.SetInt(KEY_PENDING_DEATH_RELOAD, 0);
            PlayerPrefs.Save();
        }

        playerLayer = gameObject.layer;
        enemyLayer = LayerMask.NameToLayer(enemyLayerName);

        if (enemyLayer == -1)
            Debug.LogError($"Enemy layer '{enemyLayerName}' does not exist. Create it in Layers.");
    }

    private void Update()
    {
        HandleInput();
        HandleDash();
        HandleMovement();
        HandleAnimations();
        HandleFlip();
        HandleCollision();
    }

    // =========================================================
    // Public API
    // =========================================================
    public void EnableMovementAndJump(bool enable)
    {
        canJump = enable;
        canMove = enable;
    }

    /// <summary>
    /// Called by PlayerHealth.Die(). Locks out all input and transformation
    /// so the death sequence cannot be interrupted or corrupted.
    /// </summary>
    public void SetDead()
    {
        isDead  = true;
        canMove = false;
        canJump = false;

        // Stop any in-progress transformation immediately so children stay disabled
        // and PlayerPrefs is not overwritten with the wrong form.
        if (isTransforming)
        {
            StopAllCoroutines();
            isTransforming = false;

            // Restore the form the player had before they started transforming
            SetForm(isHuman);
        }
    }

    public float GetDashCooldownRemaining()
    {
        return canDash ? 0f : Mathf.Max(0f, dashCooldownTimer);
    }

    public float GetDashCooldownDuration()
    {
        return dashCooldown;
    }

   

    public void ApplyKnockback(Vector2 direction)
    {
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(direction * knockbackForce, ForceMode2D.Impulse);
    }

    /// <summary>
    /// Grants temporary i-frames (e.g. after a successful parry).
    /// Safe to call while dashing — dash i-frames will not be cleared early.
    /// </summary>
    public void SetInvulnerable(float duration)
    {
        IsInvulnerable = true;
        parryInvulnerableTimer = duration;
    }

    // =========================================================
    // Input
    // =========================================================
    private void HandleInput()
    {
        if (isDead) return;

        xInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetKeyDown(KeyCode.Space))
            TryToJump();

        if (Input.GetKeyDown(KeyCode.LeftShift))
            TryToDash();

        if (Input.GetKeyDown(KeyCode.E))
            SwitchForm();
    }

    // =========================================================
    // Movement / Jump / Dash
    // =========================================================
    private void HandleMovement()
    {
        ApplyWaterPhysics();

        if (isDashing)
            return;

        if (canMove)
            rb.linearVelocity = new Vector2(xInput * moveSpeed, rb.linearVelocity.y);
        else if (!isTransforming || isGrounded)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    private void TryToJump()
    {
        if (!canJump)
            return;

        if (isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpsRemaining--;
            return;
        }

        // Air jump for human only
        if (isHuman && jumpsRemaining > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpsRemaining--;
        }
    }

    [Header("Dash I-Frames")]
    [SerializeField] private bool dashInvulnerable = true;
    [SerializeField] private bool dashThroughEnemies = true;

    // Set this to whatever layer your enemies use (ex: "Enemy")
    [SerializeField] private string enemyLayerName = "Enemy";

    private int playerLayer;
    private int enemyLayer;
    public bool IsInvulnerable { get; private set; }
    private float parryInvulnerableTimer = 0f;

    private void TryToDash()
    {
        if (!isHuman) return;

        if (canDash && !isDashing)
        {
            isDashing = true;
            canDash = false;

            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown;
            dashDirection = facingRight ? 1 : -1;

            // Enable i-frames + pass-through ONLY when dash starts
            if (dashInvulnerable)
                IsInvulnerable = true;

            if (dashThroughEnemies && enemyLayer != -1)
                Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, true);

            // Set animator parameter to allow dash transition
            if (anim != null)
            {
                anim.SetBool("allowDash", true);
                anim.SetTrigger("dash");
            }
        }
    }


    private void HandleDash()
    {
        // Parry i-frame countdown
        if (parryInvulnerableTimer > 0f)
        {
            parryInvulnerableTimer -= Time.deltaTime;
            if (parryInvulnerableTimer <= 0f && !isDashing)
                IsInvulnerable = false;
        }

        if (!canDash)
        {
            dashCooldownTimer -= Time.deltaTime;
            if (dashCooldownTimer <= 0f)
                canDash = true;
        }

        if (!isDashing)
            return;

        dashTimer -= Time.deltaTime;

        if (dashTimer <= 0f)
        {
            isDashing = false;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

            // Disable animator parameter to prevent dash from being triggered again
            if (anim != null)
                anim.SetBool("allowDash", false);

            // Disable i-frames + pass-through ONLY when dash ends
            if (dashInvulnerable)
                IsInvulnerable = false;

            if (dashThroughEnemies && enemyLayer != -1)
                Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, false);

            return;
        }

        rb.linearVelocity = new Vector2(dashDirection * dashSpeed, 0f);
    }

    private void OnDisable()
    {
        IsInvulnerable = false;

        if (dashThroughEnemies && enemyLayer != -1)
            Physics2D.IgnoreLayerCollision(gameObject.layer, enemyLayer, false);
    }

    /// <summary>
    /// Called by PlayerCombat to notify when attacking/charging state changes
    /// </summary>
    public void SetAttackingOrCharging(bool attacking)
    {
        isAttackingOrCharging = attacking;
        
        // Disable dash transition when attacking/charging
        if (anim != null)
            anim.SetBool("allowDash", false);
    }

    // =========================================================
    // Collision / Ground checks
    // =========================================================
    private void HandleCollision()
    {
        // Active hitbox collider determines the feet position
        Collider2D col = isHuman
            ? humanHitbox.GetComponent<Collider2D>()
            : slimeHitbox.GetComponent<Collider2D>();

        Vector2 raycastOrigin = new Vector2(rb.transform.position.x, col.bounds.min.y);

        isGrounded = Physics2D.Raycast(raycastOrigin, Vector2.down, groundCheckDistance, whatIsGround);

        Debug.DrawRay(raycastOrigin, Vector2.down * groundCheckDistance, isGrounded ? Color.green : Color.red);

        // Reset jump counter on landing
        if (isGrounded && !wasGrounded)
            jumpsRemaining = isHuman ? maxJumpsHuman : maxJumpsSlime;

        wasGrounded = isGrounded;
    }

    private bool IsConfinedSpace()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.up, ventCheckDistance, whatIsGround);
        return hit.collider != null;
    }

    // =========================================================
    // Anim / Flip
    // =========================================================
    private void HandleAnimations()
    {
        if (anim == null || rb == null)
            return;

        anim.SetFloat("xVelocity", rb.linearVelocity.x);
        anim.SetFloat("yVelocity", rb.linearVelocity.y);
        anim.SetBool("isGrounded", isGrounded);
    }

    private void HandleFlip()
    {
        if (xInput > 0 && !facingRight) Flip();
        else if (xInput < 0 && facingRight) Flip();
    }

    private void Flip()
    {
        facingRight = !facingRight;
        ApplyFacingRotation();
    }

    // Rotates only the visual animator children so the root transform stays
    // at zero rotation — this keeps minimap icons and other root children unaffected.
    private void ApplyFacingRotation()
    {
        Quaternion rot = facingRight ? Quaternion.identity : Quaternion.Euler(0, 180, 0);
        if (humanAnimator != null) humanAnimator.transform.localRotation = rot;
        if (slimeAnimator  != null) slimeAnimator.transform.localRotation = rot;

        if (flipLight2D && light2DsToFlip != null)
        {
            float yAdd = facingRight ? 0f : 180f;
            for (int i = 0; i < light2DsToFlip.Length; i++)
            {
                Light2D l = light2DsToFlip[i];
                if (l != null)
                {
                    Vector3 baseEuler = (light2DBaseLocalEuler != null && i < light2DBaseLocalEuler.Length)
                        ? light2DBaseLocalEuler[i]
                        : l.transform.localEulerAngles;

                    // Preserve the original Z offset (and any X), and only flip around Y.
                    l.transform.localRotation = Quaternion.Euler(baseEuler.x, baseEuler.y + yAdd, baseEuler.z);
                }
            }
        }
    }

    

    // =========================================================
    // Water
    // =========================================================
    private void ApplyWaterPhysics()
    {
        if (!isInWater || rb == null)
            return;

        if (!isHuman)
        {
            rb.AddForce(Vector2.up * slimeBuoyancy);
        }
        else
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, -humanWeight));
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Water"))
            return;

        isInWater = true;
        rb.linearDamping = waterDrag;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Water"))
            return;

        isInWater = false;
        rb.linearDamping = 0f;
    }

    // =========================================================
    // Slime ↔ Human Transformation
    // =========================================================
    public void EnableHumanTransformation()
    {
        canTransform = true;
        PlayerPrefs.SetInt("TransformUnlocked", 1);
        PlayerPrefs.Save();
        Debug.Log("Human transformation unlocked! Press E to transform.");
    }

    private void SwitchForm()
    {
        if (isDead) return;
        if (isTransforming) return;

        // Prevent morphing while attacking or charging (e.g., charged sword/gun)
        if (isAttackingOrCharging)
        {
            Debug.Log("Cannot transform while attacking or charging.");
            return;
        }

        if (playerEnergy != null && !playerEnergy.CanAffordTransform(transformCost))
        {
            Debug.Log("Not enough energy to transform!");
            return;
        }

        if (!canTransform)
        {
            Debug.Log("Cannot transform yet - need Code Fragment!");
            return;
        }

        if (!isHuman && IsConfinedSpace())
        {
            Debug.Log("Cannot transform - confined space above!");
            return;
        }

        preservedVelocity = rb.linearVelocity;

        StartCoroutine(TransformCoroutine(!isHuman));
    }

    private IEnumerator TransformCoroutine(bool toHuman)
    {
        isTransforming = true;
        bool lockedMovement = isGrounded;
        if (lockedMovement)
        {
            canMove = false;
            canJump = false;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        // Keep the player's "feet" at the same world Y when swapping between
        // different hitboxes (human vs slime). Without this, different collider
        // sizes/pivots can cause the player to appear below ground.
        float feetYBefore = GetActiveHitboxFeetWorldY();

        // Cache both animators up front
        Animator humanAnim = humanAnimator.GetComponent<Animator>();
        Animator slimeAnim = slimeAnimator.GetComponent<Animator>();

        // --- Rapid blink: alternate between slime and human animators ---
        for (int i = 0; i < transformBlinkCount; i++)
        {
            bool showHuman = (i % 2 == 0) ? toHuman : !toHuman;
            slimeAnimator.SetActive(!showHuman);
            humanAnimator.SetActive(showHuman);
            anim = showHuman ? humanAnim : slimeAnim;
            yield return new WaitForSeconds(transformBlinkInterval);
        }

        // --- Scale punch (Super Mario "pop" effect) ---
        float baseAbsX = Mathf.Abs(transform.localScale.x);
        float baseScaleY = transform.localScale.y;
        float baseScaleZ = transform.localScale.z;

        float punchDuration = 0.15f;
        float elapsed = 0f;
        while (elapsed < punchDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / punchDuration;
            float scaleMult = 1f + Mathf.Sin(t * Mathf.PI) * 0.3f;
            transform.localScale = new Vector3(baseAbsX * scaleMult, baseScaleY * scaleMult, baseScaleZ);
            yield return null;
        }
        transform.localScale = new Vector3(baseAbsX, baseScaleY, baseScaleZ);

        // --- Commit final form ---
        isHuman = toHuman;
        slimeHitbox.SetActive(!toHuman);
        humanHitbox.SetActive(toHuman);
        slimeAnimator.SetActive(!toHuman);
        humanAnimator.SetActive(toHuman);

        // After enabling the new hitbox, align the new collider bottom to where
        // the old collider bottom was.
        AlignFeetToWorldY(feetYBefore);

        // Save current form so the next scene restores it correctly
        PlayerPrefs.SetInt("PlayerIsHuman", toHuman ? 1 : 0);
        PlayerPrefs.Save();

        rb = GetComponent<Rigidbody2D>();
        anim = toHuman ? humanAnim : slimeAnim;

        jumpsRemaining = toHuman ? maxJumpsHuman : maxJumpsSlime;
        canDash = true;

        if (lockedMovement)
        {
            canMove = true;
            canJump = true;
        }
        isTransforming = false;

        Debug.Log("Transformed to " + (toHuman ? "Human" : "Slime"));

        if (playerEnergy != null)
            playerEnergy.SpendEnergy(transformCost);
    }

    private float GetActiveHitboxFeetWorldY()
    {
        GameObject hb = isHuman ? humanHitbox : slimeHitbox;
        if (hb == null)
            return rb != null ? rb.position.y : transform.position.y;

        Collider2D col = hb.GetComponentInChildren<Collider2D>();
        if (col == null)
            return rb != null ? rb.position.y : transform.position.y;

        return col.bounds.min.y;
    }

    private void AlignFeetToWorldY(float targetFeetY)
    {
        if (rb == null)
            return;

        GameObject hb = isHuman ? humanHitbox : slimeHitbox;
        if (hb == null)
            return;

        Collider2D col = hb.GetComponentInChildren<Collider2D>();
        if (col == null)
            return;

        float currentFeetY = col.bounds.min.y;
        float delta = targetFeetY - currentFeetY;

        // Avoid tiny jitter due to bounds precision.
        if (Mathf.Abs(delta) < 0.0001f)
            return;

        rb.position = rb.position + Vector2.up * delta;
    }

    private void SetForm(bool human)
    {
        isHuman = human;

        slimeHitbox.SetActive(!human);
        slimeAnimator.SetActive(!human);

        humanHitbox.SetActive(human);
        humanAnimator.SetActive(human);

        rb   = GetComponent<Rigidbody2D>();
        anim = human ? humanAnimator.GetComponent<Animator>() : slimeAnimator.GetComponent<Animator>();

        jumpsRemaining = human ? maxJumpsHuman : maxJumpsSlime;

        // Re-apply facing direction so the new form looks the correct way
        ApplyFacingDirection();
    }

    private void ApplyFacingDirection()
    {
        ApplyFacingRotation();
    }

    // =========================================================
    // Flash support
    // =========================================================
    public SpriteRenderer GetActiveSpriteRenderer()
    {
        if (isHuman && humanAnimator != null)
            return humanAnimator.GetComponent<SpriteRenderer>();

        if (!isHuman && slimeAnimator != null)
            return slimeAnimator.GetComponent<SpriteRenderer>();

        Debug.LogWarning("Could not find active SpriteRenderer for flash effect.");
        return null;
    }

    // =========================================================
    // Gizmos
    // =========================================================
    

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, new Vector2(transform.position.x, transform.position.y - groundCheckDistance));

        bool isConfined = Physics2D.Raycast(transform.position, Vector2.up, ventCheckDistance, whatIsGround);
        Gizmos.color = isConfined ? Color.yellow : Color.blue;
        Gizmos.DrawLine(transform.position, new Vector2(transform.position.x, transform.position.y + ventCheckDistance));
    }
}
