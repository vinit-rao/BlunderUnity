// TEMPLATE SCRIPT — CoreLedgeTemplate.cs — Do not attach directly. Use as a base reference for character-specific scripts.
// NOTE: This script is intentionally non-functional. All logic is stubbed with TODOs.
// Attach alongside CoreMovementTemplate on the same GameObject.

using System.Collections;
using UnityEngine;

/// <summary>
/// Handles ledge grabbing, ledge options (jump, climb, attack), and ledge trump.
/// GrabLedge() is called when the character is airborne and enters a ledge trigger zone.
/// Exposes isHoldingLedge publicly so other systems can read movement state.
/// </summary>
public class CoreLedgeTemplate : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    // REFERENCES
    // ─────────────────────────────────────────────────────────────────────────

    [Header("References")]
    [SerializeField] private CoreCharacterStats stats;
    // TODO: assign in Inspector — drag this character's CoreCharacterStats asset

    private Rigidbody2D rb;

    // ─────────────────────────────────────────────────────────────────────────
    // LEDGE STATE
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Ledge State")]
    public bool isHoldingLedge = false;
    // Exposed publicly so CoreMovementTemplate can suppress normal movement while true.

    private Transform currentLedge;     // the ledge Transform currently being held
    private bool      isInvincibleOnLedge;

    // ─────────────────────────────────────────────────────────────────────────
    // LEDGE OPTIONS
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Ledge Options")]
    [SerializeField] private float ledgeJumpForce    = 12f; // upward force on ledge jump
    [SerializeField] private float ledgeClimbTime    = 0.3f; // seconds to climb up animation
    [SerializeField] private float ledgeAttackRange  = 1.2f; // hitbox range on ledge attack
    [SerializeField] private LayerMask enemyLayer;

    // ─────────────────────────────────────────────────────────────────────────
    // LEDGE GRAB INVINCIBILITY
    // Per GDD: 20 frames of invincibility on grab (20 / 60 ≈ 0.333s)
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Invincibility")]
    [SerializeField] private int ledgeInvincibilityFrames = 20;
    // Converted to seconds at 60fps: ledgeInvincibilityFrames / 60f

    // ─────────────────────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Cache components.</summary>
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        // TODO: validate stats reference is assigned
    }

    void Update()
    {
        if (isHoldingLedge)
            HandleLedgeInput();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // LEDGE GRAB
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called when the character's ledge grab trigger overlaps a ledge zone while airborne.
    /// Snaps the character to the ledge, freezes Rigidbody, and grants 20 frames of invincibility.
    /// </summary>
    /// <param name="ledge">The Transform of the ledge being grabbed.</param>
    public void GrabLedge(Transform ledge)
    {
        // TODO: if isHoldingLedge return (already on a ledge)
        // TODO: currentLedge = ledge
        // TODO: isHoldingLedge = true
        // TODO: rb.linearVelocity = Vector2.zero
        // TODO: rb.bodyType = RigidbodyType2D.Kinematic
        // TODO: snap transform.position to ledge hang position
        // TODO: StartCoroutine(LedgeInvincibilityCoroutine())
        // TODO: animator.SetBool("IsHoldingLedge", true)
        Debug.Log($"[Ledge] Grabbed ledge: {ledge?.name}");
    }

    /// <summary>Grants 20 frames (per GDD) of invincibility immediately after grabbing a ledge.</summary>
    IEnumerator LedgeInvincibilityCoroutine()
    {
        // TODO: isInvincibleOnLedge = true
        // TODO: yield return new WaitForSeconds(ledgeInvincibilityFrames / 60f)  // 20f / 60f ≈ 0.333s
        // TODO: isInvincibleOnLedge = false
        yield return null;
    }

    /// <summary>Releases the ledge and restores normal physics.</summary>
    void ReleaseLedge()
    {
        // TODO: isHoldingLedge = false
        // TODO: currentLedge = null
        // TODO: rb.bodyType = RigidbodyType2D.Dynamic
        // TODO: animator.SetBool("IsHoldingLedge", false)
        Debug.Log("[Ledge] Released ledge");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // LEDGE INPUT
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Reads input while holding a ledge and routes to the correct ledge option.</summary>
    void HandleLedgeInput()
    {
        // TODO: Jump input       → LedgeJump()
        // TODO: Up input         → LedgeClimb()
        // TODO: Attack input     → LedgeAttack()
        // TODO: Down input       → ReleaseLedge() (drop off ledge)
    }

    // ─────────────────────────────────────────────────────────────────────────
    // LEDGE OPTIONS
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Jumps off the ledge. Applies an upward force and releases the ledge hold.
    /// Jump strength is modified by the JUMP stat from CoreCharacterStats.
    /// </summary>
    public void LedgeJump()
    {
        // TODO: ReleaseLedge()
        // TODO: float force = ledgeJumpForce * Mathf.Lerp(0.8f, 1.2f, (stats.JUMP - 1f) / 9f)
        // TODO: rb.linearVelocity = new Vector2(rb.linearVelocity.x, force)
        // TODO: animator.SetTrigger("LedgeJump")
        Debug.Log("[Ledge] Ledge Jump");
    }

    /// <summary>
    /// Climbs up onto the stage surface. Plays the climb animation then places the
    /// character standing at the ledge top position.
    /// </summary>
    public void LedgeClimb()
    {
        // TODO: StartCoroutine(LedgeClimbCoroutine())
        // TODO: animator.SetTrigger("LedgeClimb")
        Debug.Log("[Ledge] Ledge Climb");
    }

    /// <summary>Moves the character to the stage top position over ledgeClimbTime seconds.</summary>
    IEnumerator LedgeClimbCoroutine()
    {
        // TODO: disable input for ledgeClimbTime
        // TODO: lerp transform.position to ledge top position over ledgeClimbTime seconds
        // TODO: yield return new WaitForSeconds(ledgeClimbTime)
        // TODO: ReleaseLedge()
        // TODO: re-enable input
        yield return null;
    }

    /// <summary>
    /// Attacks from the ledge. Spawns a hitbox on the stage surface and then releases the ledge.
    /// Uses Physics2D.OverlapCircleAll against enemyLayer.
    /// </summary>
    public void LedgeAttack()
    {
        // TODO: ReleaseLedge()
        // TODO: Vector2 attackDir = isFacingRight ? Vector2.right : Vector2.left
        // TODO: Vector2 origin = (Vector2)transform.position + attackDir * (ledgeAttackRange * 0.5f)
        // TODO: Collider2D[] hits = Physics2D.OverlapCircleAll(origin, ledgeAttackRange, enemyLayer)
        // TODO: foreach hit → hit.GetComponent<CoreKnockbackTemplate>()?.TakeDamage(...)
        // TODO: animator.SetTrigger("LedgeAttack")
        Debug.Log("[Ledge] Ledge Attack");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // LEDGE TRUMP
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called when another player grabs the same ledge this character is holding.
    /// Forces this character to release and become briefly vulnerable.
    /// </summary>
    /// <param name="trumper">The character that stole the ledge.</param>
    public void LedgeTrump(GameObject trumper)
    {
        // TODO: ReleaseLedge()
        // TODO: apply a small downward/outward velocity to simulate being knocked off
        // TODO: brief hitstun or tumble state
        // TODO: notify CoreKnockbackTemplate of the trump event
        Debug.Log($"[Ledge] Trumped by {trumper?.name}");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TRIGGER DETECTION
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Detects entry into a ledge grab zone trigger.
    /// Requires a trigger collider tagged "Ledge" on the stage.
    /// Only grabs if airborne and moving downward (falling onto the ledge).
    /// </summary>
    void OnTriggerEnter2D(Collider2D other)
    {
        // TODO: if other.CompareTag("Ledge") && !isHoldingLedge && rb.linearVelocity.y < 0
        //   → GrabLedge(other.transform)
    }

    /// <summary>
    /// Detects when another player enters the same ledge zone.
    /// If this character is already holding the ledge, trigger LedgeTrump.
    /// </summary>
    void OnTriggerStay2D(Collider2D other)
    {
        // TODO: if isHoldingLedge && other.CompareTag("Player") && other.gameObject != this.gameObject
        //   → other.GetComponent<CoreLedgeTemplate>()?.LedgeTrump(this.gameObject)
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GIZMOS
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Draws ledge attack range and invincibility state in the Scene view.</summary>
    void OnDrawGizmosSelected()
    {
        // Ledge attack range
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, ledgeAttackRange);

        // TODO: draw a marker at the expected ledge hang position when isHoldingLedge
        // TODO: highlight the character outline cyan when isInvincibleOnLedge
    }
}
