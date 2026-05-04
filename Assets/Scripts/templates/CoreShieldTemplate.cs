// TEMPLATE SCRIPT — CoreShieldTemplate.cs — Do not attach directly. Use as a base reference for character-specific scripts.
// NOTE: This script is intentionally non-functional. All logic is stubbed with TODOs.
// Attach alongside CoreMovementTemplate on the same GameObject.

#pragma warning disable CS0414
using System.Collections;
using UnityEngine;

/// <summary>
/// Handles shield, spot dodge, roll, and air dodge for a single character.
/// Shield health shrinks while held and regenerates when released.
/// Breaking the shield applies 3 seconds of stun per GDD.
/// </summary>
public class CoreShieldTemplate : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    // REFERENCES
    // ─────────────────────────────────────────────────────────────────────────

    [Header("References")]
    [SerializeField] private CoreCharacterStats stats;
    // TODO: assign in Inspector — drag this character's CoreCharacterStats asset

    private Rigidbody2D rb;

    // ─────────────────────────────────────────────────────────────────────────
    // SHIELD
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Shield")]
    [SerializeField] private float shieldMaxHealth      = 100f;
    [SerializeField] private float shieldDepletionRate  = 15f;  // health lost per second while held
    [SerializeField] private float shieldRegenRate      = 20f;  // health gained per second when released
    [SerializeField] private float shieldRegenDelay     = 1.5f; // seconds after release before regen starts
    [SerializeField] public  float shieldHealth         = 100f;

    private bool  isShielding;
    private float shieldRegenTimer;

    // ─────────────────────────────────────────────────────────────────────────
    // DODGE
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Dodge")]
    [SerializeField] private float spotDodgeDuration   = 0.3f;  // seconds of invincibility in place
    [SerializeField] private float rollDistance        = 3f;    // world units travelled during a roll
    [SerializeField] private float rollDuration        = 0.4f;  // seconds the roll takes
    [SerializeField] private float dodgeCooldown       = 0.6f;  // shared cooldown after any dodge
    [SerializeField] private float airDodgeForce       = 10f;   // impulse applied during air dodge

    // Invincibility frame counts — convert to seconds: frames / 60
    // Spot dodge:  TODO — define per GDD (stub below)
    // Roll:        TODO — define per GDD (stub below)
    // Air dodge:   TODO — define per GDD (stub below)

    private bool  isInvincible;
    private bool  hasAirDodge;     // resets on landing
    private float dodgeCooldownTimer;

    // ─────────────────────────────────────────────────────────────────────────
    // SHIELD BREAK
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Shield Break")]
    [SerializeField] private float shieldBreakStunDuration = 3f; // seconds per GDD

    private bool isShieldBroken;

    // ─────────────────────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Cache components and reset state.</summary>
    void Start()
    {
        rb          = GetComponent<Rigidbody2D>();
        shieldHealth = shieldMaxHealth;
        hasAirDodge  = true;
        // TODO: validate stats reference is assigned
    }

    /// <summary>Tick shield health and dodge cooldowns; poll input each frame.</summary>
    void Update()
    {
        dodgeCooldownTimer  -= Time.deltaTime;
        shieldRegenTimer    -= Time.deltaTime;

        HandleShieldInput();
        HandleShieldRegen();
        HandleDodgeInput();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SHIELD INPUT
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Reads shield hold input and drains or sets up regen for shield health.</summary>
    void HandleShieldInput()
    {
        // TODO: if shield button held && !isShieldBroken → isShielding = true
        // TODO: shieldHealth -= shieldDepletionRate * Time.deltaTime
        // TODO: clamp shieldHealth to 0
        // TODO: if shieldHealth <= 0 → ShieldBreak()
        // TODO: if shield button released → isShielding = false, shieldRegenTimer = shieldRegenDelay
        // TODO: animator.SetBool("IsShielding", isShielding)
    }

    /// <summary>Regenerates shield health after the regen delay has elapsed.</summary>
    void HandleShieldRegen()
    {
        // TODO: if !isShielding && !isShieldBroken && shieldRegenTimer <= 0
        //   → shieldHealth += shieldRegenRate * Time.deltaTime
        //   → clamp shieldHealth to shieldMaxHealth
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SHIELD BREAK
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Triggered when shieldHealth reaches 0.
    /// Applies 3 seconds of stun per GDD and prevents shielding until recovered.
    /// </summary>
    void ShieldBreak()
    {
        // TODO: isShieldBroken = true
        // TODO: StartCoroutine(ShieldBreakStun())
        // TODO: animator.SetTrigger("ShieldBreak")
        Debug.Log("[Shield] Shield broken!");
    }

    /// <summary>Stuns the character for shieldBreakStunDuration seconds, then resets shield.</summary>
    IEnumerator ShieldBreakStun()
    {
        // TODO: disable all input for shieldBreakStunDuration seconds
        // TODO: yield return new WaitForSeconds(shieldBreakStunDuration)
        // TODO: shieldHealth = shieldMaxHealth
        // TODO: isShieldBroken = false
        // TODO: animator.SetTrigger("ShieldRecover")
        Debug.Log("[Shield] Recovered from shield break");
        yield return null;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DODGE INPUT ROUTING
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Routes dodge input to spot dodge, roll, or air dodge based on movement state.</summary>
    void HandleDodgeInput()
    {
        // TODO: if dodgeCooldownTimer > 0 return
        // TODO: read dodge button input
        // TODO: if grounded && no directional input → HandleSpotDodge()
        // TODO: if grounded && directional input    → HandleRoll(direction)
        // TODO: if airborne && hasAirDodge          → HandleAirDodge()
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SPOT DODGE
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Brief invincibility in place with no movement.
    /// Invincibility frame count: TODO — define per GDD.
    /// </summary>
    void HandleSpotDodge()
    {
        // TODO: StartCoroutine(SpotDodgeCoroutine())
        // TODO: dodgeCooldownTimer = dodgeCooldown
        // TODO: animator.SetTrigger("SpotDodge")
        Debug.Log("[Shield] Spot Dodge");
    }

    /// <summary>Grants invincibility frames for the spot dodge duration.</summary>
    IEnumerator SpotDodgeCoroutine()
    {
        // TODO: isInvincible = true
        // TODO: yield return new WaitForSeconds(spotDodgeDuration)
        //   Frame count: TODO (e.g. ~18 frames at 60fps ≈ 0.3s — confirm in GDD)
        // TODO: isInvincible = false
        yield return null;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ROLL
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Moves the character a fixed distance in the input direction with invincibility frames.
    /// Invincibility frame count: TODO — define per GDD.
    /// </summary>
    /// <param name="direction">1f for forward roll, -1f for backward roll.</param>
    void HandleRoll(float direction)
    {
        // TODO: StartCoroutine(RollCoroutine(direction))
        // TODO: dodgeCooldownTimer = dodgeCooldown
        // TODO: animator.SetTrigger("Roll")
        Debug.Log($"[Shield] Roll — direction: {direction}");
    }

    /// <summary>Moves the character and manages invincibility over the roll duration.</summary>
    IEnumerator RollCoroutine(float direction)
    {
        // TODO: isInvincible = true
        // TODO: apply velocity or MovePosition toward direction * rollDistance over rollDuration
        //   Frame count: TODO (e.g. ~24 frames at 60fps ≈ 0.4s — confirm in GDD)
        // TODO: yield return new WaitForSeconds(rollDuration)
        // TODO: isInvincible = false
        yield return null;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // AIR DODGE
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// One-use dodge while airborne. Applies a directional impulse with invincibility frames.
    /// Resets when the character lands. Strength modified by RECOVERY stat.
    /// Invincibility frame count: TODO — define per GDD.
    /// </summary>
    void HandleAirDodge()
    {
        // TODO: if !hasAirDodge return
        // TODO: hasAirDodge = false
        // TODO: read directional input for dodge direction
        // TODO: float force = airDodgeForce * Mathf.Lerp(0.7f, 1.3f, (stats.RECOVERY - 1f) / 9f)
        // TODO: rb.linearVelocity = dodgeDirection * force
        // TODO: StartCoroutine(AirDodgeCoroutine())
        // TODO: dodgeCooldownTimer = dodgeCooldown
        // TODO: animator.SetTrigger("AirDodge")
        Debug.Log("[Shield] Air Dodge");
    }

    /// <summary>Grants and removes invincibility frames for the air dodge.</summary>
    IEnumerator AirDodgeCoroutine()
    {
        // TODO: isInvincible = true
        //   Frame count: TODO (e.g. ~25 frames at 60fps ≈ 0.4s — confirm in GDD)
        // TODO: yield return new WaitForSeconds(/* invincibility duration */)
        // TODO: isInvincible = false
        yield return null;
    }

    /// <summary>Called by CoreMovementTemplate when the character lands. Resets air dodge.</summary>
    public void OnLand()
    {
        hasAirDodge = true;
        // TODO: notify animator
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GIZMOS
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Draws shield health bar and invincibility state in the Scene view.</summary>
    void OnDrawGizmosSelected()
    {
        // TODO: draw a shrinking arc above the character representing shieldHealth / shieldMaxHealth
        // TODO: flash the character outline yellow when isInvincible
        // TODO: draw roll destination point when rolling
    }
}
