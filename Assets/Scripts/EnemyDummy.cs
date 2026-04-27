using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EnemyDummy : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("Knockback")]
    [SerializeField] private float knockbackForce    = 5f;
    [SerializeField] private float knockbackDuration = 0.15f;

    [Header("Flash on Hit")]
    [SerializeField] private Color hitColor  = Color.red;
    [SerializeField] private float flashTime = 0.1f;

    [Header("Health Bar")]
    [SerializeField] private Slider healthBar;

    [Header("Respawn")]
    [SerializeField] private float respawnDelay = 2f;

    private Rigidbody2D   rb;
    private SpriteRenderer sr;
    private Color         originalColor;
    private Vector3       spawnPosition;
    private bool          isDead;

    // ─────────────────────────────────────────────────────────────────────────
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        if (sr != null) originalColor = sr.color;

        spawnPosition = transform.position;
        currentHealth = maxHealth;

        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            healthBar.value    = currentHealth;
        }

        Debug.Log($"[Dummy] Spawned at {spawnPosition} — HP: {currentHealth}/{maxHealth}");
    }

    // ─────────────────────────────────────────────────────────────────────────
    public void TakeDamage(float amount, Vector2 hitDirection)
    {
        if (isDead) return;

        currentHealth -= amount;
        if (healthBar != null) healthBar.value = currentHealth;
        Debug.Log($"[Dummy] Hit for {amount} from direction {hitDirection} — HP: {currentHealth}/{maxHealth}");

        StartCoroutine(FlashRed());
        StartCoroutine(Knockback(hitDirection));

        if (currentHealth <= 0f)
            StartCoroutine(Die());
    }

    public void TakeDamage(float amount) => TakeDamage(amount, Vector2.zero);

    // ─────────────────────────────────────────────────────────────────────────
    IEnumerator FlashRed()
    {
        if (sr == null) yield break;
        Debug.Log("[Dummy] Flash red");
        sr.color = hitColor;
        yield return new WaitForSeconds(flashTime);
        sr.color = originalColor;
    }

    IEnumerator Knockback(Vector2 direction)
    {
        if (rb == null || direction == Vector2.zero) yield break;

        Debug.Log($"[Dummy] Knockback — direction {direction}, force {knockbackForce}");
        rb.linearVelocity = direction.normalized * knockbackForce;
        yield return new WaitForSeconds(knockbackDuration);
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); // stop horizontal, keep gravity
        Debug.Log("[Dummy] Knockback ended");
    }

    IEnumerator Die()
    {
        isDead = true;
        Debug.Log($"[Dummy] Defeated — respawning in {respawnDelay}s");

        for (int i = 0; i < 5; i++)
        {
            if (sr != null) sr.enabled = false;
            yield return new WaitForSeconds(0.1f);
            if (sr != null) sr.enabled = true;
            yield return new WaitForSeconds(0.1f);
        }

        gameObject.SetActive(false);
        Debug.Log("[Dummy] Hidden — waiting to respawn");

        yield return new WaitForSeconds(respawnDelay);

        transform.position = spawnPosition;
        currentHealth      = maxHealth;
        isDead             = false;
        gameObject.SetActive(true);
        if (rb != null) rb.linearVelocity = Vector2.zero;
        if (sr != null) sr.color = originalColor;
        if (healthBar != null) healthBar.value = maxHealth;

        Debug.Log($"[Dummy] Respawned — HP: {currentHealth}/{maxHealth}");
    }

    // ─────────────────────────────────────────────────────────────────────────
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        float   pct   = currentHealth / maxHealth;
        Vector3 base_ = transform.position + Vector3.up * 1.2f;

        Gizmos.color = Color.grey;
        Gizmos.DrawCube(base_, new Vector3(1f, 0.1f, 0f));

        Gizmos.color = Color.Lerp(Color.red, Color.green, pct);
        Gizmos.DrawCube(base_ + Vector3.left * (1f - pct) * 0.5f, new Vector3(pct, 0.1f, 0f));
    }
}
