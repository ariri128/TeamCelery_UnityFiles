using UnityEngine;

public class HitMarkerDetector : MonoBehaviour
{
    public IShootableTarget currentTarget;

    void OnTriggerEnter2D(Collider2D other)
    {
        IShootableTarget target = other.GetComponentInParent<IShootableTarget>();

        if (target != null)
        {
            currentTarget = target;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        IShootableTarget target = other.GetComponentInParent<IShootableTarget>();

        if (target != null && target == currentTarget)
        {
            currentTarget = null;
        }
    }
}
