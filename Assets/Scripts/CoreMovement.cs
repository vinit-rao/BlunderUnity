using UnityEngine;

public class CoreMovement : MonoBehaviour
{
    [Header("Movement Stats")]
    [SerializeField] private float maxSpeed = 20f;
    [SerializeField] private float accelForce = 30f;
    [SerializeField] private float decelForce = 50f;
    [SerializeField] private float jumpForce = 10f;
    private Rigidbody2D rb;
    private Collider2D collision;
    private float hInput;
    private float vInput;

    private bool canJump;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        collision = GetComponent<Collider2D>();
    }

    void Update()
    {
        // will output 1, 0, -1
        hInput = Input.GetAxisRaw("Horizontal");

        vInput = Input.GetAxisRaw("Vertical");
    }

    void FixedUpdate()
    {
        // direction * speed value
        float targetSpeed = hInput * maxSpeed;

        // if key pressed or not it decides whether to accelerate or decelerate
        float accelCurrent = (Mathf.Abs(hInput) > 0.01f) ? accelForce : decelForce;

        // using time to transition the current player speed with the target speed
        float newSpeed = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, accelCurrent * Time.fixedDeltaTime);

        // only applying to x value of rb
        rb.linearVelocity = new Vector2(newSpeed, rb.linearVelocity.y);

        // Debug.Log(accelCurrent);
        // Debug.Log(newSpeed);
        // Debug.Log(canJump);

        if (vInput > 0f && canJump == true)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode2D.Impulse);
        }
    }


    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            canJump = true;
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            canJump = false;
        }
    }
}