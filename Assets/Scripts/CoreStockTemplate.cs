// TEMPLATE SCRIPT — CoreStockTemplate.cs — Do not attach directly. Use as a base reference for the stock and respawn system.
// NOTE: This script is intentionally non-functional. All logic is stubbed with TODOs.
// Attach alongside CoreMovementTemplate, CoreKnockbackTemplate, and CoreShieldTemplate on the same GameObject.

using System.Collections;
using UnityEngine;

/// <summary>
/// Tracks stocks, handles respawning, and signals match start/end for a single character.
/// CoreKnockbackTemplate.OnBlastzone() should call OnStockLost() on this script.
/// Last player with stocks remaining wins per GDD.
/// isInvincible is exposed publicly — CoreKnockbackTemplate and CoreAttackTemplate
/// must check it before applying any damage.
/// </summary>
public class CoreStockTemplate : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    // REFERENCES
    // ─────────────────────────────────────────────────────────────────────────

    [Header("References")]
    [SerializeField] private CoreKnockbackTemplate knockback;
    // OnStockLost() resets knockback.damagePercent to 0 before respawn.

    [SerializeField] private CoreMovementTemplate movement;
    // RespawnPlayer() resets velocity, dashing, and crouching via this reference.

    [SerializeField] private CoreShieldTemplate shield;
    // RespawnPlayer() resets shield health to full via this reference.

    // ─────────────────────────────────────────────────────────────────────────
    // STOCK TRACKING
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Stock Tracking")]
    [SerializeField] public  int  maxStocks     = 3;   // per GDD default
    [SerializeField] public  int  currentStocks = 3;
    public  bool isEliminated = false;
    // isEliminated becomes true when currentStocks reaches 0.
    // Once true, this player takes no further part in the match.

    // ─────────────────────────────────────────────────────────────────────────
    // RESPAWN
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Respawn")]
    [SerializeField] private Transform respawnPosition;
    // Assign in Inspector — stage-specific spawn point Transform.
    // For multi-stage support, set this at runtime from a StageManager.

    [SerializeField] private float respawnInvincibilityDuration = 2f; // per GDD

    public bool isInvincible = false;
    // Checked by CoreKnockbackTemplate and CoreAttackTemplate before applying damage.
    // Set true during respawn invincibility and cleared after the duration.

    // ─────────────────────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Initialize stocks and validate references.</summary>
    void Start()
    {
        currentStocks = maxStocks;
        isEliminated  = false;
        // TODO: validate knockback, movement, shield, and respawnPosition references
    }

    void Update()
    {
        // TODO: no per-frame polling needed currently.
        //       Hook future time-based stock logic here if required (e.g. timed modes).
    }

    // ─────────────────────────────────────────────────────────────────────────
    // STOCK TRACKING
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by CoreKnockbackTemplate.OnBlastzone() when this character exits a blastzone.
    /// Decrements currentStocks, resets damage, and either respawns or eliminates the player.
    /// </summary>
    public void OnStockLost()
    {
        if (isEliminated) return;

        currentStocks--;
        Debug.Log($"[Stock] {name} lost a stock — {currentStocks}/{maxStocks} remaining");

        UpdateStockUI();

        if (currentStocks <= 0)
        {
            OnEliminated();
            return;
        }

        RespawnPlayer();
    }

    /// <summary>
    /// Stub for game modes or items that grant an extra stock mid-match.
    /// Clamps to maxStocks and updates the HUD.
    /// </summary>
    public void OnStockGained()
    {
        // TODO: if isEliminated return (cannot regain stock after elimination)
        // TODO: currentStocks = Mathf.Min(currentStocks + 1, maxStocks)
        // TODO: UpdateStockUI()
        // TODO: Debug.Log($"[Stock] {name} gained a stock — {currentStocks}/{maxStocks}")
    }

    /// <summary>
    /// Called when currentStocks reaches 0. Flags the player as eliminated
    /// and notifies the match manager.
    /// </summary>
    void OnEliminated()
    {
        isEliminated = true;
        // TODO: disable player input and physics
        // TODO: play elimination VFX / animation
        // TODO: notify a MatchManager or GameController that this player is out
        //       e.g. MatchManager.Instance.OnPlayerEliminated(this.gameObject)
        // TODO: UpdateStockUI()
        Debug.Log($"[Stock] {name} has been eliminated!");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // RESPAWN
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Moves the player to respawnPosition, resets all dependent system state,
    /// and grants respawnInvincibilityDuration seconds of invincibility per GDD.
    /// </summary>
    void RespawnPlayer()
    {
        OnRespawnStart();

        // Reset position
        // TODO: transform.position = respawnPosition.position

        // Reset knockback system
        // TODO: if (knockback != null) knockback.damagePercent = 0f
        // TODO: UpdateDamageUI()

        // Reset movement state (velocity, dashing, crouching)
        // TODO: movement.rb.linearVelocity = Vector2.zero
        // TODO: movement — reset isDashing, isCrouching, hasDoubleJump flags
        //       (expose a ResetState() method on CoreMovementTemplate to keep this clean)

        // Reset shield health
        // TODO: if (shield != null) shield.shieldHealth = shield.shieldMaxHealth

        // Grant respawn invincibility
        StartCoroutine(RespawnInvincibility());

        Debug.Log($"[Stock] {name} respawning");
    }

    /// <summary>
    /// Grants respawnInvincibilityDuration seconds of invincibility immediately after respawn.
    /// isInvincible is checked by CoreKnockbackTemplate and CoreAttackTemplate before dealing damage.
    /// </summary>
    IEnumerator RespawnInvincibility()
    {
        isInvincible = true;
        // TODO: animator.SetBool("IsInvincible", true)
        // TODO: optional: flash sprite to signal invincibility to other players

        yield return new WaitForSeconds(respawnInvincibilityDuration);

        isInvincible = false;
        // TODO: animator.SetBool("IsInvincible", false)

        OnRespawnEnd();
        Debug.Log($"[Stock] {name} respawn invincibility ended");
    }

    /// <summary>Event stub fired at the start of respawn. Hook in animations or UI here.</summary>
    void OnRespawnStart()
    {
        // TODO: play respawn drop-in animation
        // TODO: show respawn platform / indicator UI
        // TODO: notify HUD to flash this player's stock icon
    }

    /// <summary>Event stub fired when respawn invincibility expires. Hook in animations or UI here.</summary>
    void OnRespawnEnd()
    {
        // TODO: hide respawn platform / indicator UI
        // TODO: re-enable full player control if it was suppressed during drop-in
    }

    // ─────────────────────────────────────────────────────────────────────────
    // MATCH STATE
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by MatchManager at the start of the match.
    /// Resets stocks and damage for all players.
    /// </summary>
    public void OnMatchStart()
    {
        // TODO: currentStocks = maxStocks
        // TODO: isEliminated = false
        // TODO: if (knockback != null) knockback.damagePercent = 0f
        // TODO: UpdateStockUI()
        // TODO: UpdateDamageUI()
        Debug.Log($"[Match] {name} ready — {currentStocks} stocks");
    }

    /// <summary>
    /// Called by MatchManager when only one player with stocks remains.
    /// Logs the winner and stubs out UI / scene transition notification.
    /// NOTE: last player with stocks remaining wins per GDD.
    /// </summary>
    public void OnMatchEnd(string winnerName)
    {
        // TODO: display winner screen / UI panel
        // TODO: log match result to a scoreboard or analytics system
        // TODO: trigger end-of-match animation for winner
        Debug.Log($"[Match] Match over — winner: {winnerName}");
    }

    /// <summary>
    /// Returns the number of players that still have at least one stock remaining.
    /// MatchManager should call this each time a stock is lost to check for a winner.
    /// NOTE: last player with stocks remaining wins per GDD.
    /// </summary>
    public int GetActivePlayers()
    {
        // TODO: find all CoreStockTemplate instances in the scene
        //       e.g. FindObjectsByType<CoreStockTemplate>(FindObjectsSortMode.None)
        // TODO: return count where !isEliminated
        return 0;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UI
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called whenever currentStocks changes. Should update this player's
    /// stock icon row on the HUD (fill/empty icons per remaining stock).
    /// </summary>
    void UpdateStockUI()
    {
        // TODO: find this player's HUD stock panel by player index
        // TODO: enable/disable stock icon GameObjects based on currentStocks
        // TODO: play a stock-lost animation on the removed icon
    }

    /// <summary>
    /// Called whenever knockback.damagePercent changes. Should update this player's
    /// damage percentage display on the HUD.
    /// </summary>
    void UpdateDamageUI()
    {
        // TODO: find this player's HUD damage label by player index
        // TODO: set label text to $"{knockback.damagePercent:0}%"
        // TODO: shift label colour from white → red as damagePercent increases
    }
}
