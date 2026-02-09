using UnityEngine;

public class CitronautMovement : MonoBehaviour
{
    public float minY;                      // Spawn line height

    public float moveSpeed = 7f;

    public float borderBounceBoost = 1.0f;

    public float screenPadding = 0.2f;

    private Rigidbody2D rb;

    private Vector2 driftDir;
    private float targetSpeed;

    public float idleRotateSpeed = 12f;
    private float spinDir = 1f;

    // Bounce audio
    public AudioClip bounceClip; // plug in bounce sound
    public float bounceVolume = 0.5f;
    public float bounceCooldown = 0.06f; // prevents spam on consecutive frames

    private AudioSource audioSource;
    private float lastBounceTime = -999f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.volume = bounceVolume;
    }

    void Start()
    {
        rb.gravityScale = 0f;

        PickInitialDirection();

        // Random clockwise / counterclockwise
        spinDir = (Random.value < 0.5f) ? -1f : 1f;
    }

    void Update()
    {
        // Constant slow idle spin (random direction)
        transform.Rotate(0f, 0f, idleRotateSpeed * spinDir * Time.deltaTime);
    }

    void FixedUpdate()
    {
        if (Camera.main == null) return;

        rb.linearVelocity = driftDir * targetSpeed;

        Camera cam = Camera.main;
        Vector3 bottomLeft = cam.ViewportToWorldPoint(new Vector3(0f, 0f, 0f));
        Vector3 topRight = cam.ViewportToWorldPoint(new Vector3(1f, 1f, 0f));

        Vector2 pos = rb.position;

        float left = bottomLeft.x + screenPadding;
        float right = topRight.x - screenPadding;
        float top = topRight.y - screenPadding;

        bool hitBorder = false;

        // Left / Right
        if (pos.x < left)
        {
            pos.x = left;
            driftDir.x = Mathf.Abs(driftDir.x);
            hitBorder = true;
        }
        else if (pos.x > right)
        {
            pos.x = right;
            driftDir.x = -Mathf.Abs(driftDir.x);
            hitBorder = true;
        }

        // Top
        if (pos.y > top)
        {
            pos.y = top;
            driftDir.y = -Mathf.Abs(driftDir.y);
            hitBorder = true;
        }

        // Bottom (spawn line limit)
        if (pos.y < minY)
        {
            pos.y = minY;
            driftDir.y = Mathf.Abs(driftDir.y);
            hitBorder = true;
        }

        if (hitBorder)
        {
            rb.position = pos;

            driftDir = driftDir.normalized;

            targetSpeed *= borderBounceBoost;

            PlayBounceSound();
        }
    }

    void PickInitialDirection()
    {
        // Random direction, biased upward
        float x = Random.Range(-1f, 1f);
        float y = Random.Range(0.2f, 1f);

        driftDir = new Vector2(x, y).normalized;

        targetSpeed = moveSpeed;
    }

    void PlayBounceSound()
    {
        if (bounceClip == null) return;
        if (Time.time - lastBounceTime < bounceCooldown) return;

        lastBounceTime = Time.time;

        // Update volume in case it's tweaked in Inspector while running
        audioSource.volume = bounceVolume;

        audioSource.PlayOneShot(bounceClip);
    }
}
