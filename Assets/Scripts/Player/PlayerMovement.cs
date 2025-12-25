using UnityEngine;

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
    [SerializeField] private float ventCheckDistance = 1f;
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
    private Vector2 preservedVelocity;

    [Header("Transformation")]
    [SerializeField] private float transformCost = 25f;

   

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
        SetForm(false); // Start as Slime

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

   

    public void ApplyKnockback(Vector2 direction)
    {
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(direction * knockbackForce, ForceMode2D.Impulse);
    }

    // =========================================================
    // Input
    // =========================================================
    private void HandleInput()
    {
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
        else
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

            if (isGrounded && anim != null)
                anim.SetTrigger("dash");
        }
    }


    private void HandleDash()
    {
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
        if (rb.linearVelocity.x > 0 && !facingRight) Flip();
        else if (rb.linearVelocity.x < 0 && facingRight) Flip();
    }

    private void Flip()
    {
        facingRight = !facingRight;

        Vector3 scale = transform.localScale;
        scale.x *= -1f;
        transform.localScale = scale;
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
        Debug.Log("Human transformation unlocked! Press E to transform.");
    }

    private void SwitchForm()
    {
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

        // Sync hitbox positions
        Vector3 pos = rb.transform.position;
        if (isHuman) slimeHitbox.transform.position = pos;
        else humanHitbox.transform.position = pos;

        isHuman = !isHuman;

        slimeHitbox.SetActive(!isHuman);
        humanHitbox.SetActive(isHuman);

        slimeAnimator.SetActive(!isHuman);
        humanAnimator.SetActive(isHuman);

        rb = GetComponent<Rigidbody2D>();
        anim = isHuman ? humanAnimator.GetComponent<Animator>() : slimeAnimator.GetComponent<Animator>();

        jumpsRemaining = isHuman ? maxJumpsHuman : maxJumpsSlime;
        canDash = true;

        rb.linearVelocity = preservedVelocity;

        Debug.Log("Transformed to " + (isHuman ? "Human" : "Slime"));

        if (playerEnergy != null)
            playerEnergy.SpendEnergy(transformCost);
    }

    private void SetForm(bool human)
    {
        isHuman = human;

        slimeHitbox.SetActive(!human);
        slimeAnimator.SetActive(!human);

        humanHitbox.SetActive(human);
        humanAnimator.SetActive(human);

        rb = GetComponent<Rigidbody2D>();
        anim = human ? humanAnimator.GetComponent<Animator>() : slimeAnimator.GetComponent<Animator>();

        jumpsRemaining = human ? maxJumpsHuman : maxJumpsSlime;
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
