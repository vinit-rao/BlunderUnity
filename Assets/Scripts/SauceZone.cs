using UnityEngine;

// Placed on dynamically-spawned sauce zone GameObjects by BranduciMovement.
// Tells Branduci when he enters/exits his own sauce so he gets the speed boost.
public class SauceZone : MonoBehaviour
{
    private BranduciMovement owner;
    private float            duration;

    public void Init(BranduciMovement owner, float duration)
    {
        this.owner    = owner;
        this.duration = duration;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (owner != null && other.gameObject == owner.gameObject)
            owner.SetInOwnSauce(true);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (owner != null && other.gameObject == owner.gameObject)
            owner.SetInOwnSauce(false);
    }

    void OnDrawGizmos()
    {
        // Green translucent circle shows sauce zone extent
        CircleCollider2D c = GetComponent<CircleCollider2D>();
        if (c == null) return;
        Gizmos.color = new Color(0.3f, 1f, 0.3f, 0.25f);
        Gizmos.DrawSphere(transform.position, c.radius);
        Gizmos.color = new Color(0.3f, 1f, 0.3f, 0.7f);
        Gizmos.DrawWireSphere(transform.position, c.radius);
    }
}
