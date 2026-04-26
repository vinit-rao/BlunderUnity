// TEMPLATE SCRIPT — CoreCharacterStats.cs — Do not attach directly. Use as a base reference for character-specific scripts.
// NOTE: This is a ScriptableObject. Right-click in the Project window → Create → BlunderUnity → Character Stats to make a new asset.

using UnityEngine;

/// <summary>
/// Data asset holding the six core stats for a single roster character.
/// Each character is balanced to a total of 38 stat points across all six stats (each rated 1–10).
/// Create one asset per character and assign it to their movement and attack scripts via the Inspector.
/// </summary>
///
/// Example roster stat blocks (total = 38):
///   Harry      — ATK 9  DEF 8  SPD 2  JUMP 2  RECOVERY 8  WEIGHT 9
///   Brandon    — ATK 6  DEF 5  SPD 9  JUMP 7  RECOVERY 6  WEIGHT 5
///   ELR        — ATK 9  DEF 3  SPD 8  JUMP 8  RECOVERY 7  WEIGHT 3
///   Snackary   — ATK 5  DEF 9  SPD 3  JUMP 3  RECOVERY 8  WEIGHT 10
///   John Farkas— ATK 6  DEF 7  SPD 6  JUMP 6  RECOVERY 6  WEIGHT 7
///   The Zhangs — ATK 7  DEF 5  SPD 7  JUMP 6  RECOVERY 7  WEIGHT 6

[CreateAssetMenu(fileName = "NewCharacterStats", menuName = "BlunderUnity/Character Stats")]
public class CoreCharacterStats : ScriptableObject
{
    // ─────────────────────────────────────────────────────────────────────────
    // STATS  (each 1–10, total must equal 38)
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Combat Stats")]
    [Range(1, 10)] [SerializeField] public float ATK      = 5f; // damage output multiplier
    [Range(1, 10)] [SerializeField] public float DEF      = 5f; // damage received multiplier

    [Header("Movement Stats")]
    [Range(1, 10)] [SerializeField] public float SPD      = 5f; // walk speed, run speed, dash speed
    [Range(1, 10)] [SerializeField] public float JUMP     = 5f; // jump force, jump apex height

    [Header("Recovery Stats")]
    [Range(1, 10)] [SerializeField] public float RECOVERY = 5f; // double-jump force, fast-fall, air dodge strength
    [Range(1, 10)] [SerializeField] public float WEIGHT   = 5f; // gravity scale, knockback received multiplier

    // ─────────────────────────────────────────────────────────────────────────
    // VALIDATION
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Checks that all six stats sum to exactly 38.
    /// Call from OnValidate() in the editor or from character scripts at Start.
    /// Logs a warning in the Console if the total is off.
    /// </summary>
    public void ValidateStats()
    {
        float total = ATK + DEF + SPD + JUMP + RECOVERY + WEIGHT;
        if (!Mathf.Approximately(total, 38f))
            Debug.LogWarning($"[Stats] '{name}' stat total is {total} — expected 38. Check the stat block.");
        else
            Debug.Log($"[Stats] '{name}' validated — total: {total}");
    }

    /// <summary>Auto-validates in the Unity Inspector whenever a value is changed.</summary>
    void OnValidate()
    {
        ValidateStats();
    }
}
