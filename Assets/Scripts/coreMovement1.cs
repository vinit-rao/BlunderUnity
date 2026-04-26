using UnityEngine;

public class coreMovement1 : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed            = 5f;
    [SerializeField] private float jumpForce            = 10f;
    [SerializeField] private float crouchSpeedMultiplier = 0.5f;

    [Header("Attack")]
    [SerializeField] private float     attackRange    = 1.5f;
    [SerializeField] private float     attackWedge    = 80f;
    [SerializeField] private float     attackCooldown = 0.3f;
    [SerializeField] private LayerMask enemyLayer;

    private Rigidbody2D   rb;
    private BoxCollider2D col;

    private bool  isGrounded;
    private bool  isCrouching;
    private bool  isFacingRight = true;
    private float attackTimer;
    private float lastHorizontal;

    // 4 cardinal snap directions
    private static readonly Vector2[] Cardinals    = { Vector2.right, Vector2.up, Vector2.left, Vector2.down };
    private static readonly string[]  CardinalNames = { "Right", "Up", "Left", "Down" };
    private int     lastCardinal = -1;
    private Vector2 mouseSnapPos;

    // ─────────────────────────────────────────────────────────────────────────
    void Start()
    {
        rb  = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();
    }

    void Update()
    {
        attackTimer -= Time.deltaTime;
        isGrounded = CheckGrounded();
        HandleMovement();
        HandleJump();
        HandleCrouch();
        HandleAttack();
    }

    // Uses col.bounds (world space) so scale never affects the calculation
    bool CheckGrounded()
    {
        if (col == null) return false;

        Bounds b      = col.bounds;                                   // already world-space
        Vector2 origin = new Vector2(b.center.x, b.min.y - 0.02f);  // just below bottom edge

        RaycastHit2D hit = Physics2D.BoxCast(
            origin,
            new Vector2(b.size.x * 0.8f, 0.02f),
            0f,
            Vector2.down,
            0.15f
        );

        return hit.collider != null && hit.collider.gameObject != gameObject;
    }

    // ─────────────────────────────────────────────────────────────────────────
    void HandleMovement()
    {
        float h     = Input.GetAxisRaw("Horizontal");
        float speed = isCrouching ? moveSpeed * crouchSpeedMultiplier : moveSpeed;
        rb.linearVelocity = new Vector2(h * speed, rb.linearVelocity.y);

        if (h != lastHorizontal)
        {
            if      (h >  0f) Debug.Log("[Move] Moving Right");
            else if (h <  0f) Debug.Log("[Move] Moving Left");
            else              Debug.Log("[Move] Stopped");
            lastHorizontal = h;
        }

        if      (h > 0f && !isFacingRight) Flip();
        else if (h < 0f &&  isFacingRight) Flip();
    }

    void Flip()
    {
        isFacingRight = !isFacingRight;
        transform.localScale = new Vector3(
            -transform.localScale.x,
             transform.localScale.y,
             transform.localScale.z);
    }

    // ─────────────────────────────────────────────────────────────────────────
    void HandleJump()
    {
        if (Input.GetButtonDown("Jump") && isGrounded && !isCrouching)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            Debug.Log("[Move] Jump");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Hold S or Down Arrow to crouch (ground only)
    void HandleCrouch()
    {
        bool hold = (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) && isGrounded;

        if (hold && !isCrouching)
        {
            isCrouching = true;
            Debug.Log("[Move] Crouch Start");
        }
        else if (!hold && isCrouching)
        {
            isCrouching = false;
            Debug.Log("[Move] Crouch End");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    void HandleAttack()
    {
        if (!Input.GetMouseButtonDown(0) || attackTimer > 0f) return;

        attackTimer = attackCooldown;
        int cardinal = GetSnappedCardinal(out Vector2 snapDir);
        lastCardinal = cardinal;
        PerformAttack(cardinal, snapDir);
    }

    int GetSnappedCardinal(out Vector2 snapDir)
    {
        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseSnapPos = mouseWorld;
        Vector2 dir  = (mouseWorld - (Vector2)transform.position).normalized;

        int best = 0; float bestDot = float.MinValue;
        for (int i = 0; i < Cardinals.Length; i++)
        {
            float d = Vector2.Dot(dir, Cardinals[i]);
            if (d > bestDot) { bestDot = d; best = i; }
        }
        snapDir = Cardinals[best];
        return best;
    }

    void PerformAttack(int cardinal, Vector2 attackDir)
    {
        Vector2      origin = (Vector2)transform.position + attackDir * (attackRange * 0.5f);
        Collider2D[] hits   = Physics2D.OverlapCircleAll(origin, attackRange * 0.5f, enemyLayer);

        int hitCount = 0;
        foreach (var hit in hits)
        {
            Vector2 toTarget = ((Vector2)hit.transform.position - (Vector2)transform.position).normalized;
            if (Vector2.Angle(attackDir, toTarget) <= attackWedge * 0.5f)
            {
                hit.GetComponent<EnemyDummy>()?.TakeDamage(10f, attackDir);
                Debug.Log($"[Attack] Hit '{hit.name}'");
                hitCount++;
            }
        }
        Debug.Log($"[Attack] {CardinalNames[cardinal]} — {hitCount} hit(s)");
    }

    // ─────────────────────────────────────────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        // Ground check box visualisation
        if (col != null)
        {
            Bounds  b      = col.bounds;
            Vector3 origin = new Vector3(b.center.x, b.min.y - 0.09f, 0f);
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireCube(origin, new Vector3(b.size.x * 0.8f, 0.15f, 0f));
        }

        Vector3 center = transform.position;

        // Outer circle
        Gizmos.color = new Color(1f, 1f, 1f, 0.15f);
        DrawCircle(center, attackRange, 32);

        // Cardinal snap dots + active wedge
        for (int i = 0; i < Cardinals.Length; i++)
        {
            bool    active = (i == lastCardinal);
            Vector3 snap   = center + (Vector3)(Cardinals[i] * attackRange);
            Gizmos.color = active ? Color.red : Color.green;
            Gizmos.DrawSphere(snap, 0.08f);

            if (active)
            {
                Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.4f);
                float mid = Mathf.Atan2(Cardinals[i].y, Cardinals[i].x) * Mathf.Rad2Deg;
                DrawWedge(center, attackRange, mid - attackWedge * 0.5f, mid + attackWedge * 0.5f, 12);
            }
        }

        if (Application.isPlaying && lastCardinal >= 0)
        {
            Gizmos.color = new Color(1f, 0.5f, 0.6f);
            Gizmos.DrawSphere(mouseSnapPos, 0.06f);
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(mouseSnapPos, center + (Vector3)(Cardinals[lastCardinal] * attackRange));
        }
    }

    void DrawCircle(Vector3 center, float radius, int steps)
    {
        for (int i = 0; i < steps; i++)
        {
            float a1 = (i       / (float)steps) * 360f * Mathf.Deg2Rad;
            float a2 = ((i + 1) / (float)steps) * 360f * Mathf.Deg2Rad;
            Gizmos.DrawLine(
                center + new Vector3(Mathf.Cos(a1), Mathf.Sin(a1)) * radius,
                center + new Vector3(Mathf.Cos(a2), Mathf.Sin(a2)) * radius);
        }
    }

    void DrawWedge(Vector3 center, float radius, float fromDeg, float toDeg, int steps)
    {
        float fr = fromDeg * Mathf.Deg2Rad, tr = toDeg * Mathf.Deg2Rad;
        Gizmos.DrawLine(center, center + new Vector3(Mathf.Cos(fr), Mathf.Sin(fr)) * radius);
        Gizmos.DrawLine(center, center + new Vector3(Mathf.Cos(tr), Mathf.Sin(tr)) * radius);
        for (int i = 0; i < steps; i++)
        {
            float a1 = Mathf.Lerp(fr, tr, (float)i       / steps);
            float a2 = Mathf.Lerp(fr, tr, (float)(i + 1) / steps);
            Gizmos.DrawLine(
                center + new Vector3(Mathf.Cos(a1), Mathf.Sin(a1)) * radius,
                center + new Vector3(Mathf.Cos(a2), Mathf.Sin(a2)) * radius);
        }
    }
}
