using UnityEngine;

public class coreMovement1 : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed             = 5f;
    [SerializeField] private float crouchSpeedMultiplier = 0.5f;
    [SerializeField] private float classSpeedMultiplier  = 1f;   // set per character class

    [Header("Jump")]
    [SerializeField] private float jumpForce             = 10f;
    [SerializeField] private float doubleJumpForce       = 8.5f;
    [SerializeField] private int   maxJumps              = 2;    // set to 3 for triple-jump classes
    [SerializeField] private float freefallGravityScale  = 3f;   // faster/slower per class
    [SerializeField] private float normalGravityScale    = 2f;
    [SerializeField] private float doubleJumpDelay = 0.15f; // how much delay you want after initial jump

    [Header("Dash")]
    [SerializeField] private float dashSpeed       = 14f;
    [SerializeField] private float dashDuration    = 0.18f;
    [SerializeField] private float dashCooldown    = 0.4f;

    [Header("Melee Attack")]
    [SerializeField] private float     attackRange    = 1.5f;
    [SerializeField] private float     attackWedge    = 80f;
    [SerializeField] private float     attackCooldown = 0.3f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Ranged Attack")]
    [SerializeField] private float      rangedCooldown   = 0.5f;
    [SerializeField] private GameObject projectilePrefab; // assign Projectile prefab in Inspector

    // ─────────────────────────────────────────────────────────────────────────
    private Rigidbody2D   rb;
    private BoxCollider2D col;
    private Animator      animator;

    // Movement
    private bool  isGrounded;
    private bool  wasGrounded;
    private bool  isCrouching;
    private bool  isFacingRight = true;
    private float lastHorizontal;

    // Jump
    private int  jumpsRemaining;
    private bool wasFalling;
    private float lastJumpTime;

    // Dash
    private bool  isDashing;
    private float dashTimer;
    private float dashCooldownTimer;

    // Attack
    private float attackTimer;
    private float rangedTimer;

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

        jumpsRemaining  = maxJumps;
        rb.gravityScale = normalGravityScale;
    }

    void Update()
    {
        attackTimer       -= Time.deltaTime;
        rangedTimer       -= Time.deltaTime;
        dashCooldownTimer -= Time.deltaTime;
        dashTimer         -= Time.deltaTime;

        wasGrounded = isGrounded;
        isGrounded  = CheckGrounded();

        // check to prevent instant double jumping if you walk off a ledge
        if (wasGrounded && !isGrounded && rb.linearVelocity.y < 0f)
        {
            lastJumpTime = Time.time;
        }

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
    // GROUND CHECK — uses col.bounds (world space) so scale never matters
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
    // MOVEMENT — A/D, class speed multiplier applied per GDD
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
    // DASH — press lshift or rshift while moving
    void HandleDash()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
        {
            if (dashCooldownTimer <= 0f)
            {
                float h = Input.GetAxisRaw("Horizontal");
                if (h != 0f)
                {
                    StartDash(Mathf.Sign(h));
                }
            }
        }
        if (isDashing && dashTimer <= 0f)
            EndDash();
    }

    void StartDash(float direction)
    {
        isDashing         = true;
        dashTimer         = dashDuration;
        dashCooldownTimer = dashCooldown;
        rb.linearVelocity = new Vector2(dashSpeed * direction, 0f);
        SetAnimBool ("IsDashing",      true);
        SetAnimFloat("DashDirectionX", direction);
        Debug.Log($"[Move] Dash {(direction > 0f ? "Right" : "Left")}");
    }

    void EndDash()
    {
        isDashing = false;
        SetAnimBool("IsDashing", false);
        Debug.Log("[Move] Dash End");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // JUMP — W or Spacebar; double/triple jump via maxJumps
    void HandleJump()
    {
        if (isGrounded && !wasGrounded)
        {
            jumpsRemaining  = maxJumps;
            rb.gravityScale = normalGravityScale;
            Debug.Log("[Move] Landed");
        }

        bool jumpPressed = Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.W);
        if (!jumpPressed || isCrouching) return;

        if (jumpsRemaining == maxJumps && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpsRemaining--;
            lastJumpTime = Time.time;
            SetAnimTrigger("Jump");
            Debug.Log("[Move] Jump");
        }
        else if (jumpsRemaining > 0 && !isGrounded)
        {
            if (Time.time - lastJumpTime >= doubleJumpDelay)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, doubleJumpForce);
                jumpsRemaining--;
                lastJumpTime = Time.time;
                SetAnimTrigger("DoubleJump");
                Debug.Log(jumpsRemaining == 0 ? "[Move] Final Jump" : "[Move] Double Jump");
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CROUCH — hold S; smaller hitbox per GDD, gates Special Ability #1
    void HandleCrouch()
    {
        bool hold = (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) && isGrounded;

        if (hold && !isCrouching)
        {
            isCrouching = true;
            SetAnimBool("IsCrouching", true);
            Debug.Log("[Move] Crouch Start");
        }
        else if (!hold && isCrouching)
        {
            isCrouching = false;
            SetAnimBool("IsCrouching", false);
            Debug.Log("[Move] Crouch End");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // FREEFALL — gravity scale switches when falling; per-class via Inspector
    void HandleFreefall()
    {
        bool falling = !isGrounded && rb.linearVelocity.y < 0f;

        if (falling && !wasFalling) Debug.Log("[Move] Freefall");
        wasFalling = falling;

        rb.gravityScale = falling ? freefallGravityScale : normalGravityScale;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // MELEE ATTACK — Left Mouse Button, snaps to nearest cardinal direction
    void HandleMeleeAttack()
    {
        if (!Input.GetMouseButtonDown(0) || attackTimer > 0f) return;

        attackTimer  = attackCooldown;
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
    // RANGED ATTACK — Right Mouse Button, spawns Projectile prefab
    void HandleRangedAttack()
    {
        if (!Input.GetMouseButtonDown(1) || rangedTimer > 0f) return;

        rangedTimer = rangedCooldown;

        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 dir        = (mouseWorld - (Vector2)transform.position).normalized;

        if (projectilePrefab != null)
        {
            Vector3 spawnPos = transform.position + (Vector3)(dir * 1f);
            GameObject p = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            p.GetComponent<Projectile>()?.Init(dir, enemyLayer);
            p.transform.right = dir;
            Debug.Log($"[Ranged] Fired — direction {dir}");
        }
        else
        {
            Debug.LogWarning("[Ranged] No projectile prefab assigned in Inspector.");
        }

        SetAnimTrigger("RangedAttack");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SPECIAL ABILITY #1 — E while crouching
    void HandleSpecialAbility1()
    {
        if (!Input.GetKeyDown(KeyCode.E) || !isCrouching) return;
        // TODO: implement character-specific Special Ability #1
        Debug.Log("[Ability] Special Ability #1 — triggered while crouching");
        SetAnimTrigger("Ability1");
    }

    // SPECIAL ABILITY #2 — Q key (available from any context per GDD)
    void HandleSpecialAbility2()
    {
        if (!Input.GetKeyDown(KeyCode.Q)) return;
        // TODO: implement character-specific Special Ability #2
        Debug.Log("[Ability] Special Ability #2");
        SetAnimTrigger("Ability2");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ANIMATOR — safe helpers skip missing parameters silently
    // Required parameters: Speed (Float), IsGrounded (Bool), VerticalVelocity (Float),
    //   Jump (Trigger), DoubleJump (Trigger), IsDashing (Bool), DashDirectionX (Float),
    //   IsCrouching (Bool), RangedAttack (Trigger), Ability1 (Trigger), Ability2 (Trigger)
    void UpdateAnimator()
    {
        if (animator == null) return;
        SetAnimFloat("Speed",            Mathf.Abs(rb.linearVelocity.x));
        SetAnimBool ("IsGrounded",       isGrounded);
        SetAnimFloat("VerticalVelocity", rb.linearVelocity.y);
    }

    void SetAnimFloat(string param, float value)
    {
        foreach (var p in animator.parameters)
            if (p.name == param) { animator.SetFloat(param, value); return; }
    }

    void SetAnimBool(string param, bool value)
    {
        foreach (var p in animator.parameters)
            if (p.name == param) { animator.SetBool(param, value); return; }
    }

    void SetAnimTrigger(string param)
    {
        foreach (var p in animator.parameters)
            if (p.name == param) { animator.SetTrigger(param); return; }
    }

    // ─────────────────────────────────────────────────────────────────────────
    void OnDrawGizmos()
    {
        if (!Application.isPlaying || !HitboxDebugger.showHitboxes) return;
        if (lastCardinal < 0 || attackTimer <= 0f) return;

        Vector3 center = transform.position;
        float   mid    = Mathf.Atan2(Cardinals[lastCardinal].y, Cardinals[lastCardinal].x) * Mathf.Rad2Deg;

        // Flash orange while in cooldown window
        float alpha = Mathf.Clamp01(attackTimer / attackCooldown);
        Gizmos.color = new Color(1f, 0.4f, 0f, alpha * 0.7f);
        DrawWedge(center, attackRange, mid - attackWedge * 0.5f, mid + attackWedge * 0.5f, 16);

        Gizmos.color = new Color(1f, 0.4f, 0f, alpha);
        DrawCircle(center, attackRange, 32);
    }

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
