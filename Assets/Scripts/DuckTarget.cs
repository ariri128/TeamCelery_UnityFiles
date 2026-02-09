using UnityEngine;

public class DuckTarget : MonoBehaviour, IShootableTarget
{
    public DuckSpawner spawner;
    public float destroyDelay = 0.15f;

    public GameObject explosionPrefab;

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

        // Hide the duck instantly
        if (sr != null) sr.enabled = false;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // Explosion
        SpawnExplosion();

        if (spawner != null)
            spawner.OnTargetFinished(true, transform.position.x);

        // Now remove the object after a moment
        Destroy(gameObject, destroyDelay);
    }

    public void OnOutOfTries()
    {
        if (isEnding) return;
        isEnding = true;

        // Prevent being shot during the miss animation
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        DuckMovement mover = GetComponent<DuckMovement>();

        if (mover != null)
        {
            mover.PlayMissDive(() =>
            {
                if (spawner != null)
                    spawner.OnTargetFinished(false, transform.position.x);

                Destroy(gameObject);
            });
        }
        else
        {
            // Fallback to old behavior if DuckMovement is missing
            if (sr != null) sr.enabled = false;

            if (spawner != null)
                spawner.OnTargetFinished(false, transform.position.x);

            Destroy(gameObject, destroyDelay);
        }
    }

    private void SpawnExplosion()
    {
        if (explosionPrefab == null)
        {
            Debug.LogWarning("Explosion prefab not assigned on " + gameObject.name);
            return;
        }

        // Spawn slightly in front (for 2D layering safety)
        Vector3 pos = transform.position;
        pos.z = -1f;

        GameObject boom = Instantiate(explosionPrefab, pos, Quaternion.identity);

        // Auto-destroy after particle finishes
        ParticleSystem ps = boom.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            float time = ps.main.duration;

            if (ps.main.startLifetime.mode == ParticleSystemCurveMode.Constant)
                time += ps.main.startLifetime.constant;

            Destroy(boom, time + 0.1f);
        }
        else
        {
            Destroy(boom, 2f);
        }
    }
}
