// TEMPLATE SCRIPT — CoreGrabTemplate.cs — Do not attach directly. Use as a base reference for character-specific grab and throw scripts.
// NOTE: This script is intentionally non-functional. All logic is stubbed with TODOs.
// Attach alongside CoreMovementTemplate and CoreKnockbackTemplate on the same GameObject.

using UnityEngine;

/// <summary>
/// Handles grabbing, pummeling, throwing, and opponent escape for a single character.
/// Grabs ignore shields per GDD — TryGrab() bypasses CoreShieldTemplate entirely.
/// Exposes isHoldingOpponent publicly so other systems can suppress movement while grabbing.
/// </summary>
public class CoreGrabTemplate : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    // REFERENCES
    // ─────────────────────────────────────────────────────────────────────────

    [Header("References")]
    [SerializeField] private CoreMovementTemplate   movement;
    // Used to check isGrounded before allowing grab attempts (stub note below in TryGrab).

    [SerializeField] private CoreKnockbackTemplate  opponentKnockback;
    // Assigned dynamically when a grab lands — do not assign in Inspector.
    // Used to call TakeDamage() on pummel hits and throw release.

    [SerializeField] private CoreCharacterStats stats;
    // TODO: ATK stat can scale throwForce — higher ATK = further throws.
    // Example: float scaledForce = throwForce * Mathf.Lerp(0.8f, 1.4f, (stats.ATK - 1f) / 9f)

    // ─────────────────────────────────────────────────────────────────────────
    // GRAB
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Grab")]
    [SerializeField] private float     grabRange  = 1.2f;
    [SerializeField] private LayerMask enemyLayer;

    public  bool       isHoldingOpponent = false;
    public  GameObject heldTarget        = null;
    // heldTarget is set on a successful grab and cleared by ReleaseTarget().

    // ─────────────────────────────────────────────────────────────────────────
    // PUMMEL
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Pummel")]
    [SerializeField] private float pummelDamage   = 2f;   // damage per pummel hit
    [SerializeField] private float pummelCooldown = 0.4f; // seconds between pummel hits

    private float pummelTimer;

    // ─────────────────────────────────────────────────────────────────────────
    // THROW
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Throw")]
    [SerializeField] private float throwForce = 12f;
    // Applied to the target's Rigidbody2D on release.
    // TODO: scale by stats.ATK using the formula noted in the References header.

    // ─────────────────────────────────────────────────────────────────────────
    // ESCAPE
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Escape")]
    [SerializeField] private float grabDuration = 2.5f;
    // Max seconds the opponent can be held before auto-release.
    // TODO: escape difficulty should decrease as grabTimer approaches 0
    //       (i.e. easier to escape early in the grab than near the end).

    private float grabTimer;

    // ─────────────────────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Cache references and reset state.</summary>
    void Start()
    {
        // TODO: validate movement and stats references are assigned
    }

    /// <summary>Tick timers and poll input each frame.</summary>
    void Update()
    {
        pummelTimer -= Time.deltaTime;

        HandleGrabInput();

        if (isHoldingOpponent)
        {
            HandlePummel();
            HandleThrowInput();
            HandleGrabTimer();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GRAB INPUT
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Reads grab button input and attempts a grab if conditions are met.</summary>
    void HandleGrabInput()
    {
        // TODO: read grab button input (e.g. Input.GetButtonDown("Grab"))
        // TODO: if pressed && !isHoldingOpponent → TryGrab()
        // NOTE: per GDD, grabs are ground-only by default.
        //       To allow air grabs, remove the movement.isGrounded check in TryGrab().
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GRAB
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Attempts to grab the nearest opponent within grabRange.
    /// Grabs ignore shields per GDD — CoreShieldTemplate is bypassed entirely.
    /// Fails silently if already holding a target.
    /// Ground-only by default; see stub note for air grab option.
    /// </summary>
    void TryGrab()
    {
        if (isHoldingOpponent) return;

        // NOTE: remove or comment out the line below to enable air grabs.
        // TODO: if (!movement.isGrounded) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, grabRange, enemyLayer);

        foreach (var hit in hits)
        {
            // TODO: skip if hit.gameObject == this.gameObject
            // TODO: heldTarget = hit.gameObject
            // TODO: isHoldingOpponent = true
            // TODO: grabTimer = grabDuration
            // TODO: opponentKnockback = heldTarget.GetComponent<CoreKnockbackTemplate>()
            // TODO: freeze heldTarget's Rigidbody2D (set to Kinematic)
            // TODO: parent heldTarget to this transform so it follows the grabber
            // TODO: animator.SetBool("IsGrabbing", true)
            // TODO: notify heldTarget it has been grabbed (e.g. call a OnGrabbed() method)
            Debug.Log($"[Grab] Grabbed {hit.name}");
            return; // grab only one target
        }

        Debug.Log("[Grab] Grab missed");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PUMMEL
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called each frame while holding an opponent.
    /// Each pummel hit deals pummelDamage and respects pummelCooldown to prevent spam.
    /// </summary>
    void HandlePummel()
    {
        // TODO: if pummelTimer > 0 return
        // TODO: read pummel input (e.g. attack button while holding)
        // TODO: if pressed:
        //   opponentKnockback.TakeDamage(pummelDamage, Vector2.zero, 0f, 0f)
        //   pummelTimer = pummelCooldown
        //   animator.SetTrigger("Pummel")
        //   Debug.Log("[Grab] Pummel hit")
    }

    // ─────────────────────────────────────────────────────────────────────────
    // THROW INPUT
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Reads directional input while holding and routes to the correct throw.</summary>
    void HandleThrowInput()
    {
        // TODO: read directional input
        // TODO: if forward/back input → ThrowForward() or ThrowBack() based on facing
        // TODO: if up input           → ThrowUp()
        // TODO: if down input         → ThrowDown()
        // TODO: neutral throw option: ThrowForward() as default
    }

    // ─────────────────────────────────────────────────────────────────────────
    // THROWS
    // Each throw applies a unique force vector then calls ReleaseTarget().
    // TODO: scale throwForce by stats.ATK across all throws.
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Horizontal launch in the direction the character is facing.
    /// Trajectory: flat arc, good for edgeguarding.
    /// </summary>
    void ThrowForward()
    {
        // TODO: Vector2 dir = isFacingRight ? Vector2.right : Vector2.left
        // TODO: apply throwForce * dir to heldTarget's Rigidbody2D
        // TODO: opponentKnockback.TakeDamage(throwForce, dir, baseKnockback, scaling)
        Debug.Log("[Grab] Throw Forward");
        ReleaseTarget();
    }

    /// <summary>
    /// Reverse horizontal launch, opposite to the facing direction.
    /// Trajectory: flat arc behind the grabber, useful for stage positioning.
    /// </summary>
    void ThrowBack()
    {
        // TODO: Vector2 dir = isFacingRight ? Vector2.left : Vector2.right
        // TODO: apply throwForce * dir to heldTarget's Rigidbody2D
        // TODO: opponentKnockback.TakeDamage(throwForce, dir, baseKnockback, scaling)
        Debug.Log("[Grab] Throw Back");
        ReleaseTarget();
    }

    /// <summary>
    /// Vertical upward launch.
    /// Trajectory: steep upward arc, good for combo setups and juggling.
    /// </summary>
    void ThrowUp()
    {
        // TODO: Vector2 dir = Vector2.up
        // TODO: apply throwForce * dir to heldTarget's Rigidbody2D
        // TODO: opponentKnockback.TakeDamage(throwForce, dir, baseKnockback, scaling)
        Debug.Log("[Grab] Throw Up");
        ReleaseTarget();
    }

    /// <summary>
    /// Downward spike launch.
    /// Trajectory: sharp downward angle — most effective near ledges or off-stage.
    /// </summary>
    void ThrowDown()
    {
        // TODO: Vector2 dir = Vector2.down
        // TODO: apply throwForce * dir to heldTarget's Rigidbody2D
        // TODO: opponentKnockback.TakeDamage(throwForce, dir, baseKnockback, scaling)
        Debug.Log("[Grab] Throw Down");
        ReleaseTarget();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // RELEASE
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Clears the held target, restores its Rigidbody2D, and resets grab state.
    /// Called by every throw and by auto-release when grabTimer expires.
    /// </summary>
    void ReleaseTarget()
    {
        if (heldTarget == null) return;

        // TODO: unparent heldTarget from this transform
        // TODO: restore heldTarget's Rigidbody2D to Dynamic
        // TODO: notify heldTarget it has been released (e.g. call OnReleased())
        // TODO: animator.SetBool("IsGrabbing", false)

        Debug.Log($"[Grab] Released {heldTarget.name}");

        heldTarget        = null;
        opponentKnockback = null;
        isHoldingOpponent = false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GRAB TIMER / AUTO RELEASE
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Ticks down grabTimer each frame. Auto-releases when it reaches zero.
    /// Escape difficulty should decrease as grabTimer approaches 0 —
    /// implement by reducing the mash threshold in OnEscapeAttempt() proportionally.
    /// </summary>
    void HandleGrabTimer()
    {
        grabTimer -= Time.deltaTime;

        if (grabTimer <= 0f)
        {
            Debug.Log("[Grab] Grab expired — auto release");
            ReleaseTarget();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ESCAPE
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by the held opponent's script when they mash to escape.
    /// Escape succeeds more easily as grabTimer decreases (longer they've been held).
    /// TODO: implement mash counter threshold that scales with grabTimer remaining.
    /// </summary>
    public void OnEscapeAttempt()
    {
        // TODO: float escapeThreshold = Mathf.Lerp(1f, 10f, grabTimer / grabDuration)
        //       (low grabTimer = low threshold = easy to escape)
        // TODO: increment a mash counter on the held opponent
        // TODO: if mash counter >= escapeThreshold → ReleaseTarget()
        Debug.Log("[Grab] Escape attempted");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GIZMOS
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Draws grab range and held target indicator in the Scene view.</summary>
    void OnDrawGizmosSelected()
    {
        // Grab detection range
        Gizmos.color = isHoldingOpponent
            ? new Color(1f, 0.3f, 0.3f, 0.4f)   // red while holding
            : new Color(0.3f, 1f, 0.3f, 0.25f);  // green while idle
        Gizmos.DrawWireSphere(transform.position, grabRange);

        // Line to held target
        if (isHoldingOpponent && heldTarget != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, heldTarget.transform.position);
        }

        // TODO: draw throw trajectory previews for each direction when isHoldingOpponent
    }
}
