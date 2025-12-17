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
    private bool isGrounded;
    [SerializeField] private LayerMask whatIsGround;

    [Header("Hitbox & Animator")]
    [SerializeField] private GameObject slimeHitbox;
    [SerializeField] private GameObject slimeAnimator;
    [SerializeField] private GameObject humanHitbox;
    [SerializeField] private GameObject humanAnimator;

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
            TryToAttack();

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

    private void TryToAttack()
    {
        if (isGrounded)
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
        if (isDashing) return;

        if(canMove)
            rb.linearVelocity = new Vector2(xInput * moveSpeed, rb.linearVelocity.y);
        else
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
    }

    private void HandleCollision()
    {
        // Cast from the bottom of the collider
        Collider2D col = rb.GetComponent<Collider2D>();
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
        // Flip BOTH slime and human animators to keep them in sync
        Vector3 slimeScale = slimeAnimator.transform.localScale;
        slimeScale.x *= -1;
        slimeAnimator.transform.localScale = slimeScale;

        Vector3 humanScale = humanAnimator.transform.localScale;
        humanScale.x *= -1;
        humanAnimator.transform.localScale = humanScale;

        facingRight = !facingRight;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, new Vector2(transform.position.x, transform.position.y - groundCheckDistance));
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
        rb = isHuman ? humanHitbox.GetComponent<Rigidbody2D>() : slimeHitbox.GetComponent<Rigidbody2D>();
        anim = isHuman ? humanAnimator.GetComponent<Animator>() : slimeAnimator.GetComponent<Animator>();

        // Reset jump counter and dash when transforming
        jumpsRemaining = isHuman ? maxJumpsHuman : maxJumpsSlime;
        canDash = true;

        // Restore velocity
        if (rb != null)
            rb.linearVelocity = preservedVelocity;

        Debug.Log("Transformed to " + (isHuman ? "Human" : "Slime"));
    }

    private void SetForm(bool human)
    {
        isHuman = human;
        slimeHitbox.SetActive(!human);
        slimeAnimator.SetActive(!human);
        humanHitbox.SetActive(human);
        humanAnimator.SetActive(human);

        rb = human ? humanHitbox.GetComponent<Rigidbody2D>() : slimeHitbox.GetComponent<Rigidbody2D>();
        anim = human ? humanAnimator.GetComponent<Animator>() : slimeAnimator.GetComponent<Animator>();
        
        // Set jump counter based on form
        jumpsRemaining = human ? maxJumpsHuman : maxJumpsSlime;
    }
    #endregion
}
