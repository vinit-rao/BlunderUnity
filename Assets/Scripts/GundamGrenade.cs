// GundamGrenade — Sticky grenade spawned by HarryMovement E ability.
// Arcs through the air, sticks on contact with anything, flashes red during fuse, then explodes.
// Attach this script to the Gundam Grenade prefab alongside a Rigidbody2D and CircleCollider2D.

using System.Collections;
using UnityEngine;

public class GundamGrenade : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    // CONFIG  (set by HarryMovement before Launch())
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Grenade")]
    public float     fuseDuration      = 1.3f;   // seconds from stick to explosion
    public float     explosionRadius   = 2.5f;   // AOE radius
    public float     explosionDamage   = 40f;
    public float     flashRate         = 0.15f;  // seconds between flashes (speeds up near end)
    public LayerMask hitLayer;

    [HideInInspector] public Collider2D throwerCollider;

    [Header("Cluster Mode")]
    public bool       isCluster           = false;
    public int        clusterCount        = 6;
    public float      clusterSpread       = 7f;
    public float      clusterBombletFuse  = 0.7f;
    public float      clusterUmbrellaAngle = 160f; // spread width in degrees, centred downward
    public GameObject clusterBombletPrefab;

    // ─────────────────────────────────────────────────────────────────────────
    // PRIVATE STATE
    // ─────────────────────────────────────────────────────────────────────────

    private Rigidbody2D    rb;
    private Collider2D     col;
    private SpriteRenderer sr;
    private Color          originalColor;

    private bool  stuck    = false;
    private bool  exploded = false;
    private bool  showBlast = false;
    private float blastTimer = 0f;
    private float blastDuration = 0.45f;

    // ─────────────────────────────────────────────────────────────────────────
    // LIFECYCLE
    // ─────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        rb  = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        sr  = GetComponent<SpriteRenderer>();
        if (sr != null) originalColor = sr.color;
    }

    // Called by HarryMovement immediately after Instantiate
    public void Launch(Vector2 velocity)
    {
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.gravityScale           = 1f;
            rb.linearVelocity         = velocity;
        }

        // Don't collide with the thrower
        if (throwerCollider != null && col != null)
            Physics2D.IgnoreCollision(col, throwerCollider);

        // Brief arm delay so bomblets don't instantly pile up on each other
        StartCoroutine(ArmCoroutine());
    }

    IEnumerator ArmCoroutine()
    {
        if (col != null) col.enabled = false;
        yield return new WaitForSeconds(0.12f);
        if (col != null) col.enabled = true;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // COLLISION — stick on first contact
    // ─────────────────────────────────────────────────────────────────────────

    void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log($"[Grenade] Collision with '{collision.gameObject.name}' — stuck:{stuck}");
        if (stuck || exploded) return;
        Stick(collision.gameObject);
    }

    void Stick(GameObject target)
    {
        stuck = true;

        if (rb != null)
        {
            rb.linearVelocity  = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.gravityScale    = 0f;
            rb.bodyType        = RigidbodyType2D.Kinematic;
        }

        // Attach to enemy so it moves with them
        EnemyDummy enemy = target.GetComponent<EnemyDummy>();
        if (enemy != null) transform.SetParent(target.transform);

        Debug.Log($"[Gundam Grenade] Stuck to '{target.name}' — exploding in {fuseDuration}s");
        StartCoroutine(FuseCoroutine());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // FUSE — flash red, speed up near end
    // ─────────────────────────────────────────────────────────────────────────

    IEnumerator FuseCoroutine()
    {
        float elapsed = 0f;
        bool  lit     = true;

        while (elapsed < fuseDuration)
        {
            if (sr != null) sr.color = lit ? Color.red : originalColor;
            lit = !lit;

            // Flash faster as fuse runs out
            float progress = elapsed / fuseDuration;
            float wait     = Mathf.Lerp(flashRate, flashRate * 0.2f, progress);
            wait           = Mathf.Max(0.04f, wait);

            yield return new WaitForSeconds(wait);
            elapsed += wait;
        }

        Explode();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // EXPLOSION
    // ─────────────────────────────────────────────────────────────────────────

    public void Explode()
    {
        if (exploded) return;
        exploded = true;

        transform.SetParent(null);
        if (sr != null) sr.color = new Color(1f, 0.4f, 0f, 1f);
        if (col != null) col.enabled = false;

        if (isCluster)
            SpawnBomblets();
        else
            DealDamage(explosionRadius, explosionDamage);

        showBlast  = true;
        blastTimer = blastDuration;
        Destroy(gameObject, blastDuration + 0.05f);
    }

    void DealDamage(float radius, float damage)
    {
        Collider2D[] hits = hitLayer.value != 0
            ? Physics2D.OverlapCircleAll(transform.position, radius, hitLayer)
            : Physics2D.OverlapCircleAll(transform.position, radius);

        int count = 0;
        foreach (var hit in hits)
        {
            if (throwerCollider != null && hit.gameObject == throwerCollider.gameObject) continue;
            EnemyDummy enemy = hit.GetComponent<EnemyDummy>();
            if (enemy == null) continue;
            Vector2 dir = ((Vector2)hit.transform.position - (Vector2)transform.position).normalized;
            enemy.TakeDamage(damage, dir * 14f);
            count++;
        }
        Debug.Log($"[Gundam Grenade] BOOM — {count} hit(s) in radius {radius}");
    }

    void SpawnBomblets()
    {
        GameObject prefab = clusterBombletPrefab;
        if (prefab == null) { DealDamage(explosionRadius * 2f, explosionDamage * clusterCount); return; }

        Debug.Log($"[Cluster Grenade] Splitting into {clusterCount} bomblets — umbrella {clusterUmbrellaAngle}°");

        // Fan downward like an umbrella: 270° = straight down, spread left and right
        float halfSpread = clusterUmbrellaAngle * 0.5f;
        float startAngle = 270f - halfSpread;

        for (int i = 0; i < clusterCount; i++)
        {
            // Evenly distribute across the umbrella arc
            float t     = clusterCount > 1 ? (float)i / (clusterCount - 1) : 0.5f;
            float angle = startAngle + t * clusterUmbrellaAngle + Random.Range(-6f, 6f);
            Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            float speed = Random.Range(clusterSpread * 0.7f, clusterSpread);

            GameObject    b    = Instantiate(prefab, transform.position + (Vector3)(dir * 0.25f), Quaternion.identity);
            GundamGrenade bomb = b.GetComponent<GundamGrenade>();
            if (bomb != null)
            {
                bomb.isCluster       = false;
                bomb.fuseDuration    = clusterBombletFuse;
                bomb.explosionDamage = explosionDamage;
                bomb.explosionRadius = explosionRadius;
                bomb.hitLayer        = hitLayer;
                bomb.throwerCollider = throwerCollider;
                bomb.flashRate       = 0.06f;
                bomb.Launch(dir * speed);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // BLAST RADIUS VISUAL — fades out after explosion
    // ─────────────────────────────────────────────────────────────────────────

    void Update()
    {
        if (!showBlast) return;
        blastTimer -= Time.deltaTime;
        if (blastTimer <= 0f) showBlast = false;
    }

    void OnDrawGizmos()
    {
        if (!showBlast) return;
        float alpha = Mathf.Clamp01(blastTimer / blastDuration);
        Gizmos.color = new Color(1f, 0.3f, 0f, alpha * 0.45f);
        Gizmos.DrawSphere(transform.position, explosionRadius);
        Gizmos.color = new Color(1f, 0.6f, 0f, alpha);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
