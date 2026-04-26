using System.Collections;
using UnityEngine;

public class EnemyDummy : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("Knockback")]
    [SerializeField] private float knockbackForce    = 5f;
    [SerializeField] private float knockbackDuration = 0.15f;

    [Header("Flash on Hit")]
    [SerializeField] private Color hitColor   = Color.red;
    [SerializeField] private float flashTime  = 0.1f;

    [Header("Respawn")]
    [SerializeField] private float respawnDelay = 2f;

    private Rigidbody2D   rb;
    private SpriteRenderer sr;
    private Color         originalColor;
    private bool          isDead;

    // ─────────────────────────────────────────────────────────────────────────
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        if (rb != null) rb.bodyType = RigidbodyType2D.Kinematic;
        if (sr != null) originalColor = sr.color;

        currentHealth = maxHealth;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Call this from the player attack
    public void TakeDamage(float amount, Vector2 hitDirection)
    {
        if (isDead) return;

        currentHealth -= amount;
        Debug.Log($"[Dummy] Hit for {amount} — HP: {currentHealth}/{maxHealth}");

        StartCoroutine(FlashRed());
        StartCoroutine(Knockback(hitDirection));

        if (currentHealth <= 0f)
            StartCoroutine(Die());
    }

    // Overload with no knockback direction (attack from unknown angle)
    public void TakeDamage(float amount) => TakeDamage(amount, Vector2.zero);

    // ─────────────────────────────────────────────────────────────────────────
    IEnumerator FlashRed()
    {
        if (sr == null) yield break;
        sr.color = hitColor;
        yield return new WaitForSeconds(flashTime);
        sr.color = originalColor;
    }

    IEnumerator Knockback(Vector2 direction)
    {
        if (rb == null || direction == Vector2.zero) yield break;

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.linearVelocity = direction.normalized * knockbackForce;
        yield return new WaitForSeconds(knockbackDuration);
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    IEnumerator Die()
    {
        isDead = true;
        Debug.Log("[Dummy] Defeated — respawning...");

        // Simple death flash
        for (int i = 0; i < 5; i++)
        {
            if (sr != null) sr.enabled = false;
            yield return new WaitForSeconds(0.1f);
            if (sr != null) sr.enabled = true;
            yield return new WaitForSeconds(0.1f);
        }

        gameObject.SetActive(false);
        yield return new WaitForSeconds(respawnDelay);

        // Respawn
        currentHealth = maxHealth;
        isDead        = false;
        gameObject.SetActive(true);
        if (sr != null) sr.color = originalColor;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Health bar in Scene view
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        float pct    = currentHealth / maxHealth;
        Vector3 base_ = transform.position + Vector3.up * 1.2f;

        // Background
        Gizmos.color = Color.grey;
        Gizmos.DrawCube(base_, new Vector3(1f, 0.1f, 0f));

        // Fill
        Gizmos.color = Color.Lerp(Color.red, Color.green, pct);
        Gizmos.DrawCube(base_ + Vector3.left * (1f - pct) * 0.5f, new Vector3(pct, 0.1f, 0f));
    }
}
