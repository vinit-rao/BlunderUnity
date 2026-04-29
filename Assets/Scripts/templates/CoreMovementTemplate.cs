// TEMPLATE SCRIPT — CoreMovementTemplate.cs — Do not attach directly. Use as a base reference for character-specific movement scripts.
// NOTE: This script is intentionally non-functional. All movement logic is stubbed with TODOs.
// To create a character: copy this file, rename the class, fill in each TODO, and set the 6 stat values in the Inspector.

#pragma warning disable CS0414
using UnityEngine;

/// <summary>
/// Base movement template for all BlunderUnity roster characters.
/// Copy this file, rename the class, and implement each TODO section.
/// Stat fields drive all runtime movement values via ApplyStats().
/// </summary>
public class CoreMovementTemplate : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    // CHARACTER STATS
    // Every character is balanced to 38–60 points across 6 stats (each 1–10).
    // ApplyStats() maps these onto the movement parameters below at Start.
    //
    // Example roster stat blocks:
    //   Harry   — ATK 9  DEF 8  SPD 2  JUMP 2  RECOVERY 8  WEIGHT 9
    //   Brandon — ATK 6  DEF 5  SPD 9  JUMP 7  RECOVERY 6  WEIGHT 5
    //   ELR     — ATK 9  DEF 3  SPD 8  JUMP 8  RECOVERY 7  WEIGHT 3
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Character Stats")]
    [SerializeField] private float statATK      = 5f; // 1–10 — damage output (not used in movement)
    [SerializeField] private float statDEF      = 5f; // 1–10 — damage received modifier (not used in movement)
    [SerializeField] private float statSPD      = 5f; // 1–10 — drives moveSpeed and runSpeed
    [SerializeField] private float statJUMP     = 5f; // 1–10 — drives jumpForce and max jump height
    [SerializeField] private float statRECOVERY = 5f; // 1–10 — drives double-jump strength and fast-fall modifier
    [SerializeField] private float statWEIGHT   = 5f; // 1–10 — drives gravityScale and knockback received multiplier

    // ─────────────────────────────────────────────────────────────────────────
    // MOVEMENT
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Movement")]
    [SerializeField] private float moveSpeed       = 5f;   // set by statSPD in ApplyStats()
    [SerializeField] private float runSpeed        = 9f;   // set by statSPD in ApplyStats()
    [SerializeField] private float runHoldTime     = 0.2f; // seconds of held input before run activates
    [SerializeField] private float crouchSpeedMult = 0.4f; // fraction of moveSpeed while crouching

    // ─────────────────────────────────────────────────────────────────────────
    // JUMP
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Jump")]
    [SerializeField] private float jumpForce       = 12f;  // set by statJUMP in ApplyStats()
    [SerializeField] private float doubleJumpForce = 10f;  // set by statRECOVERY in ApplyStats()
    [SerializeField] private float fastFallForce   = 18f;  // downward impulse — set by statRECOVERY
    [SerializeField] private float gravityScale    = 3f;   // set by statWEIGHT in ApplyStats()

    // ─────────────────────────────────────────────────────────────────────────
    // DASH
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Dash")]
    [SerializeField] private float dashSpeed       = 18f;
    [SerializeField] private float dashDuration    = 0.15f; // seconds the dash lasts
    [SerializeField] private float dashCooldown    = 0.4f;
    [SerializeField] private float doubleTapWindow = 0.2f;  // seconds between taps to trigger dash

    // ─────────────────────────────────────────────────────────────────────────
    // GROUND CHECK
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Ground Check")]
    [SerializeField] private float groundCheckDepth  = 0.15f; // BoxCast distance downward
    [SerializeField] private float groundCheckWidthPct = 0.8f; // fraction of collider width to cast

    // ─────────────────────────────────────────────────────────────────────────
    // PRIVATE STATE
    // ─────────────────────────────────────────────────────────────────────────

    private Rigidbody2D   rb;
    private BoxCollider2D col;
    private Animator      animator;
    // Animator Controller parameters (create these in the Animator window):
    //   Speed            (Float)   — 0 = Idle, >0 = Running
    //   IsGrounded       (Bool)    — ground/air state transitions
    //   VerticalVelocity (Float)   — negative = Freefall blend
    //   Jump             (Trigger) — standard jump
    //   DoubleJump       (Trigger) — double jump
    //   IsDashing        (Bool)    — directional dash active
    //   DashDirectionX   (Float)   — 1 = right, -1 = left

    // Movement state — public so CoreAttackTemplate, CoreGrabTemplate, CoreStockTemplate can read them
    public bool  isFacingRight   = true;
    public bool  isGrounded;
    public bool  isCrouching;
    public bool  isRunning;
    private float horizontalInput;
    private float runHoldTimer;

    // Jump state
    private bool hasDoubleJump;
    private bool hasFastFalled;

    // Dash state
    public  bool  isDashing;
    private float dashTimer;
    private float dashCooldownTimer;
    private float lastTapTimeLeft;   // timestamp of last left tap
    private float lastTapTimeRight;  // timestamp of last right tap

    // ─────────────────────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Cache components and apply stat-driven values.</summary>
    void Start()
    {
        rb       = GetComponent<Rigidbody2D>();
        col      = GetComponent<BoxCollider2D>();
        animator = GetComponent<Animator>();
        ApplyStats();
    }

    /// <summary>Run all per-frame movement handlers in priority order.</summary>
    void Update()
    {
        dashCooldownTimer -= Time.deltaTime;
        dashTimer         -= Time.deltaTime;

        isGrounded    = CheckGrounded();
        horizontalInput = Input.GetAxisRaw("Horizontal");

        HandleFlip();
        HandleWalkRun();
        HandleDash();
        HandleCrouch();
        HandleJump();
        HandleFastFall();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // STAT APPLICATION
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Maps the six character stats onto runtime movement parameters.
    /// Called once at Start. To tweak feel, adjust the multipliers here —
    /// do not edit the serialized movement fields directly.
    /// </summary>
    void ApplyStats()
    {
        // ── ATK ──────────────────────────────────────────────────────────────
        // Not used in movement.
        // TODO: pass statATK to a CombatController or AttackData component.

        // ── DEF ──────────────────────────────────────────────────────────────
        // Not used in movement.
        // TODO: pass statDEF to a HealthController or DamageReceiver component.

        // ── SPD → walk speed, run speed ───────────────────────────────────────
        // stat 1 = 0.8  |  stat 5 = 4.0  |  stat 10 = 8.0
        moveSpeed = statSPD * 0.8f;
        // stat 1 = 1.4  |  stat 5 = 7.0  |  stat 10 = 14.0
        runSpeed  = statSPD * 1.4f;

        // ── JUMP → jump force ────────────────────────────────────────────────
        // stat 1 = 3.0  |  stat 5 = 15.0  |  stat 10 = 30.0
        jumpForce = statJUMP * 3.0f;

        // ── RECOVERY → double-jump force, fast-fall speed ────────────────────
        // stat 1 = 2.0  |  stat 5 = 10.0  |  stat 10 = 20.0
        doubleJumpForce = statRECOVERY * 2.0f;
        // stat 1 = 3.0  |  stat 5 = 15.0  |  stat 10 = 30.0  (higher = snappier fall)
        fastFallForce   = statRECOVERY * 3.0f;

        // ── WEIGHT → gravity scale ───────────────────────────────────────────
        // stat 1 = 1.5 (floaty)  |  stat 5 = 3.25  |  stat 10 = 5.0 (heavy)
        gravityScale = Mathf.Lerp(1.5f, 5.0f, (statWEIGHT - 1f) / 9f);
        if (rb != null) rb.gravityScale = gravityScale;
        // TODO: pass statWEIGHT knockback multiplier to a KnockbackReceiver component.
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GROUND CHECK
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// BoxCasts just below the collider's world-space bottom edge.
    /// Uses col.bounds so player scale never breaks the calculation.
    /// Returns true if any collider other than the player is directly below.
    /// </summary>
    bool CheckGrounded()
    {
        if (col == null) return false;

        Bounds  b      = col.bounds;
        Vector2 origin = new Vector2(b.center.x, b.min.y - 0.02f);

        RaycastHit2D hit = Physics2D.BoxCast(
            origin,
            new Vector2(b.size.x * groundCheckWidthPct, 0.02f),
            0f,
            Vector2.down,
            groundCheckDepth
        );

        return hit.collider != null && hit.collider.gameObject != gameObject;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // WALK / RUN
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Applies horizontal movement. Holding a direction longer than
    /// runHoldTime transitions from walk to run speed.
    /// Crouching overrides speed with crouchSpeedMult.
    /// Dashing skips this handler entirely.
    /// </summary>
    void HandleWalkRun()
    {
        if (isDashing) return;

        // TODO: increment runHoldTimer while input is held, reset on direction change
        // TODO: set isRunning = runHoldTimer >= runHoldTime
        // TODO: choose speed = isCrouching ? moveSpeed * crouchSpeedMult
        //                    : isRunning    ? runSpeed
        //                    :                moveSpeed
        // TODO: rb.linearVelocity = new Vector2(horizontalInput * speed, rb.linearVelocity.y)
        // TODO: animator.SetFloat("Speed", Mathf.Abs(horizontalInput))   → 0 = Idle, >0 = Running
        // TODO: animator.SetBool("IsGrounded", isGrounded)               → ground/air transitions
        // TODO: animator.SetFloat("VerticalVelocity", rb.linearVelocity.y) → negative drives Freefall state
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DASH
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Detects double-tap on a horizontal direction to trigger a dash.
    /// During a dash the character is immune to normal movement input.
    /// Can be cancelled into a dash attack (hook into AttackSystem).
    /// </summary>
    void HandleDash()
    {
        // TODO: on each GetButtonDown("Horizontal"):
        //   - record timestamp in lastTapTimeLeft / lastTapTimeRight
        //   - if second tap within doubleTapWindow && dashCooldownTimer <= 0 → StartDash()
        // TODO: if isDashing && dashTimer <= 0 → EndDash()
        // TODO: if isDashing && attack input → cancel into dash attack via AttackSystem
    }

    /// <summary>Launches the dash: sets velocity, timers, and animator trigger.</summary>
    void StartDash()
    {
        // TODO: isDashing = true
        // TODO: dashTimer = dashDuration
        // TODO: dashCooldownTimer = dashCooldown
        // TODO: rb.linearVelocity = new Vector2(dashSpeed * (isFacingRight ? 1f : -1f), 0f)
        // TODO: animator.SetBool("IsDashing", true)                              → enters Directional Dash state
        // TODO: animator.SetFloat("DashDirectionX", isFacingRight ? 1f : -1f)   → 1 = right, -1 = left
        Debug.Log("[Move] Dash Start");
    }

    /// <summary>Ends the dash and restores normal movement control.</summary>
    void EndDash()
    {
        // TODO: isDashing = false
        // TODO: animator.SetBool("IsDashing", false)   → exits Directional Dash state, returns to Idle/Running
        Debug.Log("[Move] Dash End");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CROUCH
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Hold S / Down Arrow while grounded to crouch.
    /// Reduces movement speed via crouchSpeedMult (see HandleWalkRun).
    /// Does NOT resize the BoxCollider2D — hitbox reduction is handled
    /// by toggling a separate child trigger collider tagged "HurtboxCrouch".
    /// </summary>
    void HandleCrouch()
    {
        // TODO: bool want = (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) && isGrounded
        // TODO: if want && !isCrouching → enter crouch, enable HurtboxCrouch child, log, set animator
        // TODO: if !want && isCrouching → exit crouch, disable HurtboxCrouch child, log, set animator
    }

    // ─────────────────────────────────────────────────────────────────────────
    // JUMP + DOUBLE JUMP
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Standard jump on Space while grounded.
    /// Double jump consumed on second press while airborne.
    /// hasDoubleJump resets on landing (CheckGrounded returning true).
    /// </summary>
    void HandleJump()
    {
        // TODO: on land (isGrounded && !wasGrounded) → hasDoubleJump = true, hasFastFalled = false
        // TODO: GetButtonDown("Jump") && isGrounded && !isCrouching → standard jump
        //   rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce)
        //   animator.SetTrigger("Jump")         → fires Jump state
        // TODO: GetButtonDown("Jump") && !isGrounded && hasDoubleJump → double jump
        //   hasDoubleJump = false
        //   rb.linearVelocity = new Vector2(rb.linearVelocity.x, doubleJumpForce)
        //   animator.SetTrigger("DoubleJump")   → fires Double Jump state
        // NOTE: Freefall is parameter-driven — no trigger needed.
        //   animator.SetFloat("VerticalVelocity", rb.linearVelocity.y) set each frame in HandleWalkRun
        //   animator.SetBool("IsGrounded", isGrounded)                 set each frame in HandleWalkRun
        //   Animator transition: IsGrounded=false && VerticalVelocity < -0.1 → Freefall state
        // TODO: Debug.Log on each state transition
    }

    // ─────────────────────────────────────────────────────────────────────────
    // FAST FALL
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Pressing Down while airborne and moving downward triggers a fast fall.
    /// Applies a one-time downward impulse scaled by statRECOVERY (lower = weaker).
    /// Can only trigger once per airborne period.
    /// </summary>
    void HandleFastFall()
    {
        // TODO: if !isGrounded && !hasFastFalled && rb.linearVelocity.y < 0
        //          && (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        //   → hasFastFalled = true
        //   → rb.linearVelocity = new Vector2(rb.linearVelocity.x, -fastFallForce)
        //   → animator.SetTrigger("FastFall")
        //   → Debug.Log("[Move] Fast Fall")
    }

    // ─────────────────────────────────────────────────────────────────────────
    // FLIP
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Flips the sprite to face the current movement direction.
    /// Uses transform.localScale X inversion; no separate SpriteRenderer flip
    /// so child objects (hitboxes, VFX) mirror correctly.
    /// </summary>
    void HandleFlip()
    {
        // TODO: if horizontalInput > 0 && !isFacingRight → Flip()
        // TODO: if horizontalInput < 0 &&  isFacingRight → Flip()
    }

    /// <summary>Inverts localScale.x and toggles isFacingRight.</summary>
    void Flip()
    {
        isFacingRight = !isFacingRight;
        transform.localScale = new Vector3(
            -transform.localScale.x,
             transform.localScale.y,
             transform.localScale.z);
        Debug.Log($"[Move] Facing {(isFacingRight ? "Right" : "Left")}");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GIZMOS
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Draws debug shapes in the Scene view when the GameObject is selected.
    /// Ground check box: green = grounded, red = airborne.
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (col == null) col = GetComponent<BoxCollider2D>();
        if (col == null) return;

        // Ground check box
        Bounds  b      = col.bounds;
        Vector3 origin = new Vector3(b.center.x, b.min.y - 0.09f, 0f);
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireCube(origin, new Vector3(b.size.x * groundCheckWidthPct, groundCheckDepth, 0f));

        // TODO: draw dash direction indicator while isDashing
        // TODO: draw double-jump availability indicator (e.g. cyan dot above head)
        // TODO: draw crouch hurtbox outline when isCrouching
    }
}
