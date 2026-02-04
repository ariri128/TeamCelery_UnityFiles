using UnityEngine;

public class DuckTarget : MonoBehaviour, IShootableTarget
{
    public DuckSpawner spawner;
    public float destroyDelay = 0.15f;

    private SpriteRenderer sr;
    private bool isEnding = false;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public void OnShot()
    {
        if (isEnding) return;
        isEnding = true;

        if (spawner != null)
            spawner.OnTargetFinished(true, transform.position.x);

        Destroy(gameObject, destroyDelay);
    }

    public void OnOutOfTries()
    {
        if (isEnding) return;
        isEnding = true;

        // “Goes back in the water” = not visible to player anymore
        if (sr != null)
            sr.enabled = false;

        if (spawner != null)
            spawner.OnTargetFinished(false, transform.position.x);

        Destroy(gameObject, destroyDelay);
    }
}
