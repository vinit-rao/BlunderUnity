using UnityEngine;

/// <summary>
/// General-purpose ranged projectile. Spawned by coreMovement1.HandleRangedAttack().
/// Travels in a straight line, deals damage on first hit, then destroys itself.
/// </summary>
public class Projectile : MonoBehaviour
{
    [Header("Projectile")]
    [SerializeField] public  float     damage          = 8f;
    [SerializeField] public  float     speed           = 18f;
    [SerializeField] public  float     lifetime        = 3f;
    [SerializeField] private LayerMask hitLayer;

    [Header("Explosion")]
    public bool  explodeOnHit    = false;
    public float explosionRadius = 1.5f;

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
        if (other.gameObject == transform.parent?.gameObject) return;

        bool layerMatch = hitLayer.value != 0
            ? (hitLayer.value & (1 << other.gameObject.layer)) != 0
            : other.GetComponent<EnemyDummy>() != null;

        if (!layerMatch) return;

        if (explodeOnHit)
        {
            Explode();
        }
        else
        {
            EnemyDummy enemy = other.GetComponent<EnemyDummy>();
            if (enemy == null) return;
            enemy.TakeDamage(damage, direction);
            Debug.Log($"[Ranged] Hit '{other.name}' for {damage}");
        }

        Destroy(gameObject);
    }

    void Explode()
    {
        Collider2D[] hits = hitLayer.value != 0
            ? Physics2D.OverlapCircleAll(transform.position, explosionRadius, hitLayer)
            : Physics2D.OverlapCircleAll(transform.position, explosionRadius);

        int count = 0;
        foreach (var hit in hits)
        {
            if (hit.gameObject == transform.parent?.gameObject) continue;
            EnemyDummy enemy = hit.GetComponent<EnemyDummy>();
            if (enemy == null) continue;
            Vector2 knockDir = ((Vector2)hit.transform.position - (Vector2)transform.position).normalized;
            enemy.TakeDamage(damage, knockDir * 10f);
            count++;
        }
        Debug.Log($"[Ranged] Exploded at {transform.position} — {count} hit(s) in radius {explosionRadius}");
    }
}
