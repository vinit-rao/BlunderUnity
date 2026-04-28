using UnityEngine;

/// <summary>
/// General-purpose ranged projectile. Spawned by coreMovement1.HandleRangedAttack().
/// Travels in a straight line, deals damage on first hit, then destroys itself.
/// </summary>
public class Projectile : MonoBehaviour
{
    [Header("Projectile")]
    [SerializeField] public  float damage       = 8f;
    [SerializeField] public  float speed        = 18f;
    [SerializeField] public  float lifetime     = 3f;   // auto-destroy after this many seconds
    [SerializeField] private LayerMask hitLayer;        // should match the player's enemyLayer

    private Vector2      direction;
    private Rigidbody2D  rb;

    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>Set travel direction and auto-destroy timer. Called by the spawner.</summary>
    public void Init(Vector2 dir, LayerMask layer)
    {
        rb        = GetComponent<Rigidbody2D>();
        direction = dir.normalized;
        hitLayer  = layer;

        if (rb != null)
            rb.linearVelocity = direction * speed;

        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[Ranged] Collider touched: '{other.name}' on layer {other.gameObject.layer} ({LayerMask.LayerToName(other.gameObject.layer)}) — hitLayer value: {hitLayer.value}");

        if ((hitLayer.value & (1 << other.gameObject.layer)) == 0)
        {
            Debug.Log($"[Ranged] Layer mismatch — skipping '{other.name}'");
            return;
        }

        other.GetComponent<EnemyDummy>()?.TakeDamage(damage, direction);
        Debug.Log($"[Ranged] Projectile hit '{other.name}'");
        Destroy(gameObject);
    }
}
