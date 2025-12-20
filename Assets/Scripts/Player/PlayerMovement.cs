using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class PlayerMovement : MonoBehaviour
{
    private Animator anim;
    private Rigidbody2D rb;

    [Header("Movement Settings")]
    private float xInput;   
    private bool facingRight = true;
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float moveSpeed = 5f;
    private bool canMove = true;
    private bool canJump = true;

    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;
    private bool isDashing = false;
    private bool canDash = true;
    private float dashTimer;
    private float dashCooldownTimer;
    private int dashDirection;

    [Header("Jump Settings")]
    private int jumpsRemaining = 0;
    private int maxJumpsHuman = 2;
    private int maxJumpsSlime = 1;
    private bool wasGrounded = false;

    [Header("Collision Detection")]
    [SerializeField] private float groundCheckDistance;
    [SerializeField] private float ventCheckDistance;
    private bool isGrounded;
    [SerializeField] private LayerMask whatIsGround;

    [Header("Hitbox & Animator")]
    [SerializeField] private GameObject slimeHitbox;
    [SerializeField] private GameObject slimeAnimator;
    [SerializeField] private GameObject humanHitbox;
    [SerializeField] private GameObject humanAnimator;


    [Header("Weapon System")]
[SerializeField] private GameObject bulletPrefab;
[SerializeField] private Transform firePoint;
[SerializeField] private float shootCooldown = 0.3f;


[Header("Water Physics")]
[SerializeField] private float slimeBuoyancy = 15f;   // upward force for slime
[SerializeField] private float humanWeight = 5f;      // downward force for human
[SerializeField] private float waterDrag = 2f;        // slow movement in water
private bool isInWater = false;

private bool usingGun = false;
private bool canShoot = true;

    private bool isHuman = false;        // Start as Slime
    private bool canTransform = false;   // Enable after Code Fragment
    private Vector2 preservedVelocity;

    void Awake()
    {
        SetForm(false); // Start as Slime - this will set rb and anim
    }

    private void Update()
    {
        HandleInput();
        HandleMovement();
        HandleAnimations();
        HandleFlip();
        HandleCollision();
        HandleDash();
    }

    public void EnableMovementAndJump(bool enable)
    {
        canJump = enable;
        canMove = enable;
    }

    private void HandleAnimations()
    {
        anim.SetFloat("xVelocity", rb.linearVelocity.x);
        anim.SetFloat("yVelocity", rb.linearVelocity.y);
        anim.SetBool("isGrounded", isGrounded);
    }

    private void HandleInput()
    {
        xInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetKeyDown(KeyCode.Space))
            TryToJump();

        if (Input.GetKeyDown(KeyCode.Mouse0))
{
    if (isHuman && usingGun)
        TryToShoot();
    else
        TryToAttack();
}

if (Input.GetKeyDown(KeyCode.Q))
    SwitchWeapon();

        if (Input.GetKeyDown(KeyCode.LeftShift))
            TryToDash();    

        if (Input.GetKeyDown(KeyCode.E))
            SwitchForm(); // Press E to transform
    }

    private void TryToDash()
    {
        // Only human form can dash
        if (!isHuman)
            return;

        if (canDash && !isDashing)
        {
            isDashing = true;
            canDash = false;
            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown;
            dashDirection = facingRight ? 1 : -1;

            if (isGrounded)
                anim.SetTrigger("dash");
        }
    }


    private void TryToShoot()
{
    if (!canShoot || !isHuman)
        return;

    if (firePoint == null || bulletPrefab == null)
    {
        Debug.LogError("FirePoint or BulletPrefab is missing!");
        return;
    }

    canShoot = false;

    anim.SetTrigger("shoot");

    GameObject bullet = Instantiate(
        bulletPrefab,
        firePoint.position,
        Quaternion.identity
    );

    // Set bullet z-position to be above background
    Vector3 bulletPos = bullet.transform.position;
    bulletPos.z = -1f;
    bullet.transform.position = bulletPos;

    // Set sorting order above background
    SpriteRenderer bulletSprite = bullet.GetComponent<SpriteRenderer>();
    if (bulletSprite != null)
    {
        bulletSprite.sortingOrder = 10;
    }

    Bullet bulletScript = bullet.GetComponent<Bullet>();
    if (bulletScript != null)
    {
        float dir = facingRight ? 1f : -1f;
        bulletScript.SetDirection(dir);
    }

    Invoke(nameof(ResetShoot), shootCooldown);
}

private void ResetShoot()
{
    canShoot = true;
}



private void SwitchWeapon()
{
    if (!isHuman)
        return;

    usingGun = !usingGun;
    anim.SetTrigger("switchWeapon");

    Debug.Log(usingGun ? "Switched to GUN" : "Switched to SWORD");
}



    private void TryToAttack()
{
    if (!usingGun && isGrounded)
        anim.SetTrigger("attack");
}

    private void TryToJump()
    {
        if (!canJump)
            return;

        // Jump if grounded
        if (isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpsRemaining--;
        }
        // Air jump for human form only
        else if (isHuman && jumpsRemaining > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpsRemaining--;
        }
    }

    private void HandleDash()
    {
        if (!canDash)
        {
            dashCooldownTimer -= Time.deltaTime;
            if (dashCooldownTimer <= 0)
                canDash = true;
        }

        if (isDashing)
        {
            dashTimer -= Time.deltaTime;

            if (dashTimer <= 0)
            {
                isDashing = false;
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            }
            else
            {
                rb.linearVelocity = new Vector2(dashDirection * dashSpeed, 0);
            }
        }
    }

    private void HandleMovement()
    {


        // Apply water physics
if (isInWater)
{
    if (!isHuman) // Slime: float
    {
        rb.AddForce(Vector2.up * slimeBuoyancy);
    }
    else // Human: sink naturally
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, -humanWeight));
    }
}


        if (isDashing) return;

        if(canMove)
            rb.linearVelocity = new Vector2(xInput * moveSpeed, rb.linearVelocity.y);
        else
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
    }

    private void HandleCollision()
    {
        // Get collider from active hitbox
        Collider2D col = isHuman ? humanHitbox.GetComponent<Collider2D>() : slimeHitbox.GetComponent<Collider2D>();
        Vector2 raycastOrigin = new Vector2(rb.transform.position.x, col.bounds.min.y);
        
        isGrounded = Physics2D.Raycast(raycastOrigin, Vector2.down, 0.1f, whatIsGround);
        
        Debug.DrawRay(raycastOrigin, Vector2.down * 0.1f, isGrounded ? Color.green : Color.red);
        
        // Reset jump counter when transitioning from air to ground
        if (isGrounded && !wasGrounded)
        {
            jumpsRemaining = isHuman ? maxJumpsHuman : maxJumpsSlime;
        }
        
        wasGrounded = isGrounded;
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
        
        // Rotate hitboxes to face direction (Y rotation 0 for right, 180 for left)
        Quaternion newRotation = facingRight ? Quaternion.Euler(0, 0, 0) : Quaternion.Euler(0, 180, 0);
        
        slimeHitbox.transform.rotation = newRotation;
        humanHitbox.transform.rotation = newRotation;
    }

    private void OnDrawGizmos()
    {
        // Ground check (downward)
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, new Vector2(transform.position.x, transform.position.y - groundCheckDistance));
        
        // Vent/confined space check (upward)
        bool isConfined = Physics2D.Raycast(transform.position, Vector2.up, ventCheckDistance, whatIsGround);
        Gizmos.color = isConfined ? Color.yellow : Color.blue;
        Gizmos.DrawLine(transform.position, new Vector2(transform.position.x, transform.position.y + ventCheckDistance));
    }

    private bool IsConfinedSpace()
    {
        // Check if there's a collider above within ventCheckDistance
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.up, ventCheckDistance, whatIsGround);
        return hit.collider != null;
    }

    #region Slime ↔ Human Transformation
    public void EnableHumanTransformation()
    {
        canTransform = true;
        Debug.Log("Human transformation unlocked! Press E to transform.");
    }

    private void SwitchForm()
    {
        if (!canTransform)
        {
            Debug.Log("Cannot transform yet - need Code Fragment!");
            usingGun = false; // Reset to sword when transforming
            return;
        }

        // Check if confined space above (vent check)
        if (!isHuman && IsConfinedSpace())
        {
            Debug.Log("Cannot transform - confined space above!");
            return;
        }

        // Preserve velocity
        preservedVelocity = rb.linearVelocity;

        // Sync positions before switching forms
        Vector3 currentPosition = rb.transform.position;
        if (isHuman)
            slimeHitbox.transform.position = currentPosition;
        else
            humanHitbox.transform.position = currentPosition;

        // Toggle form
        isHuman = !isHuman;

        // Swap Hitboxes
        slimeHitbox.SetActive(!isHuman);
        humanHitbox.SetActive(isHuman);

        // Swap Animators
        slimeAnimator.SetActive(!isHuman);
        humanAnimator.SetActive(isHuman);

        // Update Rigidbody2D and Animator references
        rb = GetComponent<Rigidbody2D>();
        anim = isHuman ? humanAnimator.GetComponent<Animator>() : slimeAnimator.GetComponent<Animator>();

        // Reset jump counter and dash when transforming
        jumpsRemaining = isHuman ? maxJumpsHuman : maxJumpsSlime;
        canDash = true;

        // Restore velocity
        if (rb != null)
            rb.linearVelocity = preservedVelocity;

        Debug.Log("Transformed to " + (isHuman ? "Human" : "Slime"));
    }


    private void OnTriggerEnter2D(Collider2D collision)
{
    if (collision.CompareTag("Water"))
    {
        isInWater = true;
        rb.linearDamping = waterDrag; // slow horizontal movement
    }
}

private void OnTriggerExit2D(Collider2D collision)
{
    if (collision.CompareTag("Water"))
    {
        isInWater = false;
        rb.linearDamping = 0f; // reset drag
    }
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
        
        // Set jump counter based on form
        jumpsRemaining = human ? maxJumpsHuman : maxJumpsSlime;
    }
    #endregion

    //Getting hit flash effect
    public SpriteRenderer GetActiveSpriteRenderer()
    {
        if (isHuman && humanAnimator != null)
        {
            // Assuming the SpriteRenderer is on the same object as the Animator
            return humanAnimator.GetComponent<SpriteRenderer>();
        }
        else if (!isHuman && slimeAnimator != null)
        {
            return slimeAnimator.GetComponent<SpriteRenderer>();
        }

        // Fallback if something is missing
        Debug.LogWarning("Could not find active SpriteRenderer for flash effect.");
        return null;
    }
}
