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
    [SerializeField] private GameObject    gundamGrenadePrefab;    // M2 + E — needs GundamGrenade script
    [SerializeField] private GameObject    clusterBombletPrefab;   // E bomblets — leave empty to reuse gundamGrenadePrefab

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
    // M2 — GUNDAM GRENADE  (right click — arc throw, sticks, explodes)
    // ─────────────────────────────────────────────────────────────────────────

    [Header("M2 — Gundam Grenade")]
    [SerializeField] private float grenadeDamage   = 40f;
    [SerializeField] private float grenadeRadius   = 2.5f;
    [SerializeField] private float grenadeFuse     = 1.3f;
    [SerializeField] private float grenadeArcTime  = 1.5f;
    [SerializeField] private float grenadeCooldown = 4f;

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

    [Header("E — Cluster Grenade")]
    [SerializeField] private float dumgunDamage          = 15f;   // damage per bomblet
    [SerializeField] private float dumgunExplosionRadius = 1.2f;  // radius per bomblet
    [SerializeField] private int   dumgunProjectileCount = 6;     // number of bomblets
    [SerializeField] private float dumgunSpread          = 7f;    // bomblet outward speed
    [SerializeField] private float dumgunUmbrellaAngle   = 160f;  // total spread of umbrella (degrees)
    [SerializeField] private float dumgunFuseDuration    = 0.7f;  // bomblet fuse after split
    [SerializeField] private float dumgunMainFuse        = 0.8f;  // main grenade fuse before splitting
    [SerializeField] private float dumgunArcTime         = 1.5f;  // flight time to target
    [SerializeField] private float dumgunCooldown        = 8f;
    [SerializeField] private int   dumgunFuelCost        = 3;
    [SerializeField] private int   reticleSteps          = 40;

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
    private bool  wasRunning;
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

    // Gundam Grenade reticle
    private LineRenderer reticleLine;
    private bool         reticleVisible = true;
    private Camera       cam;

    // Active cluster grenade (E again to detonate early)
    private GundamGrenade activeCluster;

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
        cam = Camera.main;
        if (cam == null) cam = FindFirstObjectByType<Camera>();

        if (enemyLayer.value == 0)
            Debug.LogWarning("[Harry] enemyLayer is not set — melee and abilities will not hit enemies. Assign it in the Inspector.");

        SetupDumgunReticle();

        Debug.Log($"[Harry] Ready — HP: full | Fuel: {currentFuel}/{maxFuel}");
    }

    void OnDestroy()
    {
        if (reticleLine != null) Destroy(reticleLine.gameObject);
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
        UpdateDumgunReticle();

        // Tab toggles the grenade reticle
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            reticleVisible = !reticleVisible;
            Debug.Log($"[Harry] Grenade reticle {(reticleVisible ? "ON" : "OFF")}");
        }

        if (!inputLocked)
        {
            HandleDash();
            HandleMovement();
            HandleJump();
            HandleCrouch();
            HandleFreefall();
            HandleMeleeAttack();
            HandleGundamGrenade();
        }

        // Jetpack ticks every frame (manages inputLocked itself)
        HandleDumgunJetpack();

        if (!isChanneling && !isJetpacking)
        {
            HandleClusterGrenade();
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
        Vector2 mouseWorld = cam != null ? MouseWorldPos() : (Vector2)transform.position + Vector2.right;
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
    // M2 — GUNDAM GRENADE  (right click)
    // ─────────────────────────────────────────────────────────────────────────

    void HandleGundamGrenade()
    {
        if (!Input.GetMouseButtonDown(1) || gundamTimer > 0f) return;
        if (gundamGrenadePrefab == null) { Debug.LogWarning("[Harry] Gundam Grenade — assign prefab."); return; }

        gundamTimer = grenadeCooldown;
        Vector2 spawnPos = (Vector2)transform.position + Vector2.up * 0.3f;
        Vector2 target   = MouseWorldPos();
        Vector2 vel      = CalcGrenadeVelocity(spawnPos, target, grenadeArcTime);
        ThrowGrenade(vel, gundamGrenadePrefab, grenadeDamage, grenadeRadius, grenadeFuse, false);
        StartCoroutine(FlashColor(ColorGundam, 0.15f));
        Debug.Log("[Harry] Gundam Grenade thrown");
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
    // E — CLUSTER GRENADE
    // Arcs toward mouse, sticks, splits into bomblets that scatter and explode.
    // Tab = toggle reticle.
    // ─────────────────────────────────────────────────────────────────────────

    void HandleClusterGrenade()
    {
        if (!Input.GetKeyDown(KeyCode.E)) return;

        // E again while one is in the air — detonate early
        if (activeCluster != null)
        {
            activeCluster.Explode();
            activeCluster = null;
            Debug.Log("[Harry] Cluster — early detonate");
            return;
        }

        if (dumgunCooldownTimer > 0f) { Debug.Log($"[Harry] Cluster cooldown — {dumgunCooldownTimer:0.0}s"); return; }
        if (currentFuel < dumgunFuelCost) { Debug.Log($"[Harry] Cluster — no fuel ({currentFuel:0.0}/{maxFuel})"); return; }
        if (gundamGrenadePrefab == null) { Debug.LogWarning("[Harry] Cluster Grenade — assign prefab."); return; }

        dumgunCooldownTimer = dumgunCooldown;
        currentFuel        -= dumgunFuelCost;

        Vector2 spawnPos = (Vector2)transform.position + Vector2.up * 0.3f;
        Vector2 mouse    = MouseWorldPos();
        Vector2 vel      = CalcGrenadeVelocity(spawnPos, mouse, dumgunArcTime);

        GameObject bombletPrefab = clusterBombletPrefab != null ? clusterBombletPrefab : gundamGrenadePrefab;
        activeCluster = ThrowGrenade(vel, gundamGrenadePrefab, dumgunDamage, dumgunExplosionRadius, dumgunMainFuse,
                                     true, dumgunProjectileCount, dumgunSpread, dumgunFuseDuration, bombletPrefab, dumgunUmbrellaAngle);

        StartCoroutine(FlashColor(ColorDumgun, 0.15f));
        Debug.Log($"[Harry] Cluster Grenade thrown — {dumgunProjectileCount} bomblets — fuel: {currentFuel}/{maxFuel}");
    }

    GundamGrenade ThrowGrenade(Vector2 velocity, GameObject prefab, float damage, float radius, float fuse,
                               bool cluster, int bombletCount = 0, float spread = 0f, float bombletFuse = 0f,
                               GameObject bombletPrefab = null, float umbrellaAngle = 160f)
    {
        Vector2       spawnPos = (Vector2)transform.position + Vector2.up * 0.3f;
        GameObject    go       = Instantiate(prefab, spawnPos, Quaternion.identity);
        GundamGrenade gren     = go.GetComponent<GundamGrenade>();
        if (gren != null)
        {
            gren.explosionDamage      = damage;
            gren.explosionRadius      = radius;
            gren.fuseDuration         = fuse;
            gren.hitLayer             = enemyLayer;
            gren.throwerCollider      = col;
            gren.isCluster            = cluster;
            gren.clusterCount         = bombletCount;
            gren.clusterSpread        = spread;
            gren.clusterBombletFuse   = bombletFuse;
            gren.clusterBombletPrefab = bombletPrefab;
            gren.clusterUmbrellaAngle = umbrellaAngle;
            gren.Launch(velocity);
        }
        return gren;
    }

    Vector2 MouseWorldPos()
    {
        Vector3 mp = Input.mousePosition;
        mp.z = cam.nearClipPlane + Mathf.Abs(cam.transform.position.z);
        return cam.ScreenToWorldPoint(mp);
    }

    Vector2 CalcGrenadeVelocity(Vector2 start, Vector2 target, float arcTime)
    {
        float g  = Physics2D.gravity.y;
        float dx = target.x - start.x;
        float dy = target.y - start.y;
        return new Vector2(
            dx / arcTime,
            dy / arcTime - 0.5f * g * arcTime
        );
    }

    void SetupDumgunReticle()
    {
        GameObject obj    = new GameObject("DumgunReticle");
        reticleLine       = obj.AddComponent<LineRenderer>();
        reticleLine.positionCount = reticleSteps;
        reticleLine.startWidth    = 0.06f;
        reticleLine.endWidth      = 0.02f;
        reticleLine.useWorldSpace = true;
        reticleLine.sortingOrder  = 10;
        reticleLine.material      = new Material(Shader.Find("Sprites/Default"));
        reticleLine.startColor    = new Color(1f, 0.5f, 0f, 0.9f);
        reticleLine.endColor      = new Color(1f, 0.5f, 0f, 0.1f);
        reticleLine.enabled       = false;
    }

    void UpdateDumgunReticle()
    {
        if (reticleLine == null || cam == null) return;

        bool m2Ready      = gundamTimer <= 0f;
        bool eReady       = dumgunCooldownTimer <= 0f && currentFuel >= dumgunFuelCost;
        bool shouldShow   = reticleVisible && (m2Ready || eReady) && !isChanneling && !isJetpacking && gundamGrenadePrefab != null;
        reticleLine.enabled = shouldShow;
        if (!shouldShow) return;

        // Orange = M2 ready, yellow = E (cluster) ready
        Color arcColor = eReady ? new Color(1f, 0.85f, 0f, 0.9f) : new Color(1f, 0.5f, 0f, 0.9f);
        reticleLine.startColor = arcColor;
        reticleLine.endColor   = new Color(arcColor.r, arcColor.g, arcColor.b, 0.1f);

        Vector2 start   = (Vector2)transform.position + Vector2.up * 0.3f;
        Vector2 mouse   = MouseWorldPos();
        float   arcTime = eReady ? dumgunArcTime : grenadeArcTime;
        Vector2 vel     = CalcGrenadeVelocity(start, mouse, arcTime);
        float   g       = Physics2D.gravity.y;
        float   dt      = arcTime / reticleSteps;

        for (int i = 0; i < reticleSteps; i++)
        {
            float t = i * dt;
            reticleLine.SetPosition(i, new Vector3(
                start.x + vel.x * t,
                start.y + vel.y * t + 0.5f * g * t * t,
                0f
            ));
        }
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
        var   alreadyHit   = new System.Collections.Generic.HashSet<GameObject>();
        while (dashTimer < chargeDashDuration)
        {
            Collider2D[] hits = enemyLayer.value != 0
                ? Physics2D.OverlapCircleAll(transform.position, chargeHitRadius, enemyLayer)
                : Physics2D.OverlapCircleAll(transform.position, chargeHitRadius);

            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject) continue;
                if (alreadyHit.Contains(hit.gameObject)) continue;
                EnemyDummy enemy = hit.GetComponent<EnemyDummy>();
                if (enemy == null) continue;
                alreadyHit.Add(hit.gameObject);
                enemy.TakeDamage(chargeDamage, dashDir * chargeKnockback);
                Debug.Log($"[Harry] Infinity Charge — HIT '{hit.name}' for {chargeDamage}");
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
        bool running = Mathf.Abs(rb.linearVelocity.x) > 0.1f;
        if (running != wasRunning)
        {
            Debug.Log($"[Harry] {(running ? "Running" : "Stopped running")}");
            wasRunning = running;
        }
        SetAnimBool("isRunning", running);
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
            Gizmos.DrawWireSphere(transform.position, dumgunExplosionRadius);
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
