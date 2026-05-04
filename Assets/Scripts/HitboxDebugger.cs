using UnityEngine;

public class HitboxDebugger : MonoBehaviour
{
    [Header("Colors")]
    public Color playerColor     = Color.cyan;
    public Color enemyColor      = Color.red;
    public Color projectileColor = Color.yellow;
    public Color defaultColor    = new Color(1f, 1f, 1f, 0.4f);

    public static bool showHitboxes = true;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            showHitboxes = !showHitboxes;
            Debug.Log($"[Hitbox] {(showHitboxes ? "ON" : "OFF")}");
        }
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying || !showHitboxes) return;

        foreach (var col in FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None))
            DrawBox(col);

        foreach (var col in FindObjectsByType<CircleCollider2D>(FindObjectsSortMode.None))
            DrawCircle(col);

        foreach (var col in FindObjectsByType<CapsuleCollider2D>(FindObjectsSortMode.None))
        {
            Gizmos.color = PickColor(col.gameObject);
            Gizmos.DrawWireSphere(col.bounds.center, col.bounds.extents.x);
        }
    }

    void DrawBox(BoxCollider2D col)
    {
        Gizmos.color = PickColor(col.gameObject);
        Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
    }

    void DrawCircle(CircleCollider2D col)
    {
        Gizmos.color = PickColor(col.gameObject);
        Gizmos.DrawWireSphere(col.bounds.center, col.bounds.extents.x);
    }

    Color PickColor(GameObject obj)
    {
        if (obj.GetComponentInParent<HarryMovement>()  != null) return playerColor;
        if (obj.GetComponentInParent<BranduciMovement>() != null) return playerColor;
        if (obj.GetComponentInParent<EnemyDummy>()     != null) return enemyColor;
        if (obj.GetComponentInParent<Projectile>()     != null) return projectileColor;
        if (obj.GetComponentInParent<GundamGrenade>()  != null) return projectileColor;
        return defaultColor;
    }
}
