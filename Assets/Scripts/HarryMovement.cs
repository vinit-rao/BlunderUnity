// Harry (Mechacommando) — Self-contained movement and abilities script.
// Do NOT attach coreMovement1 alongside this. This script is fully independent.
//
// Stats (ATK 9 | DEF 8 | SPD 2 | JUMP 2 | RECOVERY 8 | WEIGHT 9)
//
// Controls:
//   A/D          — Move
//   W / Space    — Jump (double jump)
//   S            — Crouch + Scroll Session passive (cooldowns x1.5)
//   AA / DD      — Dash
//   Left Click   — M1: Tinger Brochure (short range melee slash)
//   Right Click  — M2: Gundam Throw (slow, high-damage projectile)
//   Q            — Dumgun Jetpack (5s flight: 2s ascent + 3s hover, launch blast)
//   E            — Dumgun Blast (explosive AOE, costs 3 fuel, slight delay)
//   R            — Infinity Charge (2s channel + invincible, then high-speed dash)

using System.Collections;
using UnityEngine;

public class HarryMovement : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    // REFERENCES
    // ─────────────────────────────────────────────────────────────────────────

    [Header("References")]
    [SerializeField] private LayerMask     enemyLayer;
    [SerializeField] private GameObject    gundamProjectilePrefab; // M2 — assign slow heavy projectile prefab

    private Rigidbody2D    rb;
    private BoxCollider2D  col;
    private Animator       animator;
    private SpriteRenderer sr;
    private Color          originalColor;

    // Visual cue colors per ability
    private static readonly Color ColorMelee   = new Color(1f, 1f, 1f, 1f);        // white flash
    private static readonly Color ColorGundam  = new Color(0.3f, 0.6f, 1f, 1f);    // blue
    private static readonly Color ColorJetpack = new Color(1f, 0.55f, 0f, 1f);     // orange
    private static readonly Color ColorHover   = new Color(1f, 0.85f, 0.2f, 1f);   // yellow
    private static readonly Color ColorDumgun  = new Color(1f, 0.15f, 0.15f, 1f);  // red charge
    private static readonly Color ColorCharge  = new Color(0.7f, 0.1f, 1f, 1f);    // purple channel
    private static readonly Color ColorLaunch  = new Color(1f, 1f, 1f, 1f);        // white burst
    private static readonly Color ColorScroll  = new Color(0.4f, 1f, 0.4f, 1f);    // green passive

    // ─────────────────────────────────────────────────────────────────────────
    // MOVEMENT  (SPD 2 — slow but deliberate)
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Movement")]
    [SerializeField] private float moveSpeed             = 1.6f;  // SPD 2 × 0.8
    [SerializeField] private float crouchSpeedMultiplier = 0.4f;

    [Header("Jump")]
    [SerializeField] private float jumpForce            = 6f;    // JUMP 2 × 3.0
    [SerializeField] private float doubleJumpForce      = 16f;   // RECOVERY 8 × 2.0
    [SerializeField] private int   maxJumps             = 2;
    [SerializeField] private float normalGravityScale   = 4.6f;  // WEIGHT 9 — heavy
    [SerializeField] private float freefallGravityScale = 6f;    // falls fast

    [Header("Dash")]
    [SerializeField] private float dashSpeed       = 10f;
    [SerializeField] private float dashDuration    = 0.18f;
    [SerializeField] private float dashCooldown    = 0.5f;
    [SerializeField] private float doubleTapWindow = 0.25f;

    // ─────────────────────────────────────────────────────────────────────────
    // M1 — TINGER BROCHURE  (short range melee slash)
    // ─────────────────────────────────────────────────────────────────────────

    [Header("M1 — Tinger Brochure")]
    [SerializeField] private float meleeRange    = 1.0f;  // short range
    [SerializeField] private float meleeWedge    = 90f;
    [SerializeField] private float meleeDamage   = 18f;   // ATK 9 — high damage
    [SerializeField] private float meleeCooldown = 0.35f;

    // ─────────────────────────────────────────────────────────────────────────
    // M2 — GUNDAM THROW  (big slow linear projectile, high damage, high knockback)
    // ─────────────────────────────────────────────────────────────────────────

    [Header("M2 — Gundam Throw")]
    [SerializeField] private float gundamDamage   = 30f;
    [SerializeField] private float gundamSpeed    = 5f;   // slow projectile
    [SerializeField] private float gundamCooldown = 1.2f;

    // ─────────────────────────────────────────────────────────────────────────
    // FUEL  (shared resource for E — Dumgun Blast)
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Fuel")]
    [SerializeField] private float maxFuel       = 9f;   // 3 full E charges
    [SerializeField] private float fuelRegenRate = 1f;   // units per second
    public  float currentFuel;

    // ─────────────────────────────────────────────────────────────────────────
    // Q — DUMGUN JETPACK
    // 5s total: 2s ascent → 3s hover. Cancel early with Q (cooldown stays same).
    // Initial launch blasts nearby enemies.
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Q — Dumgun Jetpack")]
    [SerializeField] private float jetpackAscentDuration = 2f;
    [SerializeField] private float jetpackHoverDuration  = 3f;
    [SerializeField] private float jetpackAscentSpeed    = 8f;
    [SerializeField] private float jetpackHoverGravity   = 0.15f;
    [SerializeField] private float jetpackMoveSpeed      = 3f;
    [SerializeField] private float jetpackLaunchDamage   = 15f;
    [SerializeField] private float jetpackLaunchRadius   = 2f;
    [SerializeField] private float jetpackCooldown       = 8f;

    // ─────────────────────────────────────────────────────────────────────────
    // E — DUMGUN BLAST
    // Explosive AOE. Costs 3 fuel. Slight delay. Can aim slightly up/down.
    // ─────────────────────────────────────────────────────────────────────────

    [Header("E — Dumgun Blast")]
    [SerializeField] private float dumgunDamage      = 35f;
    [SerializeField] private float dumgunKnockback   = 18f;
    [SerializeField] private float dumgunBlastRadius = 2.5f;
    [SerializeField] private float dumgunFiringDelay = 0.4f;
    [SerializeField] private float dumgunCooldown    = 5f;
    [SerializeField] private float dumgunAimRange    = 20f;
    [SerializeField] private int   dumgunFuelCost    = 3;

    // ─────────────────────────────────────────────────────────────────────────
    // R — INFINITY CHARGE
    // 2s channel (invincible) → high-speed dash. Massive damage + knockback.
    // Long endlag if whiffed. Aim slightly up/down before launch.
    // ─────────────────────────────────────────────────────────────────────────

    [Header("R — Infinity Charge")]
    [SerializeField] private float chargeChannelDuration = 2f;
    [SerializeField] private float chargeDashSpeed       = 30f;
    [SerializeField] private float chargeDashDuration    = 0.3f;
    [SerializeField] private float chargeDamage          = 40f;
    [SerializeField] private float chargeKnockback       = 25f;
    [SerializeField] private float chargeHitRadius       = 1.5f;
    [SerializeField] private float chargeWhiffEndlag     = 1.5f;
    [SerializeField] private float chargeAimRange        = 25f;
    [SerializeField] private float chargeCooldown        = 12f;

    // ─────────────────────────────────────────────────────────────────────────
    // S — SCROLL SESSION  (passive)
    // Holding S: cooldowns tick 1.5x faster. 1s delay after release to deactivate.
    // ─────────────────────────────────────────────────────────────────────────

    [Header("S — Scroll Session")]
    [SerializeField] private float scrollCooldownMultiplier = 1.5f;
    [SerializeField] private float scrollExitDelay          = 1f;

    // ─────────────────────────────────────────────────────────────────────────
    // PRIVATE STATE
    // ─────────────────────────────────────────────────────────────────────────

    // Movement
    public  bool  isFacingRight = true;
    private bool  isGrounded;
    private bool  wasGrounded;
    private bool  isCrouching;
    private bool  wasFalling;
    private float lastHorizontal;

    // Jump
    private int jumpsRemaining;

    // Dash
    private bool  isDashing;
    private float dashTimer;
    private float dashCooldownTimer;
    private float lastTapTimeLeft;
    private float lastTapTimeRight;

    // Attack
    private float meleeTimer;
    private float gundamTimer;
    private static readonly Vector2[] Cardinals     = { Vector2.right, Vector2.up, Vector2.left, Vector2.down };
    private static readonly string[]  CardinalNames = { "Right", "Up", "Left", "Down" };
    private int     lastCardinal = -1;
    private Vector2 mouseSnapPos;

    // Ability timers
    private float jetpackCooldownTimer;
    private float dumgunCooldownTimer;
    private float chargeCooldownTimer;

    // Ability states
    private bool isJetpacking;
    private bool isFiringDumgun;
    private bool isChanneling;
    public  bool isInvincible;

    // Jetpack Update-state (replaces coroutine)
    private enum JetpackPhase { None, Ascent, Hover }
    private JetpackPhase jetpackPhase = JetpackPhase.None;
    private float        jetpackPhaseTimer;

    // Scroll Session
    private bool  scrollActive;
    private float scrollExitTimer;

    // Input lock (blocks movement during certain abilities)
    private bool inputLocked;

    // ─────────────────────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────────────────────

    void Start()
    {
        rb       = GetComponent<Rigidbody2D>();
        col      = GetComponent<BoxCollider2D>();
        animator = GetComponent<Animator>();
        sr       = GetComponent<SpriteRenderer>();

        if (sr != null) originalColor = sr.color;

        jumpsRemaining  = maxJumps;
        rb.gravityScale = normalGravityScale;
        currentFuel     = maxFuel;

        // Prevent tunneling through floors at high fall speed
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        if (enemyLayer.value == 0)
            Debug.LogWarning("[Harry] enemyLayer is not set — melee and abilities will not hit enemies. Assign it in the Inspector.");

        Debug.Log($"[Harry] Ready — HP: full | Fuel: {currentFuel}/{maxFuel}");
    }

    void Update()
    {
        meleeTimer        -= Time.deltaTime;
        gundamTimer       -= Time.deltaTime;
        dashCooldownTimer -= Time.deltaTime;
        dashTimer         -= Time.deltaTime;

        wasGrounded = isGrounded;
        isGrounded  = CheckGrounded();

        HandleScrollSession();
        TickCooldowns();
        RegenerateFuel();

        if (!inputLocked)
        {
            HandleDash();
            HandleMovement();
            HandleJump();
            HandleCrouch();
            HandleFreefall();
            HandleMeleeAttack();
            HandleGundamThrow();
        }

        // Jetpack ticks every frame (manages inputLocked itself)
        HandleDumgunJetpack();

        if (!isChanneling && !isJetpacking && !isFiringDumgun)
        {
            HandleDumgunBlast();
            HandleInfinityCharge();
        }

        UpdateAnimator();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GROUND CHECK
    // ─────────────────────────────────────────────────────────────────────────

    bool CheckGrounded()
    {
        if (col == null) return false;
        Bounds  b      = col.bounds;
        Vector2 origin = new Vector2(b.center.x, b.min.y - 0.02f);
        RaycastHit2D hit = Physics2D.BoxCast(origin, new Vector2(b.size.x * 0.8f, 0.02f), 0f, Vector2.down, 0.15f);
        return hit.collider != null && hit.collider.gameObject != gameObject;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // MOVEMENT
    // ─────────────────────────────────────────────────────────────────────────

    void HandleMovement()
    {
        if (isDashing) return;
        float h     = Input.GetAxisRaw("Horizontal");
        float speed = (isCrouching ? moveSpeed * crouchSpeedMultiplier : moveSpeed);
        rb.linearVelocity = new Vector2(h * speed, rb.linearVelocity.y);

        if (h != lastHorizontal)
        {
            if      (h >  0f) Debug.Log("[Harry] Moving Right");
            else if (h <  0f) Debug.Log("[Harry] Moving Left");
            else              Debug.Log("[Harry] Stopped");
            lastHorizontal = h;
        }

        if      (h > 0f && !isFacingRight) Flip();
        else if (h < 0f &&  isFacingRight) Flip();
    }

    void Flip()
    {
        isFacingRight = !isFacingRight;
        transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DASH
    // ─────────────────────────────────────────────────────────────────────────

    void HandleDash()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            if (Time.time - lastTapTimeRight <= doubleTapWindow && dashCooldownTimer <= 0f) StartDash(1f);
            else lastTapTimeRight = Time.time;
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            if (Time.time - lastTapTimeLeft <= doubleTapWindow && dashCooldownTimer <= 0f) StartDash(-1f);
            else lastTapTimeLeft = Time.time;
        }
        if (isDashing && dashTimer <= 0f) EndDash();
    }

    void StartDash(float dir)
    {
        isDashing         = true;
        dashTimer         = dashDuration;
        dashCooldownTimer = dashCooldown;
        rb.linearVelocity = new Vector2(dashSpeed * dir, 0f);
        SetAnimBool("IsDashing", true);
        Debug.Log($"[Harry] Dash {(dir > 0f ? "Right" : "Left")}");
    }

    void EndDash()
    {
        isDashing = false;
        SetAnimBool("IsDashing", false);
        Debug.Log("[Harry] Dash End");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // JUMP
    // ─────────────────────────────────────────────────────────────────────────

    void HandleJump()
    {
        if (isGrounded && !wasGrounded)
        {
            jumpsRemaining  = maxJumps;
            rb.gravityScale = normalGravityScale;
            Debug.Log("[Harry] Landed");
        }

        bool jumpPressed = Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.W);
        if (!jumpPressed || isCrouching) return;

        if (jumpsRemaining == maxJumps && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpsRemaining--;
            SetAnimTrigger("Jump");
            Debug.Log("[Harry] Jump");
        }
        else if (jumpsRemaining > 0 && !isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, doubleJumpForce);
            jumpsRemaining--;
            SetAnimTrigger("DoubleJump");
            Debug.Log("[Harry] Double Jump");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CROUCH  (also activates Scroll Session passive)
    // ─────────────────────────────────────────────────────────────────────────

    void HandleCrouch()
    {
        bool hold = (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) && isGrounded;
        if (hold && !isCrouching)
        {
            isCrouching = true;
            SetAnimBool("IsCrouching", true);
            Debug.Log("[Harry] Crouch Start");
        }
        else if (!hold && isCrouching)
        {
            isCrouching = false;
            SetAnimBool("IsCrouching", false);
            Debug.Log("[Harry] Crouch End");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // FREEFALL
    // ─────────────────────────────────────────────────────────────────────────

    void HandleFreefall()
    {
        if (isJetpacking || inputLocked) return;
        bool falling = !isGrounded && rb.linearVelocity.y < 0f;
        if (falling && !wasFalling) Debug.Log("[Harry] Freefall");
        wasFalling      = falling;
        rb.gravityScale = falling ? freefallGravityScale : normalGravityScale;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // M1 — TINGER BROCHURE
    // ─────────────────────────────────────────────────────────────────────────

    void HandleMeleeAttack()
    {
        if (!Input.GetMouseButtonDown(0) || meleeTimer > 0f) return;

        meleeTimer = meleeCooldown;
        int     cardinal = GetSnappedCardinal(out Vector2 snapDir);
        lastCardinal     = cardinal;

        Vector2      origin = (Vector2)transform.position + snapDir * (meleeRange * 0.5f);
        // If enemyLayer isn't assigned fall back to all layers so attacks still land
        Collider2D[] hits = enemyLayer.value != 0
            ? Physics2D.OverlapCircleAll(origin, meleeRange * 0.5f, enemyLayer)
            : Physics2D.OverlapCircleAll(origin, meleeRange * 0.5f);

        int hitCount = 0;
        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;
            Vector2 toTarget = ((Vector2)hit.transform.position - (Vector2)transform.position).normalized;
            if (Vector2.Angle(snapDir, toTarget) <= meleeWedge * 0.5f)
            {
                EnemyDummy enemy = hit.GetComponent<EnemyDummy>();
                if (enemy != null)
                {
                    enemy.TakeDamage(meleeDamage, snapDir);
                    Debug.Log($"[Harry] Tinger Brochure hit '{hit.name}' for {meleeDamage}");
                    hitCount++;
                }
            }
        }
        Debug.Log($"[Harry] Tinger Brochure {CardinalNames[cardinal]} — {hitCount} hit(s) (checked {hits.Length} colliders)");
        StartCoroutine(FlashColor(ColorMelee, 0.08f));
    }

    int GetSnappedCardinal(out Vector2 snapDir)
    {
        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseSnapPos       = mouseWorld;
        Vector2 dir        = (mouseWorld - (Vector2)transform.position).normalized;
        int best = 0; float bestDot = float.MinValue;
        for (int i = 0; i < Cardinals.Length; i++)
        {
            float d = Vector2.Dot(dir, Cardinals[i]);
            if (d > bestDot) { bestDot = d; best = i; }
        }
        snapDir = Cardinals[best];
        return best;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // M2 — GUNDAM THROW
    // ─────────────────────────────────────────────────────────────────────────

    void HandleGundamThrow()
    {
        if (!Input.GetMouseButtonDown(1) || gundamTimer > 0f) return;

        gundamTimer = gundamCooldown;
        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 dir        = (mouseWorld - (Vector2)transform.position).normalized;

        if (gundamProjectilePrefab != null)
        {
            Vector3    spawnPos = transform.position + (Vector3)(dir * 1f);
            GameObject p       = Instantiate(gundamProjectilePrefab, spawnPos, Quaternion.identity);
            Projectile proj    = p.GetComponent<Projectile>();
            if (proj != null)
            {
                proj.damage = gundamDamage;
                proj.speed  = gundamSpeed;
                proj.Init(dir, enemyLayer);
            }
            p.transform.right = dir;
            Debug.Log($"[Harry] Gundam Throw fired — direction {dir}");
            StartCoroutine(FlashColor(ColorGundam, 0.15f));
        }
        else
        {
            Debug.LogWarning("[Harry] Gundam Throw — no projectile prefab assigned.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // S — SCROLL SESSION
    // ─────────────────────────────────────────────────────────────────────────

    void HandleScrollSession()
    {
        bool held = (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) && isGrounded;

        if (held)
        {
            scrollExitTimer = scrollExitDelay;
            if (!scrollActive)
            {
                scrollActive = true;
                if (!isChanneling && !isJetpacking && !isFiringDumgun) SetColor(ColorScroll);
                Debug.Log("[Harry] Scroll Session — ACTIVE (cooldowns ×1.5)");
            }
        }
        else if (scrollActive)
        {
            scrollExitTimer -= Time.deltaTime;
            if (scrollExitTimer <= 0f)
            {
                scrollActive = false;
                RestoreColor();
                Debug.Log("[Harry] Scroll Session — INACTIVE");
            }
        }
    }

    void TickCooldowns()
    {
        float tick = Time.deltaTime * (scrollActive ? scrollCooldownMultiplier : 1f);
        jetpackCooldownTimer -= tick;
        dumgunCooldownTimer  -= tick;
        chargeCooldownTimer  -= tick;
    }

    void RegenerateFuel()
    {
        if (currentFuel < maxFuel)
            currentFuel = Mathf.Min(currentFuel + fuelRegenRate * Time.deltaTime, maxFuel);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Q — DUMGUN JETPACK
    // ─────────────────────────────────────────────────────────────────────────

    void HandleDumgunJetpack()
    {
        float h = Input.GetAxisRaw("Horizontal");

        // ── Activate ─────────────────────────────────────────────────────────
        if (!isJetpacking)
        {
            if (isChanneling || isFiringDumgun) return;
            if (!Input.GetKeyDown(KeyCode.Q)) return;
            if (jetpackCooldownTimer > 0f)
            {
                Debug.Log($"[Harry] Jetpack cooldown — {jetpackCooldownTimer:0.0}s");
                return;
            }

            isJetpacking         = true;
            inputLocked          = true;
            jetpackCooldownTimer = jetpackCooldown;
            jetpackPhase         = JetpackPhase.Ascent;
            jetpackPhaseTimer    = jetpackAscentDuration;
            rb.gravityScale      = 0f;

            BlastAOE(transform.position, jetpackLaunchRadius, jetpackLaunchDamage, Vector2.up);
            SetColor(ColorJetpack);
            Debug.Log("[Harry] Jetpack — Launch blast + Ascending");
            return;
        }

        // ── Ascent ───────────────────────────────────────────────────────────
        if (jetpackPhase == JetpackPhase.Ascent)
        {
            rb.linearVelocity  = new Vector2(h * jetpackMoveSpeed, jetpackAscentSpeed);
            jetpackPhaseTimer -= Time.deltaTime;

            bool cancel = Input.GetKeyDown(KeyCode.Q);
            if (cancel)
            {
                Debug.Log("[Harry] Jetpack — Cancelled during ascent");
                EndJetpack();
            }
            else if (jetpackPhaseTimer <= 0f)
            {
                jetpackPhase      = JetpackPhase.Hover;
                jetpackPhaseTimer = jetpackHoverDuration;
                rb.gravityScale   = jetpackHoverGravity;
                SetColor(ColorHover);
                Debug.Log("[Harry] Jetpack — Hovering");
            }
            return;
        }

        // ── Hover ────────────────────────────────────────────────────────────
        if (jetpackPhase == JetpackPhase.Hover)
        {
            rb.linearVelocity  = new Vector2(h * jetpackMoveSpeed, rb.linearVelocity.y);
            jetpackPhaseTimer -= Time.deltaTime;

            bool cancel = Input.GetKeyDown(KeyCode.Q);
            if (cancel) Debug.Log("[Harry] Jetpack — Cancelled during hover");
            if (cancel || jetpackPhaseTimer <= 0f) EndJetpack();
        }
    }

    void EndJetpack()
    {
        rb.gravityScale = normalGravityScale;
        isJetpacking    = false;
        inputLocked     = false;
        jetpackPhase    = JetpackPhase.None;
        RestoreColor();
        Debug.Log($"[Harry] Jetpack — Ended — cooldown {jetpackCooldown}s");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // E — DUMGUN BLAST
    // ─────────────────────────────────────────────────────────────────────────

    void HandleDumgunBlast()
    {
        if (!Input.GetKeyDown(KeyCode.E)) return;
        if (dumgunCooldownTimer > 0f) { Debug.Log($"[Harry] Dumgun cooldown — {dumgunCooldownTimer:0.0}s"); return; }
        if (currentFuel < dumgunFuelCost) { Debug.Log($"[Harry] Dumgun — no fuel ({currentFuel:0.0}/{maxFuel})"); return; }
        StartCoroutine(DumgunBlastCoroutine());
    }

    IEnumerator DumgunBlastCoroutine()
    {
        isFiringDumgun      = true;
        dumgunCooldownTimer = dumgunCooldown;
        currentFuel        -= dumgunFuelCost;

        float   vert       = Input.GetAxisRaw("Vertical");
        float   aimAngle   = Mathf.Clamp(vert * dumgunAimRange, -dumgunAimRange, dumgunAimRange);
        float   baseAngle  = isFacingRight ? 0f : 180f;
        Vector2 aimDir     = new Vector2(Mathf.Cos((baseAngle + aimAngle) * Mathf.Deg2Rad), Mathf.Sin(aimAngle * Mathf.Deg2Rad));

        Debug.Log($"[Harry] Dumgun Blast — charging ({dumgunFiringDelay}s delay) — fuel: {currentFuel}/{maxFuel}");
        SetColor(ColorDumgun);
        yield return new WaitForSeconds(dumgunFiringDelay);

        StartCoroutine(FlashColor(ColorLaunch, 0.1f));
        Vector2 blastOrigin = (Vector2)transform.position + aimDir * dumgunBlastRadius * 0.5f;
        BlastAOE(blastOrigin, dumgunBlastRadius, dumgunDamage, aimDir * dumgunKnockback);
        Debug.Log($"[Harry] Dumgun Blast — FIRED toward {aimDir}");

        isFiringDumgun = false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // R — INFINITY CHARGE
    // ─────────────────────────────────────────────────────────────────────────

    void HandleInfinityCharge()
    {
        if (!Input.GetKeyDown(KeyCode.R)) return;
        if (chargeCooldownTimer > 0f) { Debug.Log($"[Harry] Infinity Charge cooldown — {chargeCooldownTimer:0.0}s"); return; }
        StartCoroutine(InfinityChargeCoroutine());
    }

    IEnumerator InfinityChargeCoroutine()
    {
        isChanneling        = true;
        isInvincible        = true;
        inputLocked         = true;
        chargeCooldownTimer = chargeCooldown;

        rb.linearVelocity = Vector2.zero;
        rb.gravityScale   = 0f;

        Debug.Log("[Harry] Infinity Charge — CHANNELING (invincible)");
        SetAnimBool("IsChanneling", true);
        SetColor(ColorCharge);

        yield return new WaitForSeconds(chargeChannelDuration);

        isInvincible = false;
        StartCoroutine(FlashColor(ColorLaunch, 0.12f));

        float   vert      = Input.GetAxisRaw("Vertical");
        float   aimAngle  = Mathf.Clamp(vert * chargeAimRange, -chargeAimRange, chargeAimRange);
        float   baseAngle = isFacingRight ? 0f : 180f;
        Vector2 dashDir   = new Vector2(Mathf.Cos((baseAngle + aimAngle) * Mathf.Deg2Rad), Mathf.Sin(aimAngle * Mathf.Deg2Rad)).normalized;

        rb.gravityScale   = normalGravityScale;
        rb.linearVelocity = dashDir * chargeDashSpeed;

        Debug.Log($"[Harry] Infinity Charge — LAUNCHED toward {dashDir}");
        SetAnimTrigger("InfinityCharge");

        float dashTimer    = 0f;
        bool  hitSomething = false;
        while (dashTimer < chargeDashDuration)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, chargeHitRadius, enemyLayer);
            foreach (var hit in hits)
            {
                hit.GetComponent<EnemyDummy>()?.TakeDamage(chargeDamage, dashDir * chargeKnockback);
                Debug.Log($"[Harry] Infinity Charge — HIT '{hit.name}'");
                hitSomething = true;
            }
            dashTimer += Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (!hitSomething)
        {
            Debug.Log($"[Harry] Infinity Charge — WHIFFED — endlag {chargeWhiffEndlag}s");
            yield return new WaitForSeconds(chargeWhiffEndlag);
        }

        SetAnimBool("IsChanneling", false);
        isChanneling = false;
        inputLocked  = false;
        RestoreColor();
        Debug.Log("[Harry] Infinity Charge — Complete");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────────────────────

    void BlastAOE(Vector2 origin, float radius, float damage, Vector2 knockbackDir)
    {
        Collider2D[] hits = enemyLayer.value != 0
            ? Physics2D.OverlapCircleAll(origin, radius, enemyLayer)
            : Physics2D.OverlapCircleAll(origin, radius);

        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;
            EnemyDummy enemy = hit.GetComponent<EnemyDummy>();
            if (enemy == null) continue;
            Vector2 dir = ((Vector2)hit.transform.position - origin).normalized;
            enemy.TakeDamage(damage, knockbackDir == Vector2.zero ? dir : knockbackDir);
            Debug.Log($"[Harry] AOE hit '{hit.name}' for {damage}");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ANIMATOR  (safe helpers — skip missing parameters silently)
    // ─────────────────────────────────────────────────────────────────────────

    void UpdateAnimator()
    {
        if (animator == null) return;
        SetAnimFloat("Speed",            Mathf.Abs(rb.linearVelocity.x));
        SetAnimBool ("IsGrounded",       isGrounded);
        SetAnimFloat("VerticalVelocity", rb.linearVelocity.y);
        SetAnimBool ("IsChanneling",     isChanneling);
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
    // VISUAL CUES
    // ─────────────────────────────────────────────────────────────────────────

    void SetColor(Color c)
    {
        if (sr != null) sr.color = c;
    }

    void RestoreColor()
    {
        if (sr != null) sr.color = scrollActive ? ColorScroll : originalColor;
    }

    IEnumerator FlashColor(Color c, float duration)
    {
        SetColor(c);
        yield return new WaitForSeconds(duration);
        RestoreColor();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GIZMOS
    // ─────────────────────────────────────────────────────────────────────────

    void OnDrawGizmos()
    {
        if (!Application.isPlaying || !HitboxDebugger.showHitboxes) return;

        // Melee swing wedge — orange, fades over cooldown
        if (lastCardinal >= 0 && meleeTimer > 0f)
        {
            float   alpha  = Mathf.Clamp01(meleeTimer / meleeCooldown);
            float   mid    = Mathf.Atan2(Cardinals[lastCardinal].y, Cardinals[lastCardinal].x) * Mathf.Rad2Deg;
            Gizmos.color   = new Color(1f, 0.4f, 0f, alpha * 0.7f);
            DrawWedge(transform.position, meleeRange, mid - meleeWedge * 0.5f, mid + meleeWedge * 0.5f, 16);
            Gizmos.color   = new Color(1f, 0.4f, 0f, alpha);
            DrawCircle(transform.position, meleeRange, 32);
        }

        // Jetpack launch radius
        if (isJetpacking)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, jetpackLaunchRadius);
        }

        // Dumgun blast radius
        if (isFiringDumgun)
        {
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, dumgunBlastRadius);
        }

        // Infinity Charge hit radius
        if (isChanneling)
        {
            Gizmos.color = new Color(0.5f, 0f, 1f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, chargeHitRadius);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (col == null) col = GetComponent<BoxCollider2D>();
        if (col == null) return;
        Bounds  b      = col.bounds;
        Vector3 origin = new Vector3(b.center.x, b.min.y - 0.09f, 0f);
        Gizmos.color   = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireCube(origin, new Vector3(b.size.x * 0.8f, 0.15f, 0f));
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
