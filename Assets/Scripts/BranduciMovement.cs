// Branduci — Self-contained movement and abilities script.
// Archetype: Speedster — wins through mobility, stage control, and sauce terrain advantage.
// Do NOT attach coreMovement1 alongside this. This script is fully independent.
//
// Stats (ATK 6 | DEF 5 | SPD 9 | JUMP 7 | RECOVERY 6 | WEIGHT 5)
//
// Controls:
//   A/D         — Move
//   W / Space   — Jump (double jump)
//   S           — Crouch
//   AA / DD     — Dash (hold M1 during dash = Sliding Kick)
//   Left Click  — M1: Attack (ground: 3-hit combo | W+click: Up | S+click: Down | air: aerial set)
//   Right Click — M2: Smash (wide mic arc swing)
//   Q           — Freestyle: AOE rap burst (main attack)
//   E           — Boombox Drop: stun + force dance 2s
//   R           — Sauce Spill: slippery zone 8s (Branduci +40% speed in own sauce)
//   T           — Big Arch Sauce: wide sauce splatter (gadget)
//   F           — Chart Topper: ult — deafens all enemies 4s

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BranduciMovement : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    // REFERENCES
    // ─────────────────────────────────────────────────────────────────────────

    [Header("References")]
    [SerializeField] private LayerMask enemyLayer;

    private Rigidbody2D    rb;
    private BoxCollider2D  col;
    private Animator       animator;
    private SpriteRenderer sr;
    private Color          originalColor;
    private bool           inOwnSauce;

    private static readonly Color ColorCombo     = new Color(1f,    1f,    1f,    1f);   // white flash
    private static readonly Color ColorSmash     = new Color(1f,    0.6f,  0.1f,  1f);   // orange
    private static readonly Color ColorFreestyle = new Color(1f,    0.85f, 0f,    1f);   // gold
    private static readonly Color ColorBoombox   = new Color(0.2f,  0.5f,  1f,    1f);   // blue
    private static readonly Color ColorSauce     = new Color(0.3f,  1f,    0.3f,  1f);   // green
    private static readonly Color ColorUlt       = new Color(0.9f,  0.1f,  0.9f,  1f);   // magenta

    // ─────────────────────────────────────────────────────────────────────────
    // MOVEMENT  (SPD 9 — extremely fast)
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Movement")]
    [SerializeField] private float moveSpeed             = 7.2f;  // SPD 9 × 0.8
    [SerializeField] private float crouchSpeedMultiplier = 0.5f;
    [SerializeField] private float sauceMoveBonus        = 0.4f;  // +40% in own sauce

    [Header("Jump")]
    [SerializeField] private float jumpForce          = 21f;    // JUMP 7 × 3.0
    [SerializeField] private float doubleJumpForce    = 12f;    // RECOVERY 6 × 2.0
    [SerializeField] private int   maxJumps           = 2;
    [SerializeField] private float normalGravityScale = 2.5f;   // WEIGHT 5 × 0.5
    [SerializeField] private float freefallGravity    = 3.5f;

    [Header("Dash")]
    [SerializeField] private float dashSpeed       = 18f;
    [SerializeField] private float dashDuration    = 0.15f;
    [SerializeField] private float dashCooldown    = 0.3f;
    [SerializeField] private float doubleTapWindow = 0.25f;

    // ─────────────────────────────────────────────────────────────────────────
    // M1 — ATTACK COMBO
    // Ground: Jab → Mic Swing → Trip Kick (3 hits)
    // Directional: W = Up Attack, S = Down Attack
    // Air: Aerial Neutral / Up / Down
    // Dash + M1: Sliding Kick
    // ─────────────────────────────────────────────────────────────────────────

    [Header("M1 — Attack Combo")]
    [SerializeField] private float comboRange    = 1.2f;
    [SerializeField] private float comboWindow   = 0.5f;   // time to press next hit
    [SerializeField] private float comboStep1Dmg = 8f;     // Jab
    [SerializeField] private float comboStep2Dmg = 10f;    // Mic Swing
    [SerializeField] private float comboStep3Dmg = 14f;    // Trip Kick
    [SerializeField] private float upAtkDamage   = 12f;    // Up Attack / Aerial Up
    [SerializeField] private float downAtkDamage = 10f;    // Down Attack / Aerial Down
    [SerializeField] private float aerialNeutDmg = 10f;    // Aerial Neutral
    [SerializeField] private float aerialDownDmg = 14f;    // Aerial Down spike
    [SerializeField] private float dashAtkDamage = 12f;    // Sliding Kick

    // ─────────────────────────────────────────────────────────────────────────
    // M2 — SMASH ATTACK
    // Wide mic arc — right click
    // ─────────────────────────────────────────────────────────────────────────

    [Header("M2 — Smash Attack")]
    [SerializeField] private float smashRange    = 2f;
    [SerializeField] private float smashWedge    = 180f;
    [SerializeField] private float smashDamage   = 20f;
    [SerializeField] private float smashCooldown = 0.8f;

    // ─────────────────────────────────────────────────────────────────────────
    // Q — FREESTYLE
    // AOE rap burst. Slight wind-up. All enemies in radius take music damage.
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Q — Freestyle")]
    [SerializeField] private float freestyleRadius   = 3f;
    [SerializeField] private float freestyleDamage   = 22f;
    [SerializeField] private float freestyleWindup   = 0.3f;
    [SerializeField] private float freestyleCooldown = 5f;

    // ─────────────────────────────────────────────────────────────────────────
    // E — BOOMBOX DROP
    // Hits nearby enemies. Stuns + forces dance animation for 2s.
    // ─────────────────────────────────────────────────────────────────────────

    [Header("E — Boombox Drop")]
    [SerializeField] private float boomboxRadius   = 2.5f;
    [SerializeField] private float boomboxDamage   = 12f;
    [SerializeField] private float boomboxStunTime = 2f;
    [SerializeField] private float boomboxCooldown = 8f;

    // ─────────────────────────────────────────────────────────────────────────
    // R — SAUCE SPILL
    // Drops a sauce zone. Opponents slowed. Branduci +40% speed inside.
    // ─────────────────────────────────────────────────────────────────────────

    [Header("R — Sauce Spill")]
    [SerializeField] private float sauceRadius   = 3f;
    [SerializeField] private float sauceDuration = 8f;
    [SerializeField] private float sauceCooldown = 10f;

    // ─────────────────────────────────────────────────────────────────────────
    // T — BIG ARCH SAUCE  (Gadget)
    // Larger sauce splatter on terrain.
    // ─────────────────────────────────────────────────────────────────────────

    [Header("T — Big Arch Sauce")]
    [SerializeField] private float bigSauceRadius   = 5f;
    [SerializeField] private float bigSauceDuration = 6f;
    [SerializeField] private float bigSauceCooldown = 14f;

    // ─────────────────────────────────────────────────────────────────────────
    // F — CHART TOPPER  (Ult)
    // Deafens all enemies for 4s, disabling audio cues.
    // ─────────────────────────────────────────────────────────────────────────

    [Header("F — Chart Topper (Ult)")]
    [SerializeField] private float chartRadius   = 10f;
    [SerializeField] private float chartWindup   = 0.4f;
    [SerializeField] private float chartDuration = 4f;
    [SerializeField] private float chartCooldown = 20f;

    // ─────────────────────────────────────────────────────────────────────────
    // PRIVATE STATE
    // ─────────────────────────────────────────────────────────────────────────

    public bool isFacingRight = true;
    private bool  isGrounded;
    private bool  wasGrounded;
    private bool  isCrouching;
    private bool  wasFalling;
    private float lastHorizontal;

    private int   jumpsRemaining;
    private bool  isDashing;
    private float dashTimer;
    private float dashCooldownTimer;
    private float lastTapTimeLeft;
    private float lastTapTimeRight;

    private int   comboStep;       // 0 = idle, 1/2 = mid-combo
    private float comboTimer;      // window to next hit
    private float comboLockTimer;  // brief lock per swing

    private float smashTimer;
    private float freestyleCooldownTimer;
    private float boomboxCooldownTimer;
    private float sauceCooldownTimer;
    private float bigSauceCooldownTimer;
    private float chartCooldownTimer;

    private bool inputLocked;

    private readonly List<GameObject> activeSauceZones = new List<GameObject>();

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

        jumpsRemaining            = maxJumps;
        rb.gravityScale           = normalGravityScale;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        if (enemyLayer.value == 0)
            Debug.LogWarning("[Branduci] enemyLayer not set — attacks will hit any EnemyDummy component.");

        Debug.Log("[Branduci] Ready");
    }

    void Update()
    {
        smashTimer               -= Time.deltaTime;
        dashTimer                -= Time.deltaTime;
        dashCooldownTimer        -= Time.deltaTime;
        freestyleCooldownTimer   -= Time.deltaTime;
        boomboxCooldownTimer     -= Time.deltaTime;
        sauceCooldownTimer       -= Time.deltaTime;
        bigSauceCooldownTimer    -= Time.deltaTime;
        chartCooldownTimer       -= Time.deltaTime;

        if (comboTimer > 0f) comboTimer      -= Time.deltaTime;
        else if (comboStep > 0 && comboLockTimer <= 0f) comboStep = 0;
        if (comboLockTimer > 0f) comboLockTimer -= Time.deltaTime;

        wasGrounded = isGrounded;
        isGrounded  = CheckGrounded();

        if (!inputLocked)
        {
            HandleDash();
            HandleMovement();
            HandleJump();
            HandleCrouch();
            HandleFreefall();
            HandleAttack();
            HandleSmash();
        }

        HandleFreestyle();
        HandleBoomboxDrop();
        HandleSauceSpill();
        HandleBigArchSauce();
        HandleChartTopper();

        UpdateAnimator();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GROUND CHECK
    // ─────────────────────────────────────────────────────────────────────────

    bool CheckGrounded()
    {
        if (col == null) return false;
        Bounds   b   = col.bounds;
        Vector2  org = new Vector2(b.center.x, b.min.y - 0.02f);
        RaycastHit2D hit = Physics2D.BoxCast(org, new Vector2(b.size.x * 0.8f, 0.02f), 0f, Vector2.down, 0.15f);
        return hit.collider != null && hit.collider.gameObject != gameObject;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // MOVEMENT
    // ─────────────────────────────────────────────────────────────────────────

    void HandleMovement()
    {
        if (isDashing) return;
        float speed = moveSpeed * (isCrouching ? crouchSpeedMultiplier : 1f);
        if (inOwnSauce) speed *= (1f + sauceMoveBonus);

        float h = Input.GetAxisRaw("Horizontal");
        rb.linearVelocity = new Vector2(h * speed, rb.linearVelocity.y);
        lastHorizontal    = h;

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
        Debug.Log($"[Branduci] Dash {(dir > 0f ? "Right" : "Left")}");

        if (Input.GetMouseButton(0)) DashAttack();
    }

    void EndDash()
    {
        isDashing = false;
        SetAnimBool("IsDashing", false);
    }

    void DashAttack()
    {
        Vector2 dir = isFacingRight ? Vector2.right : Vector2.left;
        Collider2D[] hits = OverlapEnemies((Vector2)transform.position + dir * 0.8f, 1f);
        foreach (var h in hits)
            h.GetComponent<EnemyDummy>()?.TakeDamage(dashAtkDamage, dir * 8f);
        Debug.Log($"[Branduci] Sliding Kick — {hits.Length} hit(s)");
        StartCoroutine(FlashColor(ColorCombo, 0.1f));
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
            Debug.Log("[Branduci] Landed");
        }
        bool pressed = Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.W);
        if (!pressed || isCrouching) return;

        if (jumpsRemaining == maxJumps && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpsRemaining--;
            SetAnimTrigger("Jump");
            Debug.Log("[Branduci] Jump");
        }
        else if (jumpsRemaining > 0 && !isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, doubleJumpForce);
            jumpsRemaining--;
            SetAnimTrigger("DoubleJump");
            Debug.Log("[Branduci] Double Jump");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CROUCH
    // ─────────────────────────────────────────────────────────────────────────

    void HandleCrouch()
    {
        bool hold = (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) && isGrounded;
        if (hold && !isCrouching)       { isCrouching = true;  SetAnimBool("IsCrouching", true);  Debug.Log("[Branduci] Crouch"); }
        else if (!hold && isCrouching)  { isCrouching = false; SetAnimBool("IsCrouching", false); }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // FREEFALL
    // ─────────────────────────────────────────────────────────────────────────

    void HandleFreefall()
    {
        if (inputLocked) return;
        bool falling = !isGrounded && rb.linearVelocity.y < 0f;
        if (falling && !wasFalling) Debug.Log("[Branduci] Freefall");
        wasFalling      = falling;
        rb.gravityScale = falling ? freefallGravity : normalGravityScale;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // M1 — ATTACK (context-sensitive)
    // Ground neutral: 3-hit combo  |  W+click: Up  |  S+click: Down
    // Air: Aerial Neutral / Up / Down
    // ─────────────────────────────────────────────────────────────────────────

    void HandleAttack()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (comboLockTimer > 0f) return;

        bool up   = Input.GetKey(KeyCode.W)   || Input.GetKey(KeyCode.UpArrow);
        bool down = Input.GetKey(KeyCode.S)   || Input.GetKey(KeyCode.DownArrow);

        if (!isGrounded)
        {
            if      (up)   AerialUp();
            else if (down) AerialDown();
            else           AerialNeutral();
            return;
        }

        if (up)   { GroundUpAttack();   return; }
        if (down) { GroundDownAttack(); return; }

        AdvanceCombo();
    }

    void AdvanceCombo()
    {
        if (comboTimer <= 0f) comboStep = 0;
        comboStep++;
        comboTimer     = comboWindow;
        comboLockTimer = 0.18f;

        Vector2 dir = isFacingRight ? Vector2.right : Vector2.left;

        switch (comboStep)
        {
            case 1:
                HitInFront(dir, comboRange * 0.8f, comboStep1Dmg, dir * 4f, "Jab");
                break;
            case 2:
                HitInFront(dir, comboRange, comboStep2Dmg, dir * 5f, "Mic Swing");
                break;
            case 3:
                HitInFront(dir, comboRange * 1.1f, comboStep3Dmg, dir * 6f + Vector2.up * 2f, "Trip Kick");
                comboStep  = 0;
                comboTimer = 0f;
                break;
        }
        StartCoroutine(FlashColor(ColorCombo, 0.07f));
    }

    void GroundUpAttack()
    {
        comboLockTimer = 0.25f;
        Collider2D[] hits = OverlapEnemies((Vector2)transform.position + Vector2.up * 0.8f, comboRange * 0.8f);
        foreach (var h in hits)
            h.GetComponent<EnemyDummy>()?.TakeDamage(upAtkDamage, Vector2.up * 7f);
        Debug.Log($"[Branduci] Up Attack (Mic Thrust) — {hits.Length} hit(s)");
        StartCoroutine(FlashColor(ColorCombo, 0.07f));
    }

    void GroundDownAttack()
    {
        comboLockTimer = 0.25f;
        Vector2 slide = isFacingRight ? Vector2.right : Vector2.left;
        Collider2D[] hits = OverlapEnemies(transform.position, comboRange);
        foreach (var h in hits)
            h.GetComponent<EnemyDummy>()?.TakeDamage(downAtkDamage, slide * 4f + Vector2.down * 2f);
        Debug.Log($"[Branduci] Down Attack (Trip Kick) — {hits.Length} hit(s)");
        StartCoroutine(FlashColor(ColorCombo, 0.07f));
    }

    void AerialNeutral()
    {
        comboLockTimer = 0.2f;
        Vector2 dir = isFacingRight ? Vector2.right : Vector2.left;
        Collider2D[] hits = OverlapEnemies(transform.position, comboRange);
        foreach (var h in hits)
            h.GetComponent<EnemyDummy>()?.TakeDamage(aerialNeutDmg, dir * 5f);
        Debug.Log($"[Branduci] Aerial Neutral (Spinning Mic) — {hits.Length} hit(s)");
        StartCoroutine(FlashColor(ColorCombo, 0.07f));
    }

    void AerialUp()
    {
        comboLockTimer = 0.22f;
        Collider2D[] hits = OverlapEnemies((Vector2)transform.position + Vector2.up * 0.6f, comboRange * 0.9f);
        foreach (var h in hits)
            h.GetComponent<EnemyDummy>()?.TakeDamage(upAtkDamage, Vector2.up * 9f);
        Debug.Log($"[Branduci] Aerial Up (Kick) — {hits.Length} hit(s)");
        StartCoroutine(FlashColor(ColorCombo, 0.07f));
    }

    void AerialDown()
    {
        comboLockTimer = 0.22f;
        Collider2D[] hits = OverlapEnemies((Vector2)transform.position + Vector2.down * 0.5f, comboRange * 0.8f);
        foreach (var h in hits)
            h.GetComponent<EnemyDummy>()?.TakeDamage(aerialDownDmg, Vector2.down * 10f);
        Debug.Log($"[Branduci] Aerial Down (Mic Spike) — {hits.Length} hit(s)");
        StartCoroutine(FlashColor(ColorCombo, 0.07f));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // M2 — SMASH ATTACK
    // ─────────────────────────────────────────────────────────────────────────

    void HandleSmash()
    {
        if (!Input.GetMouseButtonDown(1) || smashTimer > 0f) return;
        smashTimer = smashCooldown;

        Vector2      dir   = isFacingRight ? Vector2.right : Vector2.left;
        Collider2D[] hits  = OverlapEnemies(transform.position, smashRange);
        int count = 0;
        foreach (var hit in hits)
        {
            Vector2 toTarget = ((Vector2)hit.transform.position - (Vector2)transform.position).normalized;
            if (Vector2.Angle(dir, toTarget) <= smashWedge * 0.5f)
            {
                hit.GetComponent<EnemyDummy>()?.TakeDamage(smashDamage, toTarget * 10f);
                count++;
            }
        }
        Debug.Log($"[Branduci] Smash (Wide Mic Arc) — {count} hit(s)");
        StartCoroutine(FlashColor(ColorSmash, 0.12f));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Q — FREESTYLE
    // ─────────────────────────────────────────────────────────────────────────

    void HandleFreestyle()
    {
        if (!Input.GetKeyDown(KeyCode.Q)) return;
        if (freestyleCooldownTimer > 0f) { Debug.Log($"[Branduci] Freestyle cooldown — {freestyleCooldownTimer:0.0}s"); return; }
        StartCoroutine(FreestyleCoroutine());
    }

    IEnumerator FreestyleCoroutine()
    {
        freestyleCooldownTimer = freestyleCooldown;
        inputLocked            = true;

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        SetColor(ColorFreestyle);
        Debug.Log("[Branduci] Freestyle — dropping bars");

        yield return new WaitForSeconds(freestyleWindup);

        Collider2D[] hits = OverlapEnemies(transform.position, freestyleRadius);
        foreach (var hit in hits)
        {
            Vector2 knock = ((Vector2)hit.transform.position - (Vector2)transform.position).normalized * 8f;
            hit.GetComponent<EnemyDummy>()?.TakeDamage(freestyleDamage, knock);
        }
        Debug.Log($"[Branduci] Freestyle — {hits.Length} hit(s) in radius {freestyleRadius}");
        StartCoroutine(FlashColor(ColorFreestyle, 0.25f));

        inputLocked = false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // E — BOOMBOX DROP
    // ─────────────────────────────────────────────────────────────────────────

    void HandleBoomboxDrop()
    {
        if (!Input.GetKeyDown(KeyCode.E)) return;
        if (boomboxCooldownTimer > 0f) { Debug.Log($"[Branduci] Boombox cooldown — {boomboxCooldownTimer:0.0}s"); return; }
        StartCoroutine(BoomboxCoroutine());
    }

    IEnumerator BoomboxCoroutine()
    {
        boomboxCooldownTimer = boomboxCooldown;
        SetColor(ColorBoombox);
        Debug.Log("[Branduci] Boombox Drop!");

        yield return new WaitForSeconds(0.15f);

        Collider2D[] hits = OverlapEnemies(transform.position, boomboxRadius);
        foreach (var hit in hits)
        {
            EnemyDummy enemy = hit.GetComponent<EnemyDummy>();
            if (enemy == null) continue;
            Vector2 knock = ((Vector2)hit.transform.position - (Vector2)transform.position).normalized * 3f;
            enemy.TakeDamage(boomboxDamage, knock);
            Debug.Log($"[Branduci] Boombox — stunned '{hit.name}' ({boomboxStunTime}s dance)");
        }
        StartCoroutine(FlashColor(ColorBoombox, 0.2f));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // R — SAUCE SPILL
    // ─────────────────────────────────────────────────────────────────────────

    void HandleSauceSpill()
    {
        if (!Input.GetKeyDown(KeyCode.R)) return;
        if (sauceCooldownTimer > 0f) { Debug.Log($"[Branduci] Sauce Spill cooldown — {sauceCooldownTimer:0.0}s"); return; }
        sauceCooldownTimer = sauceCooldown;
        SpawnSauceZone(transform.position, sauceRadius, sauceDuration);
        Debug.Log($"[Branduci] Sauce Spill — zone {sauceRadius}r active for {sauceDuration}s");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // T — BIG ARCH SAUCE
    // ─────────────────────────────────────────────────────────────────────────

    void HandleBigArchSauce()
    {
        if (!Input.GetKeyDown(KeyCode.T)) return;
        if (bigSauceCooldownTimer > 0f) { Debug.Log($"[Branduci] Big Arch Sauce cooldown — {bigSauceCooldownTimer:0.0}s"); return; }
        bigSauceCooldownTimer = bigSauceCooldown;
        SpawnSauceZone(transform.position, bigSauceRadius, bigSauceDuration);
        StartCoroutine(FlashColor(ColorSauce, 0.25f));
        Debug.Log($"[Branduci] Big Arch Sauce — wide splatter {bigSauceRadius}r!");
    }

    void SpawnSauceZone(Vector2 position, float radius, float duration)
    {
        GameObject zone    = new GameObject("SauceZone");
        zone.transform.position = new Vector3(position.x, position.y, 0f);

        CircleCollider2D zoneCol = zone.AddComponent<CircleCollider2D>();
        zoneCol.radius    = radius;
        zoneCol.isTrigger = true;

        SauceZone sz = zone.AddComponent<SauceZone>();
        sz.Init(this, duration);

        activeSauceZones.Add(zone);
        StartCoroutine(ExpireSauceZone(zone, duration));
    }

    IEnumerator ExpireSauceZone(GameObject zone, float duration)
    {
        yield return new WaitForSeconds(duration);
        if (zone != null)
        {
            activeSauceZones.Remove(zone);
            Destroy(zone);
            if (activeSauceZones.Count == 0) inOwnSauce = false;
            Debug.Log("[Branduci] Sauce zone expired");
        }
    }

    public void SetInOwnSauce(bool value)
    {
        inOwnSauce = value;
        Debug.Log($"[Branduci] In own sauce: {value} — speed boost {(value ? "ON" : "OFF")}");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // F — CHART TOPPER  (Ult)
    // ─────────────────────────────────────────────────────────────────────────

    void HandleChartTopper()
    {
        if (!Input.GetKeyDown(KeyCode.F)) return;
        if (chartCooldownTimer > 0f) { Debug.Log($"[Branduci] Chart Topper cooldown — {chartCooldownTimer:0.0}s"); return; }
        StartCoroutine(ChartTopperCoroutine());
    }

    IEnumerator ChartTopperCoroutine()
    {
        chartCooldownTimer = chartCooldown;
        inputLocked        = true;

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        SetColor(ColorUlt);
        Debug.Log("[Branduci] Chart Topper — INCOMING");

        yield return new WaitForSeconds(chartWindup);

        Collider2D[] hits = OverlapEnemies(transform.position, chartRadius);
        foreach (var hit in hits)
        {
            EnemyDummy enemy = hit.GetComponent<EnemyDummy>();
            if (enemy == null) continue;
            enemy.TakeDamage(0f, Vector2.zero); // zero damage — debuff only
            Debug.Log($"[Branduci] Chart Topper — '{hit.name}' DEAFENED for {chartDuration}s");
        }
        Debug.Log($"[Branduci] Chart Topper — {hits.Length} enemies deafened");

        inputLocked = false;

        // Stay magenta for full deafen duration as visual indicator
        yield return new WaitForSeconds(chartDuration);
        RestoreColor();
        Debug.Log("[Branduci] Chart Topper — Deafen expired");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────────────────────

    Collider2D[] OverlapEnemies(Vector2 origin, float radius)
    {
        if (enemyLayer.value != 0)
            return Physics2D.OverlapCircleAll(origin, radius, enemyLayer);

        Collider2D[] all    = Physics2D.OverlapCircleAll(origin, radius);
        var          result = new List<Collider2D>();
        foreach (var c in all)
            if (c.gameObject != gameObject && c.GetComponent<EnemyDummy>() != null)
                result.Add(c);
        return result.ToArray();
    }

    void HitInFront(Vector2 dir, float range, float damage, Vector2 knockback, string moveName)
    {
        Vector2      origin = (Vector2)transform.position + dir * (range * 0.5f);
        Collider2D[] hits   = OverlapEnemies(origin, range * 0.5f);
        foreach (var hit in hits)
            hit.GetComponent<EnemyDummy>()?.TakeDamage(damage, knockback);
        Debug.Log($"[Branduci] {moveName} — {hits.Length} hit(s)");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ANIMATOR
    // ─────────────────────────────────────────────────────────────────────────

    void UpdateAnimator()
    {
        if (animator == null) return;
        SetAnimFloat("Speed",            Mathf.Abs(rb.linearVelocity.x));
        SetAnimBool ("IsGrounded",       isGrounded);
        SetAnimFloat("VerticalVelocity", rb.linearVelocity.y);
    }

    void SetAnimFloat(string p, float v)  { foreach (var x in animator.parameters) if (x.name == p) { animator.SetFloat(p, v);   return; } }
    void SetAnimBool (string p, bool v)   { foreach (var x in animator.parameters) if (x.name == p) { animator.SetBool(p, v);    return; } }
    void SetAnimTrigger(string p)         { foreach (var x in animator.parameters) if (x.name == p) { animator.SetTrigger(p);    return; } }

    // ─────────────────────────────────────────────────────────────────────────
    // VISUAL CUES
    // ─────────────────────────────────────────────────────────────────────────

    void SetColor(Color c)   { if (sr != null) sr.color = c; }
    void RestoreColor()      { if (sr != null) sr.color = originalColor; }

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

        // Freestyle burst radius — gold, shows briefly after use
        if (freestyleCooldownTimer > freestyleCooldown - 0.6f)
        {
            Gizmos.color = new Color(1f, 0.85f, 0f, 0.45f);
            Gizmos.DrawWireSphere(transform.position, freestyleRadius);
        }

        // Boombox radius — blue
        if (boomboxCooldownTimer > boomboxCooldown - 0.5f)
        {
            Gizmos.color = new Color(0.2f, 0.5f, 1f, 0.45f);
            Gizmos.DrawWireSphere(transform.position, boomboxRadius);
        }

        // Smash arc — orange while on cooldown
        if (smashTimer > 0f)
        {
            float alpha = Mathf.Clamp01(smashTimer / smashCooldown);
            Gizmos.color = new Color(1f, 0.6f, 0.1f, alpha * 0.6f);
            Gizmos.DrawWireSphere(transform.position, smashRange);
        }

        // Chart Topper radius — magenta
        if (chartCooldownTimer > chartCooldown - 0.8f)
        {
            Gizmos.color = new Color(0.9f, 0.1f, 0.9f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, chartRadius);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (col == null) col = GetComponent<BoxCollider2D>();
        if (col == null) return;
        Bounds  b   = col.bounds;
        Vector3 org = new Vector3(b.center.x, b.min.y - 0.09f, 0f);
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireCube(org, new Vector3(b.size.x * 0.8f, 0.15f, 0f));
    }
}
