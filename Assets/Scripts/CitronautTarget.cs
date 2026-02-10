using UnityEngine;

public class CitronautTarget : MonoBehaviour, IShootableTarget
{
    public CitronautSpawner spawner;

    public GameObject explosionPrefab;

    public float maxTimeOnScreen = 7f; // Seconds before timeout (counts as miss)
    private Coroutine timeoutRoutine;

    public float escapeSpeed = 6f;
    public float destroyDelayWhenShot = 0.15f;

    private bool escaping = false;
    private bool isEnding = false;

    private Vector3 escapeDir = Vector3.up;

    // Escape laugh audio (plays while floating away)
    public AudioClip escapeLaughClip;
    public float escapeLaughVolume = 0.8f;
    public bool loopLaughWhileEscaping = false;

    private AudioSource escapeAudio;

    void Awake()
    {
        escapeAudio = GetComponent<AudioSource>();
        if (escapeAudio == null)
            escapeAudio = gameObject.AddComponent<AudioSource>();

        escapeAudio.playOnAwake = false;
        escapeAudio.loop = false;
        escapeAudio.spatialBlend = 0f; // 2D
    }

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

        if (timeoutRoutine != null) { StopCoroutine(timeoutRoutine); timeoutRoutine = null; }
        if (escapeAudio != null) escapeAudio.Stop();

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

        if (timeoutRoutine != null) { StopCoroutine(timeoutRoutine); timeoutRoutine = null; }

        // Play laugh while escaping (miss or timeout both call this)
        if (escapeLaughClip != null && escapeAudio != null)
        {
            escapeAudio.Stop();

            if (loopLaughWhileEscaping)
            {
                escapeAudio.clip = escapeLaughClip;
                escapeAudio.volume = escapeLaughVolume;
                escapeAudio.loop = true;
                escapeAudio.Play();
            }
            else
            {
                escapeAudio.loop = false;
                escapeAudio.PlayOneShot(escapeLaughClip, escapeLaughVolume);
            }
        }

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

        float life = 0.5f; // fallback

        // Auto-destroy after particle finishes
        ParticleSystem ps = boom.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            life = ps.main.duration;

            if (ps.main.startLifetime.mode == ParticleSystemCurveMode.Constant)
                life += ps.main.startLifetime.constant;
            else
                life += 0.5f; // safe fallback for non-constant modes
        }

        // Audio lifetime
        AudioSource a = boom.GetComponent<AudioSource>();
        if (a != null && a.clip != null)
        {
            life = Mathf.Max(life, a.clip.length);
        }

        Destroy(boom, life + 0.1f);
    }

    void OnEnable()
    {
        if (timeoutRoutine != null) StopCoroutine(timeoutRoutine);
        timeoutRoutine = StartCoroutine(TimeoutRoutine());
    }

    private System.Collections.IEnumerator TimeoutRoutine()
    {
        yield return new WaitForSeconds(maxTimeOnScreen);

        if (isEnding || escaping) yield break;

        // Clear shooter target so player isn't stuck with an expired target
        if (spawner != null && spawner.shooter != null)
            spawner.shooter.RegisterTarget(null);

        // Trigger the same behavior as missing all bullets
        OnOutOfTries();
    }
}
