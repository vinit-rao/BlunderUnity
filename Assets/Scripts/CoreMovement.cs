using UnityEngine;

public class CoreMovement : MonoBehaviour
{
    [Header("Movement Stats")]
    [SerializeField] private float maxSpeed = 8f;
    [SerializeField] private float accelForce = 40f;
    [SerializeField] private float decelForce = 60f;

    private Rigidbody2D rb;
    private float moveInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // will output 1, 0, -1
        moveInput = Input.GetAxisRaw("Horizontal");
    }

    void FixedUpdate()
    {
        // 1. How fast do we WANT to be going right now?
        float targetSpeed = moveInput * maxSpeed;

        // 2. Are we pressing a key (accelerating) or letting go (decelerating)?
        float currentAccel = (Mathf.Abs(moveInput) > 0.01f) ? acceleration : deceleration;

        // 3. Calculate the smooth transition from our current speed to our target speed
        float newSpeed = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, currentAccel * Time.fixedDeltaTime);

        // 4. Apply the new speed to the Rigidbody, leaving gravity (Y) exactly as it is
        rb.linearVelocity = new Vector2(newSpeed, rb.linearVelocity.y);
    }
}