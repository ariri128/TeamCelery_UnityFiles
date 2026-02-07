using UnityEngine;

public class CitronautTarget : MonoBehaviour, IShootableTarget
{
    public CitronautSpawner spawner;

    public GameObject explosionPrefab;

    public float escapeSpeed = 6f;
    public float destroyDelayWhenShot = 0.15f;

    private bool escaping = false;
    private bool isEnding = false;

    private Vector3 escapeDir = Vector3.up;

    void Update()
    {
        if (escaping)
        {
            transform.position += escapeDir * escapeSpeed * Time.deltaTime;

            if (IsOffscreen())
            {
                if (spawner != null)
                    spawner.OnTargetFinished(false, transform.position.x);

                Destroy(gameObject);
            }
        }
    }

    bool IsOffscreen()
    {
        if (Camera.main == null) return false;

        Vector3 vp = Camera.main.WorldToViewportPoint(transform.position);

        // a small margin so it fully disappears before destroying
        return (vp.x < -0.1f || vp.x > 1.1f || vp.y < -0.1f || vp.y > 1.1f);
    }

    public void OnShot()
    {
        if (isEnding) return;
        isEnding = true;

        // Hide citronaut instantly
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = false;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // Play explosion
        SpawnExplosion();

        if (spawner != null)
            spawner.OnTargetFinished(true, transform.position.x);

        // Remove object shortly after
        Destroy(gameObject, destroyDelayWhenShot);
    }
    
    public void OnOutOfTries()
    {
        if (isEnding) return;
        isEnding = true;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        CitronautMovement move = GetComponent<CitronautMovement>();

        if (rb != null)
        {
            Vector2 v = rb.linearVelocity;

            // Direction: keep going where it was moving
            if (v.sqrMagnitude > 0.0001f)
                escapeDir = new Vector3(v.x, v.y, 0f).normalized;
            else
                escapeDir = Vector3.up;

            // Speed: match its randomized movement speed
            escapeSpeed = v.magnitude;

            // Stop physics so it doesn't fight escape motion
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        // Disable normal movement so it doesn't clamp/bounce it back onscreen
        if (move != null)
            move.enabled = false;

        escaping = true;
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
