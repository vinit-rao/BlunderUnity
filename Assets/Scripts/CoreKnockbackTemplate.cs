// TEMPLATE SCRIPT — CoreKnockbackTemplate.cs — Do not attach directly. Use as a base reference for character-specific scripts.
// NOTE: This script is intentionally non-functional. All logic is stubbed with TODOs.
// Attach alongside CoreMovementTemplate and CoreAttackTemplate on the same GameObject.

using System.Collections;
using UnityEngine;

/// <summary>
/// Handles all incoming damage, knockback calculation, hitstun, and blastzone elimination
/// for a single character. The attacker calls TakeDamage() on this component.
/// WEIGHT stat from CoreCharacterStats scales knockback received.
/// </summary>
public class CoreKnockbackTemplate : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    // REFERENCES
    // ─────────────────────────────────────────────────────────────────────────

    [Header("References")]
    [SerializeField] private CoreCharacterStats stats;
    // TODO: assign in Inspector — drag this character's CoreCharacterStats asset

    private Rigidbody2D rb;

    // ─────────────────────────────────────────────────────────────────────────
    // DAMAGE
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Damage")]
    [SerializeField] public float damagePercent = 0f;
    // Starts at 0. Increases each time TakeDamage() is called.
    // Higher damage % = more knockback received (see CalculateKnockback).
    // Resets to 0 on stock loss (in OnBlastzone).

    // ─────────────────────────────────────────────────────────────────────────
    // HITSTUN
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Hitstun")]
    [SerializeField] private float hitstunMultiplier = 0.4f;
    // Seconds of hitstun per unit of knockback. Tune per GDD.

    private bool isInHitstun;

    // ─────────────────────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Cache components.</summary>
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        // TODO: validate that stats reference is assigned
    }

    void Update()
    {
        // TODO: expose isInHitstun to CoreMovementTemplate and CoreAttackTemplate
        //       so inputs are blocked during hitstun
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DAMAGE ENTRY POINT
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by the attacker's CoreAttackTemplate when a hitbox connects.
    /// Increases damagePercent, calculates knockback, and triggers hitstun.
    /// </summary>
    /// <param name="amount">Raw damage dealt by the attack.</param>
    /// <param name="direction">Normalised world-space direction of the hit.</param>
    /// <param name="baseKnockback">Flat knockback from the attack.</param>
    /// <param name="knockbackScaling">Per-attack scaling factor.</param>
    public void TakeDamage(float amount, Vector2 direction, float baseKnockback, float knockbackScaling)
    {
        // TODO: damagePercent += amount * (DEF modifier from stats — low DEF = more damage taken)
        // TODO: float kb = CalculateKnockback(baseKnockback, knockbackScaling)
        // TODO: apply kb force to rb in direction
        // TODO: ApplyHitstun(kb)
        // TODO: animator.SetTrigger("Hurt")
        Debug.Log($"[Knockback] {name} hit — damage% now {damagePercent}");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // KNOCKBACK FORMULA
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// GDD formula: knockback = baseKnockback + (damagePercent × scaling)
    /// WEIGHT stat reduces the result (heavier characters travel less far).
    /// </summary>
    float CalculateKnockback(float baseKnockback, float scaling)
    {
        // TODO: float raw = baseKnockback + (damagePercent * scaling)
        // TODO: float weightModifier = Mathf.Lerp(1.2f, 0.6f, (stats.WEIGHT - 1f) / 9f)
        //       (low WEIGHT = flies further, high WEIGHT = stays grounded)
        // TODO: return raw * weightModifier
        return 0f;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HITSTUN
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Prevents the character from acting for a duration proportional to knockback.
    /// Hitstun = knockback × hitstunMultiplier seconds.
    /// </summary>
    void ApplyHitstun(float knockback)
    {
        // TODO: StopCoroutine(HitstunCoroutine()) if already running
        // TODO: StartCoroutine(HitstunCoroutine(knockback * hitstunMultiplier))
    }

    /// <summary>Sets isInHitstun for the calculated duration then clears it.</summary>
    IEnumerator HitstunCoroutine(float duration)
    {
        // TODO: isInHitstun = true
        // TODO: animator.SetBool("InHitstun", true)
        // TODO: yield return new WaitForSeconds(duration)
        // TODO: isInHitstun = false
        // TODO: animator.SetBool("InHitstun", false)
        yield return null;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // BLASTZONE
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by a trigger collider on each blastzone boundary.
    /// Handles stock loss, resets damagePercent, and respawns the character.
    /// </summary>
    /// <param name="side">Which blastzone was crossed: "Left", "Right", "Top", or "Bottom".</param>
    public void OnBlastzone(string side)
    {
        // TODO: lose one stock
        // TODO: damagePercent = 0f
        // TODO: move character to spawn point
        // TODO: play respawn animation / invincibility frames
        Debug.Log($"[Blastzone] {name} eliminated via {side} blastzone");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GIZMOS
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Draws a label showing current damagePercent above the character in Scene view.</summary>
    void OnDrawGizmosSelected()
    {
        // TODO: draw damage% as a label using Handles.Label (requires UnityEditor namespace, wrap in #if UNITY_EDITOR)
        // TODO: draw a coloured ring that shifts green → red as damagePercent increases
    }
}
