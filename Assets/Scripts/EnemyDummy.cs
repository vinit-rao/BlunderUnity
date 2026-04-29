using System.Collections;
using UnityEngine;

public class EnemyDummy : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth    = 200f;
    public float currentHealth;

    [Header("Knockback")]
    public float knockbackForce    = 8f;
    public float knockbackDuration = 0.2f;

    [Header("Flash")]
    public Color hitColor  = Color.red;
    public float flashTime = 0.12f;

    [Header("Respawn")]
    public float respawnDelay = 3f;

    private Rigidbody2D    rb;
    private SpriteRenderer sr;
    private Collider2D     col;
    private Color          originalColor;
    private Vector3        spawnPos;
    private bool           isDead;

    void Awake()
    {
        rb  = GetComponent<Rigidbody2D>();
        sr  = GetComponentInChildren<SpriteRenderer>();
        col = GetComponent<Collider2D>();

        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.freezeRotation         = true;
            rb.linearDamping          = 8f;
        }

        if (sr != null) originalColor = sr.color;
    }

    void Start()
    {
        spawnPos      = transform.position;
        currentHealth = maxHealth;
    }

    // ─────────────────────────────────────────────────────────────────────────

    public void TakeDamage(float amount, Vector2 direction)
    {
        if (isDead) return;

        currentHealth -= amount;
        Debug.Log($"[Dummy] -{amount} HP ({currentHealth}/{maxHealth}) from {direction}");

        StopAllCoroutines();
        StartCoroutine(FlashCoroutine());
        if (direction != Vector2.zero)
            StartCoroutine(KnockbackCoroutine(direction));

        if (currentHealth <= 0f)
            StartCoroutine(DieCoroutine());
    }

    public void TakeDamage(float amount) => TakeDamage(amount, Vector2.zero);

    // ─────────────────────────────────────────────────────────────────────────

    IEnumerator FlashCoroutine()
    {
        if (sr == null) yield break;
        sr.color = hitColor;
        yield return new WaitForSeconds(flashTime);
        sr.color = originalColor;
    }

    IEnumerator KnockbackCoroutine(Vector2 direction)
    {
        if (rb == null) yield break;
        rb.linearVelocity = direction.normalized * knockbackForce;
        yield return new WaitForSeconds(knockbackDuration);
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    IEnumerator DieCoroutine()
    {
        isDead = true;
        if (col != null) col.enabled = false;
        if (rb  != null) rb.linearVelocity = Vector2.zero;

        // Flicker
        for (int i = 0; i < 6; i++)
        {
            if (sr != null) sr.enabled = !sr.enabled;
            yield return new WaitForSeconds(0.1f);
        }

        gameObject.SetActive(false);
        yield return new WaitForSeconds(respawnDelay);

        transform.position = spawnPos;
        currentHealth      = maxHealth;
        isDead             = false;
        gameObject.SetActive(true);
        if (col != null) col.enabled  = true;
        if (rb  != null) rb.linearVelocity = Vector2.zero;
        if (sr  != null) { sr.enabled = true; sr.color = originalColor; }

        Debug.Log("[Dummy] Respawned");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Health bar drawn above the sprite in Scene/Game Gizmos view

    void OnDrawGizmos()
    {
        if (!Application.isPlaying || isDead) return;

        Renderer rend = GetComponentInChildren<Renderer>();
        float topY    = rend != null ? rend.bounds.max.y + 0.2f
                                     : transform.position.y + 1.5f;
        float width   = rend != null ? rend.bounds.size.x : 1f;
        width         = Mathf.Max(width, 0.5f);

        float pct    = Mathf.Clamp01(currentHealth / maxHealth);
        Vector3 orig = new Vector3(transform.position.x - width * 0.5f, topY, 0f);

        // Background
        Gizmos.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        Gizmos.DrawCube(orig + Vector3.right * width * 0.5f, new Vector3(width, 0.12f, 0f));

        // Fill
        Gizmos.color = Color.Lerp(Color.red, Color.green, pct);
        Gizmos.DrawCube(orig + Vector3.right * width * pct * 0.5f, new Vector3(width * pct, 0.12f, 0f));
    }
}
