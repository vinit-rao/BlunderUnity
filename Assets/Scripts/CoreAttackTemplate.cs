// TEMPLATE SCRIPT — CoreAttackTemplate.cs — Do not attach directly. Use as a base reference for character-specific attack scripts.
// NOTE: This script is intentionally non-functional. All attack logic is stubbed with TODOs.
// To create a character: copy this file, rename the class, fill in each TODO, and tune frame data in the Inspector.

using UnityEngine;

/// <summary>
/// Base attack template for all BlunderUnity roster characters.
/// Attach alongside a character-specific CoreMovementTemplate subclass.
/// Reads movement state (grounded, dashing, crouching) via the movement reference.
/// </summary>
public class CoreAttackTemplate : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    // MOVEMENT REFERENCE
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Movement Reference")]
    [SerializeField] private CoreMovementTemplate movement;
    // Used to read isGrounded, isCrouching, isDashing, isFacingRight, etc.
    // Assign in the Inspector — drag the GameObject that holds the movement script.

    // ─────────────────────────────────────────────────────────────────────────
    // HITBOX SETTINGS
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Hitbox Settings")]
    [SerializeField] private float     attackRange = 1.5f;  // radius of the OverlapCircle
    [SerializeField] private float     attackWedge = 80f;   // cone width in degrees for directional attacks
    [SerializeField] private LayerMask enemyLayer;          // layer(s) the hitbox can hit

    // ─────────────────────────────────────────────────────────────────────────
    // KNOCKBACK
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Knockback")]
    [SerializeField] private float baseKnockback    = 5f;   // flat value added to every hit
    [SerializeField] private float knockbackScaling = 0.1f; // multiplied against target's damage %
    [SerializeField] private float damagePercent    = 0f;   // this character's current damage (resets on stock loss)

    // ─────────────────────────────────────────────────────────────────────────
    // NEUTRAL ATTACK
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Neutral Attack")]
    [SerializeField] private int naStartupFrames = 3;
    [SerializeField] private int naActiveFrames  = 4;
    [SerializeField] private int naEndlagFrames  = 8;

    // ─────────────────────────────────────────────────────────────────────────
    // FORWARD ATTACK
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Forward Attack")]
    [SerializeField] private int fAttackStartupFrames = 5;
    [SerializeField] private int fAttackActiveFrames  = 4;
    [SerializeField] private int fAttackEndlagFrames  = 14;

    // ─────────────────────────────────────────────────────────────────────────
    // UP ATTACK
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Up Attack")]
    [SerializeField] private int uAttackStartupFrames = 5;
    [SerializeField] private int uAttackActiveFrames  = 6;
    [SerializeField] private int uAttackEndlagFrames  = 12;

    // ─────────────────────────────────────────────────────────────────────────
    // DOWN ATTACK
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Down Attack")]
    [SerializeField] private int dAttackStartupFrames = 6;
    [SerializeField] private int dAttackActiveFrames  = 4;
    [SerializeField] private int dAttackEndlagFrames  = 16;

    // ─────────────────────────────────────────────────────────────────────────
    // AERIAL NEUTRAL
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Aerial Neutral")]
    // NOTE: All aerial attacks must check !movement.isGrounded before executing.
    [SerializeField] private int nAirStartupFrames = 4;
    [SerializeField] private int nAirActiveFrames  = 6;
    [SerializeField] private int nAirEndlagFrames  = 10;

    // ─────────────────────────────────────────────────────────────────────────
    // AERIAL FORWARD
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Aerial Forward")]
    // NOTE: All aerial attacks must check !movement.isGrounded before executing.
    [SerializeField] private int fAirStartupFrames = 7;
    [SerializeField] private int fAirActiveFrames  = 3;
    [SerializeField] private int fAirEndlagFrames  = 18;

    // ─────────────────────────────────────────────────────────────────────────
    // AERIAL UP
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Aerial Up")]
    // NOTE: All aerial attacks must check !movement.isGrounded before executing.
    [SerializeField] private int uAirStartupFrames = 6;
    [SerializeField] private int uAirActiveFrames  = 5;
    [SerializeField] private int uAirEndlagFrames  = 14;

    // ─────────────────────────────────────────────────────────────────────────
    // AERIAL DOWN
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Aerial Down")]
    // NOTE: All aerial attacks must check !movement.isGrounded before executing.
    [SerializeField] private int dAirStartupFrames = 8;
    [SerializeField] private int dAirActiveFrames  = 4;
    [SerializeField] private int dAirEndlagFrames  = 20;

    // ─────────────────────────────────────────────────────────────────────────
    // DASH ATTACK
    // Triggered by OnDashAttack() called from CoreMovementTemplate when the
    // player inputs an attack during a dash cancel window.
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Dash Attack")]
    [SerializeField] private int dashAtkStartupFrames = 4;
    [SerializeField] private int dashAtkActiveFrames  = 5;
    [SerializeField] private int dashAtkEndlagFrames  = 18;

    // ─────────────────────────────────────────────────────────────────────────
    // SMASH ATTACK  (chargeable — hold attack + direction)
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Smash Attack")]
    [SerializeField] private int   smashStartupFrames = 10;
    [SerializeField] private int   smashActiveFrames  = 4;
    [SerializeField] private int   smashEndlagFrames  = 28;
    [SerializeField] private float smashMinCharge     = 0f;   // seconds — no charge
    [SerializeField] private float smashMaxCharge     = 1.5f; // seconds — full charge
    [SerializeField] private float smashChargeMultiplier = 2f; // damage/knockback at full charge

    // ─────────────────────────────────────────────────────────────────────────
    // MAIN ATTACK  (character unique)
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Main Attack (Character Unique)")]
    [SerializeField] private int mainAtkStartupFrames = 6;
    [SerializeField] private int mainAtkActiveFrames  = 6;
    [SerializeField] private int mainAtkEndlagFrames  = 16;

    // ─────────────────────────────────────────────────────────────────────────
    // ABILITY 1  (character unique)
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Ability 1 (Character Unique)")]
    [SerializeField] private int   ab1StartupFrames = 8;
    [SerializeField] private int   ab1ActiveFrames  = 6;
    [SerializeField] private int   ab1EndlagFrames  = 20;
    [SerializeField] private float ab1Cooldown      = 5f;

    // ─────────────────────────────────────────────────────────────────────────
    // ABILITY 2  (character unique)
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Ability 2 (Character Unique)")]
    [SerializeField] private int   ab2StartupFrames = 10;
    [SerializeField] private int   ab2ActiveFrames  = 4;
    [SerializeField] private int   ab2EndlagFrames  = 24;
    [SerializeField] private float ab2Cooldown      = 8f;

    // ─────────────────────────────────────────────────────────────────────────
    // GADGET  (character unique)
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Gadget (Character Unique)")]
    [SerializeField] private int   gadgetStartupFrames = 12;
    [SerializeField] private int   gadgetActiveFrames  = 8;
    [SerializeField] private int   gadgetEndlagFrames  = 20;
    [SerializeField] private float gadgetCooldown      = 10f;

    // ─────────────────────────────────────────────────────────────────────────
    // ULT  (character unique)
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Ult (Character Unique)")]
    [SerializeField] private int   ultStartupFrames = 20;
    [SerializeField] private int   ultActiveFrames  = 30;
    [SerializeField] private int   ultEndlagFrames  = 40;
    [SerializeField] private float ultCooldown      = 30f;

    // ─────────────────────────────────────────────────────────────────────────
    // PRIVATE STATE
    // ─────────────────────────────────────────────────────────────────────────

    private float attackTimer;      // general cooldown between attacks
    private float smashChargeTimer; // tracks how long smash is held
    private bool  isChargingSmash;
    private float ab1CooldownTimer;
    private float ab2CooldownTimer;
    private float gadgetCooldownTimer;
    private float ultCooldownTimer;

    // ─────────────────────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Cache references and reset state.</summary>
    void Start()
    {
        // TODO: validate that movement reference is assigned; warn if null
        damagePercent = 0f;
    }

    /// <summary>Tick cooldowns and poll attack inputs each frame.</summary>
    void Update()
    {
        attackTimer         -= Time.deltaTime;
        ab1CooldownTimer    -= Time.deltaTime;
        ab2CooldownTimer    -= Time.deltaTime;
        gadgetCooldownTimer -= Time.deltaTime;
        ultCooldownTimer    -= Time.deltaTime;

        HandleAttackInput();
        HandleSmashCharge();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // INPUT ROUTING
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Reads attack inputs each frame and routes to the correct attack method.
    /// Check movement state here before calling aerial vs grounded variants.
    /// </summary>
    void HandleAttackInput()
    {
        // TODO: read attack button input (e.g. Mouse0, gamepad face button)
        // TODO: read directional input to choose Forward / Up / Down variants
        // TODO: if movement.isGrounded  → route to grounded attacks
        // TODO: if !movement.isGrounded → route to aerial attacks
        // TODO: if attackTimer > 0 → block new attacks (endlag)
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GROUNDED ATTACKS
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Quick jab. No directional input held.</summary>
    void NeutralAttack()
    {
        // TODO: run startup frames, activate hitbox for activeFrames, then endlag
        // TODO: call SpawnHitbox(Vector2.right or left based on facing, naActiveFrames)
        // TODO: animator.SetTrigger("NeutralAttack")
        // TODO: Debug.Log("[Attack] Neutral")
    }

    /// <summary>Horizontal attack in the direction the character is facing.</summary>
    void ForwardAttack()
    {
        // TODO: SpawnHitbox(facing direction, fAttackActiveFrames)
        // TODO: animator.SetTrigger("ForwardAttack")
        // TODO: Debug.Log("[Attack] Forward")
    }

    /// <summary>Upward attack. Can hit opponents above the character.</summary>
    void UpAttack()
    {
        // TODO: SpawnHitbox(Vector2.up, uAttackActiveFrames)
        // TODO: animator.SetTrigger("UpAttack")
        // TODO: Debug.Log("[Attack] Up")
    }

    /// <summary>Low attack. Can hit crouching opponents or trip them.</summary>
    void DownAttack()
    {
        // TODO: SpawnHitbox(Vector2.down, dAttackActiveFrames)
        // TODO: animator.SetTrigger("DownAttack")
        // TODO: Debug.Log("[Attack] Down")
    }

    // ─────────────────────────────────────────────────────────────────────────
    // AERIAL ATTACKS
    // All aerials must confirm !movement.isGrounded before executing.
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Aerial neutral — hits in all directions around the character.</summary>
    void AerialNeutral()
    {
        // NOTE: check !movement.isGrounded before executing.
        // TODO: SpawnHitbox(Vector2.zero /*omnidirectional*/, nAirActiveFrames)
        // TODO: animator.SetTrigger("AerialNeutral")
        // TODO: Debug.Log("[Attack] Aerial Neutral")
    }

    /// <summary>Aerial forward — horizontal hit in the facing direction.</summary>
    void AerialForward()
    {
        // NOTE: check !movement.isGrounded before executing.
        // TODO: SpawnHitbox(facing direction, fAirActiveFrames)
        // TODO: animator.SetTrigger("AerialForward")
        // TODO: Debug.Log("[Attack] Aerial Forward")
    }

    /// <summary>Aerial up — launches opponents upward. Common juggle tool.</summary>
    void AerialUp()
    {
        // NOTE: check !movement.isGrounded before executing.
        // TODO: SpawnHitbox(Vector2.up, uAirActiveFrames)
        // TODO: animator.SetTrigger("AerialUp")
        // TODO: Debug.Log("[Attack] Aerial Up")
    }

    /// <summary>Aerial down — spikes opponents below. High risk, high reward.</summary>
    void AerialDown()
    {
        // NOTE: check !movement.isGrounded before executing.
        // TODO: SpawnHitbox(Vector2.down, dAirActiveFrames)
        // TODO: animator.SetTrigger("AerialDown")
        // TODO: Debug.Log("[Attack] Aerial Down")
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DASH ATTACK
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by CoreMovementTemplate when an attack input is detected
    /// during the dash cancel window. Do not call directly from input.
    /// </summary>
    public void OnDashAttack()
    {
        // TODO: SpawnHitbox(facing direction, dashAtkActiveFrames)
        // TODO: animator.SetTrigger("DashAttack")
        // TODO: apply extra horizontal force to hitbox origin for lunge feel
        Debug.Log("[Attack] Dash Attack");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SMASH ATTACK
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Tracks how long the smash button is held to build charge.</summary>
    void HandleSmashCharge()
    {
        // TODO: on smash input down → isChargingSmash = true, smashChargeTimer = 0
        // TODO: while held → smashChargeTimer += Time.deltaTime (clamp to smashMaxCharge)
        // TODO: on smash input up → ReleaseSmash()
    }

    /// <summary>
    /// Releases the smash. Damage and knockback scale with smashChargeTimer.
    /// Full charge applies smashChargeMultiplier to both values.
    /// </summary>
    void ReleaseSmash()
    {
        // TODO: float chargeRatio = Mathf.Clamp01(smashChargeTimer / smashMaxCharge)
        // TODO: float chargedKnockback = baseKnockback * Mathf.Lerp(1f, smashChargeMultiplier, chargeRatio)
        // TODO: SpawnHitbox(facing direction, smashActiveFrames, overrideKnockback: chargedKnockback)
        // TODO: animator.SetTrigger("SmashAttack")
        // TODO: isChargingSmash = false
        Debug.Log("[Attack] Smash Released");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CHARACTER-UNIQUE ATTACKS
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Character's signature main attack. Implement per character.</summary>
    void MainAttack()
    {
        // TODO: implement character-specific behaviour
        // TODO: SpawnHitbox(direction, mainAtkActiveFrames)
        // TODO: animator.SetTrigger("MainAttack")
        Debug.Log("[Attack] Main");
    }

    /// <summary>First unique ability. Check ab1CooldownTimer &lt;= 0 before firing.</summary>
    void Ability1()
    {
        // TODO: if ab1CooldownTimer > 0 return
        // TODO: ab1CooldownTimer = ab1Cooldown
        // TODO: implement character-specific behaviour
        Debug.Log("[Attack] Ability 1");
    }

    /// <summary>Second unique ability. Check ab2CooldownTimer &lt;= 0 before firing.</summary>
    void Ability2()
    {
        // TODO: if ab2CooldownTimer > 0 return
        // TODO: ab2CooldownTimer = ab2Cooldown
        // TODO: implement character-specific behaviour
        Debug.Log("[Attack] Ability 2");
    }

    /// <summary>Gadget attack. Check gadgetCooldownTimer &lt;= 0 before firing.</summary>
    void Gadget()
    {
        // TODO: if gadgetCooldownTimer > 0 return
        // TODO: gadgetCooldownTimer = gadgetCooldown
        // TODO: implement character-specific behaviour (projectile, trap, etc.)
        Debug.Log("[Attack] Gadget");
    }

    /// <summary>Ultimate attack. Check ultCooldownTimer &lt;= 0 before firing.</summary>
    void Ult()
    {
        // TODO: if ultCooldownTimer > 0 return
        // TODO: ultCooldownTimer = ultCooldown
        // TODO: implement character-specific behaviour
        // TODO: animator.SetTrigger("Ult")
        Debug.Log("[Attack] Ult");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HITBOX
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Spawns a hitbox in the given direction using Physics2D.OverlapCircleAll.
    /// Filters hits inside the attack wedge cone, then applies damage and knockback.
    /// Pass Vector2.zero as direction for omnidirectional attacks (aerial neutral).
    /// </summary>
    void SpawnHitbox(Vector2 direction, int activeFrames, float overrideKnockback = -1f)
    {
        Vector2 origin = (Vector2)transform.position + direction * (attackRange * 0.5f);
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, attackRange, enemyLayer);

        foreach (var hit in hits)
        {
            // Wedge filter — skip for omnidirectional (direction == zero)
            if (direction != Vector2.zero)
            {
                Vector2 toTarget = ((Vector2)hit.transform.position - (Vector2)transform.position).normalized;
                if (Vector2.Angle(direction, toTarget) > attackWedge * 0.5f) continue;
            }

            float kb = overrideKnockback >= 0f ? overrideKnockback : baseKnockback;

            // TODO: hit.GetComponent<CoreAttackTemplate>()?.ReceiveHit(kb, knockbackScaling, direction)
            Debug.Log($"[Attack] Hit '{hit.name}'");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // KNOCKBACK & HITSTUN
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Calculates outgoing knockback using the GDD formula:
    /// knockback = baseKnockback + (targetDamagePercent × knockbackScaling)
    /// </summary>
    float CalculateKnockback(float targetDamagePercent, float kb, float scaling)
    {
        // TODO: return kb + (targetDamagePercent * scaling)
        return 0f;
    }

    /// <summary>
    /// Called on this character when struck. Increases damagePercent,
    /// calculates knockback received, and triggers hitstun.
    /// </summary>
    public void ReceiveHit(float incomingKnockback, float incomingScaling, Vector2 hitDirection)
    {
        // TODO: damagePercent += some damage value from the attacker
        // TODO: float kb = CalculateKnockback(damagePercent, incomingKnockback, incomingScaling)
        // TODO: apply kb as a force to this character's Rigidbody2D in hitDirection
        // TODO: ApplyHitstun(kb)
        Debug.Log($"[Hit] Received hit — damage% now {damagePercent}");
    }

    /// <summary>
    /// Applies hitstun proportional to the knockback received.
    /// Hitstun prevents the character from acting until it expires.
    /// </summary>
    void ApplyHitstun(float knockback)
    {
        // TODO: float hitstunDuration = knockback * someConstant (tune per GDD)
        // TODO: start a coroutine or timer that sets isInHitstun = true for hitstunDuration
        // TODO: while isInHitstun, block all input in HandleAttackInput() and CoreMovementTemplate
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GIZMOS
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Visualises attack range and the last active hitbox wedge in the Scene view.</summary>
    void OnDrawGizmosSelected()
    {
        // Outer attack range circle
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // TODO: draw active hitbox wedge when an attack is in its activeFrames window
        // TODO: draw cooldown indicators for ab1, ab2, gadget, ult (e.g. coloured dots above head)
        // TODO: draw smash charge bar when isChargingSmash
    }
}
