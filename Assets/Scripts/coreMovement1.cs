using UnityEngine;

public class coreMovement1 : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed             = 5f;
    [SerializeField] private float crouchSpeedMultiplier = 0.5f;
    // Character class multiplier — set per-character in the Inspector
    // (acts as a stat multiplier on moveSpeed per GDD)
    [SerializeField] private float classSpeedMultiplier  = 1f;

    [Header("Jump")]
    [SerializeField] private float jumpForce      = 10f;
    [SerializeField] private float doubleJumpForce = 8.5f;
    // Set to 3 in Inspector for classes that have triple jump
    [SerializeField] private int   maxJumps        = 2;
    // Freefall gravity multiplier — pick certain classes to freefall faster/slower
    [SerializeField] private float freefallGravityScale = 3f;
    [SerializeField] private float normalGravityScale   = 2f;

    [Header("Dash")]
    [SerializeField] private float dashSpeed       = 14f;
    [SerializeField] private float dashDuration    = 0.18f;
    [SerializeField] private float dashCooldown    = 0.4f;
    [SerializeField] private float doubleTapWindow = 0.25f; // seconds between taps to trigger dash

    [Header("Melee Attack")]
    [SerializeField] private float     attackRange    = 1.5f;
    [SerializeField] private float     attackWedge    = 80f;
    [SerializeField] private float     attackCooldown = 0.3f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Ranged Attack")]
    [SerializeField] private float rangedCooldown  = 0.5f;
    [SerializeField] private float rangedRange     = 8f;
    // TODO: assign a projectile prefab here for the ranged attack
    // [SerializeField] private GameObject projectilePrefab;

    private Rigidbody2D   rb;
    private BoxCollider2D col;
    private Animator      animator;

    // Movement state
    private bool  isGrounded;
    private bool  wasGrounded;
    private bool  isCrouching;
    private bool  isFacingRight = true;

    // Jump state
    private int   jumpsRemaining;

    // Dash state
    private bool  isDashing;
    private float dashTimer;
    private float dashCooldownTimer;
    private float lastTapTimeLeft;
    private float lastTapTimeRight;

    // Attack state
    private float attackTimer;
    private float rangedTimer;
    private float lastHorizontal;

    // 4 cardinal snap directions for melee
    private static readonly Vector2[] Cardinals     = { Vector2.right, Vector2.up, Vector2.left, Vector2.down };
    private static readonly string[]  CardinalNames = { "Right", "Up", "Left", "Down" };
    private int     lastCardinal = -1;
    private Vector2 mouseSnapPos;

    // ─────────────────────────────────────────────────────────────────────────
    void Start()
    {
        rb       = GetComponent<Rigidbody2D>();
        col      = GetComponent<BoxCollider2D>();
        animator = GetComponent<Animator>();

        jumpsRemaining      = maxJumps;
        rb.gravityScale     = normalGravityScale;
    }

    void Update()
    {
        attackTimer       -= Time.deltaTime;
        rangedTimer       -= Time.deltaTime;
        dashCooldownTimer -= Time.deltaTime;
        dashTimer         -= Time.deltaTime;

        wasGrounded = isGrounded;
        isGrounded  = CheckGrounded();

        HandleDash();
        HandleMovement();
        HandleJump();
        HandleCrouch();
        HandleFreefall();
        HandleMeleeAttack();
        HandleRangedAttack();
        HandleSpecialAbility1();
        HandleSpecialAbility2();
        UpdateAnimator();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GROUND CHECK
    bool CheckGrounded()
    {
        if (col == null) return false;

        Bounds  b      = col.bounds;
        Vector2 origin = new Vector2(b.center.x, b.min.y - 0.02f);

        RaycastHit2D hit = Physics2D.BoxCast(
            origin,
            new Vector2(b.size.x * 0.8f, 0.02f),
            0f,
            Vector2.down,
            0.15f
        );

        return hit.collider != null && hit.collider.gameObject != gameObject;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // MOVEMENT — A/D — class speed multiplier applied per GDD
    void HandleMovement()
    {
        if (isDashing) return;

        float h     = Input.GetAxisRaw("Horizontal");
        float speed = (isCrouching ? moveSpeed * crouchSpeedMultiplier : moveSpeed) * classSpeedMultiplier;
        rb.linearVelocity = new Vector2(h * speed, rb.linearVelocity.y);

        if (h != lastHorizontal)
        {
            if      (h >  0f) Debug.Log("[Move] Moving Right");
            else if (h <  0f) Debug.Log("[Move] Moving Left");
            else              Debug.Log("[Move] Stopped");
            lastHorizontal = h;
        }

        if      (h > 0f && !isFacingRight) Flip();
        else if (h < 0f &&  isFacingRight) Flip();
    }

    void Flip()
    {
        isFacingRight = !isFacingRight;
        transform.localScale = new Vector3(
            -transform.localScale.x,
             transform.localScale.y,
             transform.localScale.z);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DASH — double-tap A or D
    void HandleDash()
    {
        // Detect double-tap right (D)
        if (Input.GetKeyDown(KeyCode.D))
        {
            if (Time.time - lastTapTimeRight <= doubleTapWindow && dashCooldownTimer <= 0f)
                StartDash(1f);
            else
                lastTapTimeRight = Time.time;
        }

        // Detect double-tap left (A)
        if (Input.GetKeyDown(KeyCode.A))
        {
            if (Time.time - lastTapTimeLeft <= doubleTapWindow && dashCooldownTimer <= 0f)
                StartDash(-1f);
            else
                lastTapTimeLeft = Time.time;
        }

        // End dash when timer expires
        if (isDashing && dashTimer <= 0f)
            EndDash();
    }

    void StartDash(float direction)
    {
        isDashing         = true;
        dashTimer         = dashDuration;
        dashCooldownTimer = dashCooldown;
        rb.linearVelocity = new Vector2(dashSpeed * direction, 0f);
        animator?.SetBool("IsDashing", true);
        animator?.SetFloat("DashDirectionX", direction);
        Debug.Log($"[Move] Dash {(direction > 0 ? "Right" : "Left")}");
    }

    void EndDash()
    {
        isDashing = false;
        animator?.SetBool("IsDashing", false);
        Debug.Log("[Move] Dash End");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // JUMP — W or Spacebar
    // Double-tap Spacebar / W for double jump (or triple jump for certain classes)
    void HandleJump()
    {
        // Reset jumps on landing
        if (isGrounded && !wasGrounded)
        {
            jumpsRemaining = maxJumps;
            rb.gravityScale = normalGravityScale;
        }

        bool jumpPressed = Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.W);

        if (!jumpPressed || isCrouching) return;

        if (jumpsRemaining == maxJumps && isGrounded)
        {
            // Standard jump
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpsRemaining--;
            animator?.SetTrigger("Jump");
            Debug.Log("[Move] Jump");
        }
        else if (jumpsRemaining > 0 && !isGrounded)
        {
            // Double jump (or third jump for triple-jump classes)
            bool isLastJump = jumpsRemaining == 1;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, doubleJumpForce);
            jumpsRemaining--;
            animator?.SetTrigger(isLastJump && maxJumps == 2 ? "DoubleJump" : "DoubleJump");
            Debug.Log(jumpsRemaining == 0 ? "[Move] Final Jump" : "[Move] Double Jump");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CROUCH — hold S (smaller hitbox per GDD, also gates Special Ability #1)
    void HandleCrouch()
    {
        bool hold = (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) && isGrounded;

        if (hold && !isCrouching)
        {
            isCrouching = true;
            animator?.SetBool("IsCrouching", true);
            Debug.Log("[Move] Crouch Start");
        }
        else if (!hold && isCrouching)
        {
            isCrouching = false;
            animator?.SetBool("IsCrouching", false);
            Debug.Log("[Move] Crouch End");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // FREEFALL — per GDD certain classes freefall faster/slower via freefallGravityScale
    void HandleFreefall()
    {
        if (!isGrounded && rb.linearVelocity.y < 0f)
            rb.gravityScale = freefallGravityScale;
        else if (isGrounded)
            rb.gravityScale = normalGravityScale;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // MELEE ATTACK — Left Mouse Button
    void HandleMeleeAttack()
    {
        if (!Input.GetMouseButtonDown(0) || attackTimer > 0f) return;

        attackTimer = attackCooldown;
        int cardinal = GetSnappedCardinal(out Vector2 snapDir);
        lastCardinal = cardinal;
        PerformMelee(cardinal, snapDir);
    }

    int GetSnappedCardinal(out Vector2 snapDir)
    {
        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseSnapPos = mouseWorld;
        Vector2 dir  = (mouseWorld - (Vector2)transform.position).normalized;

        int best = 0; float bestDot = float.MinValue;
        for (int i = 0; i < Cardinals.Length; i++)
        {
            float d = Vector2.Dot(dir, Cardinals[i]);
            if (d > bestDot) { bestDot = d; best = i; }
        }
        snapDir = Cardinals[best];
        return best;
    }

    void PerformMelee(int cardinal, Vector2 attackDir)
    {
        Vector2      origin = (Vector2)transform.position + attackDir * (attackRange * 0.5f);
        Collider2D[] hits   = Physics2D.OverlapCircleAll(origin, attackRange * 0.5f, enemyLayer);

        int hitCount = 0;
        foreach (var hit in hits)
        {
            Vector2 toTarget = ((Vector2)hit.transform.position - (Vector2)transform.position).normalized;
            if (Vector2.Angle(attackDir, toTarget) <= attackWedge * 0.5f)
            {
                hit.GetComponent<EnemyDummy>()?.TakeDamage(10f, attackDir);
                Debug.Log($"[Melee] Hit '{hit.name}'");
                hitCount++;
            }
        }
        Debug.Log($"[Melee] {CardinalNames[cardinal]} — {hitCount} hit(s)");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // RANGED ATTACK — Right Mouse Button
    void HandleRangedAttack()
    {
        if (!Input.GetMouseButtonDown(1) || rangedTimer > 0f) return;

        rangedTimer = rangedCooldown;
        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 dir        = (mouseWorld - (Vector2)transform.position).normalized;

        // TODO: instantiate projectilePrefab and set its velocity/direction
        // Projectile placeholder — raycast to show intended direction
        RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, rangedRange, enemyLayer);
        if (hit.collider != null)
        {
            hit.collider.GetComponent<EnemyDummy>()?.TakeDamage(8f, dir);
            Debug.Log($"[Ranged] Hit '{hit.collider.name}'");
        }

        Debug.Log($"[Ranged] Fired toward {dir}");
        animator?.SetTrigger("RangedAttack");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SPECIAL ABILITY #1 — triggered from crouch context (E key while crouching)
    void HandleSpecialAbility1()
    {
        if (!Input.GetKeyDown(KeyCode.E) || !isCrouching) return;
        // TODO: implement character-specific Special Ability #1
        Debug.Log("[Ability] Special Ability #1");
        animator?.SetTrigger("Ability1");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SPECIAL ABILITY #2 — triggered from crouch, melee, or ranged context (Q key)
    void HandleSpecialAbility2()
    {
        if (!Input.GetKeyDown(KeyCode.Q)) return;
        // TODO: implement character-specific Special Ability #2
        // Can be called as a follow-up to melee or ranged per GDD diagram
        Debug.Log("[Ability] Special Ability #2");
        animator?.SetTrigger("Ability2");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ANIMATOR
    // Parameters:  Speed (Float), IsGrounded (Bool), VerticalVelocity (Float),
    //              Jump (Trigger), DoubleJump (Trigger),
    //              IsDashing (Bool), DashDirectionX (Float),
    //              IsCrouching (Bool), RangedAttack (Trigger),
    //              Ability1 (Trigger), Ability2 (Trigger)
    void UpdateAnimator()
    {
        if (animator == null) return;

        animator.SetFloat("Speed",           Mathf.Abs(rb.linearVelocity.x));
        animator.SetBool ("IsGrounded",      isGrounded);
        animator.SetFloat("VerticalVelocity", rb.linearVelocity.y);
    }

    // ─────────────────────────────────────────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        if (col != null)
        {
            Bounds  b      = col.bounds;
            Vector3 origin = new Vector3(b.center.x, b.min.y - 0.09f, 0f);
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireCube(origin, new Vector3(b.size.x * 0.8f, 0.15f, 0f));
        }

        Vector3 center = transform.position;

        // Melee range
        Gizmos.color = new Color(1f, 1f, 1f, 0.15f);
        DrawCircle(center, attackRange, 32);

        // Ranged range
        Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.1f);
        DrawCircle(center, rangedRange, 32);

        // Cardinal snap dots + active wedge
        for (int i = 0; i < Cardinals.Length; i++)
        {
            bool    active = (i == lastCardinal);
            Vector3 snap   = center + (Vector3)(Cardinals[i] * attackRange);
            Gizmos.color = active ? Color.red : Color.green;
            Gizmos.DrawSphere(snap, 0.08f);

            if (active)
            {
                Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.4f);
                float mid = Mathf.Atan2(Cardinals[i].y, Cardinals[i].x) * Mathf.Rad2Deg;
                DrawWedge(center, attackRange, mid - attackWedge * 0.5f, mid + attackWedge * 0.5f, 12);
            }
        }

        if (Application.isPlaying && lastCardinal >= 0)
        {
            Gizmos.color = new Color(1f, 0.5f, 0.6f);
            Gizmos.DrawSphere(mouseSnapPos, 0.06f);
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(mouseSnapPos, center + (Vector3)(Cardinals[lastCardinal] * attackRange));
        }
    }

    void DrawCircle(Vector3 center, float radius, int steps)
    {
        for (int i = 0; i < steps; i++)
        {
            float a1 = (i       / (float)steps) * 360f * Mathf.Deg2Rad;
            float a2 = ((i + 1) / (float)steps) * 360f * Mathf.Deg2Rad;
            Gizmos.DrawLine(
                center + new Vector3(Mathf.Cos(a1), Mathf.Sin(a1)) * radius,
                center + new Vector3(Mathf.Cos(a2), Mathf.Sin(a2)) * radius);
        }
    }

    void DrawWedge(Vector3 center, float radius, float fromDeg, float toDeg, int steps)
    {
        float fr = fromDeg * Mathf.Deg2Rad, tr = toDeg * Mathf.Deg2Rad;
        Gizmos.DrawLine(center, center + new Vector3(Mathf.Cos(fr), Mathf.Sin(fr)) * radius);
        Gizmos.DrawLine(center, center + new Vector3(Mathf.Cos(tr), Mathf.Sin(tr)) * radius);
        for (int i = 0; i < steps; i++)
        {
            float a1 = Mathf.Lerp(fr, tr, (float)i       / steps);
            float a2 = Mathf.Lerp(fr, tr, (float)(i + 1) / steps);
            Gizmos.DrawLine(
                center + new Vector3(Mathf.Cos(a1), Mathf.Sin(a1)) * radius,
                center + new Vector3(Mathf.Cos(a2), Mathf.Sin(a2)) * radius);
        }
    }
}
