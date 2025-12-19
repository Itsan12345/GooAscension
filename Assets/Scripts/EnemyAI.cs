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
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    private void Update()
    {
        CheckGrounded();
        ChasePlayer();
        HandleFlip();
        UpdateAnimations();
    }

    private void ChasePlayer()
    {
        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);
        if (distance <= stopDistance)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        // Check if player is directly above/below the enemy (horizontally aligned)
        float horizontalDifference = Mathf.Abs(player.position.x - transform.position.x);
        if (player.position.y > transform.position.y && horizontalDifference < horizontalAlignmentThreshold)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        float dir = Mathf.Sign(player.position.x - transform.position.x);
        rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);
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
}
