using UnityEngine;

/// <summary>
/// Draws collider outlines for the player, enemy, and projectiles.
/// Press L to toggle. Requires Gizmos ON in the Game view toolbar.
/// Attach to any single GameObject in the scene.
/// </summary>
public class HitboxDebugger : MonoBehaviour
{
    [Header("Colors")]
    [SerializeField] private Color playerColor     = Color.cyan;
    [SerializeField] private Color enemyColor      = Color.red;
    [SerializeField] private Color projectileColor = Color.yellow;
    [SerializeField] private Color defaultColor    = Color.white;

    public static bool showHitboxes = true;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            showHitboxes = !showHitboxes;
            Debug.Log($"[Hitbox] Debug outlines {(showHitboxes ? "ON" : "OFF")}");
        }
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying || !showHitboxes) return;

        foreach (var col in FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None))
        {
            Gizmos.color = GetColor(col.gameObject);
            Bounds b = col.bounds;
            Gizmos.DrawWireCube(b.center, b.size);
        }

        foreach (var col in FindObjectsByType<CircleCollider2D>(FindObjectsSortMode.None))
        {
            Gizmos.color = GetColor(col.gameObject);
            Gizmos.DrawWireSphere(col.bounds.center, col.bounds.extents.x);
        }
    }

    Color GetColor(GameObject obj)
    {
        if (obj.GetComponent<coreMovement1>() != null) return playerColor;
        if (obj.GetComponent<EnemyDummy>()    != null) return enemyColor;
        if (obj.GetComponent<Projectile>()    != null) return projectileColor;
        return defaultColor;
    }
}
