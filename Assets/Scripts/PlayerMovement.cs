using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float maxMoveSpeed = 10f;
    public float acceleration = 20f;
    public float deceleration = 20f;
    public float crouchSpeedMultiplier = 0.5f;

    [Header("Jump Settings")]
    public float jumpForce = 16f;

    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Animator anim;
    
    // NEW: We need a reference to the Collider to shrink it!
    [SerializeField] private BoxCollider2D boxCol;

    [Header("Crouch Hitbox Settings")]
    // Set these in the Unity Inspector!
    public Vector2 crouchColliderSize = new Vector2(1f, 1f); 
    public Vector2 crouchColliderOffset = new Vector2(0f, 0.5f);

    // To remember what the collider looks like when standing
    private Vector2 standingColliderSize;
    private Vector2 standingColliderOffset;

    // State Variables
    private float horizontalInput;
    private bool isFacingRight = true;
    private bool isCrouching = false;

    void Start()
    {
        // Save the original standing collider size when the game starts
        standingColliderSize = boxCol.size;
        standingColliderOffset = boxCol.offset;
    }

    void Update()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        isCrouching = Input.GetAxisRaw("Vertical") < 0; 

        // NEW: Adjust the physical BoxCollider based on if we are crouching
        if (isCrouching)
        {
            boxCol.size = crouchColliderSize;
            boxCol.offset = crouchColliderOffset;
        }
        else
        {
            boxCol.size = standingColliderSize;
            boxCol.offset = standingColliderOffset;
        }

        if (Input.GetButton("Jump") && IsGrounded() && !isCrouching)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        if (Input.GetButtonUp("Jump") && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
        }

        UpdateAnimations();

        // Skid logic moved here so we don't flip backward while sliding
        bool isSkidding = false;
        if (Mathf.Abs(horizontalInput) > 0 && Mathf.Abs(rb.linearVelocity.x) > 1f)
        {
            if (Mathf.Sign(horizontalInput) != Mathf.Sign(rb.linearVelocity.x))
            {
                isSkidding = true;
            }
        }
        anim.SetBool("IsSkidding", isSkidding);

        // Only flip if we are NOT skidding
        if (!isSkidding)
        {
            Flip();
        }
    }

    private void FixedUpdate()
    {
        float targetSpeed = horizontalInput * maxMoveSpeed;
        if (isCrouching) targetSpeed *= crouchSpeedMultiplier;

        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;
        float currentSpeed = rb.linearVelocity.x;
        float newSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accelRate * Time.fixedDeltaTime);

        rb.linearVelocity = new Vector2(newSpeed, rb.linearVelocity.y);
    }

    private void UpdateAnimations()
    {
        anim.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x)); 
        anim.SetFloat("yVelocity", rb.linearVelocity.y); 
        anim.SetBool("IsGrounded", IsGrounded());
        anim.SetBool("IsCrouching", isCrouching);
    }

    private bool IsGrounded()
    {
        return Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
    }

    private void Flip()
    {
        if (isFacingRight && horizontalInput < 0f || !isFacingRight && horizontalInput > 0f)
        {
            isFacingRight = !isFacingRight;
            Vector3 localScale = transform.localScale;
            localScale.x *= -1f;
            transform.localScale = localScale;
        }
    }
}