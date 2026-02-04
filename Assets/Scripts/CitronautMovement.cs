using UnityEngine;

public class CitronautMovement : MonoBehaviour
{
    public float minY;                      // Spawn line height

    public float minSpeed = 0.6f;
    public float maxSpeed = 1.6f;

    public float borderBounceBoost = 1.0f;

    public float rotateSpeedAfterHit = 40f;
    public float rotationEase = 2.0f;

    public float screenPadding = 0.2f;

    private Rigidbody2D rb;

    private Vector2 driftDir;
    private float targetSpeed;

    private float spinAmount;
    private float spinDirection = 1f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        rb.gravityScale = 0f;

        PickInitialDirection();
    }

    void Update()
    {
        // Slowly fade out spinning
        spinAmount = Mathf.MoveTowards(spinAmount, 0f, Time.deltaTime * 0.25f);

        float spin = rotateSpeedAfterHit * spinAmount * spinDirection;
        transform.Rotate(0f, 0f, spin * Time.deltaTime);
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

            // Start spin
            spinAmount = Mathf.MoveTowards(spinAmount, 1f, Time.fixedDeltaTime * rotationEase);
            spinDirection = (Random.value < 0.5f) ? -1f : 1f;
        }
    }

    void PickInitialDirection()
    {
        // Random direction, biased upward
        float x = Random.Range(-1f, 1f);
        float y = Random.Range(0.2f, 1f);

        driftDir = new Vector2(x, y).normalized;

        targetSpeed = Random.Range(minSpeed, maxSpeed);
    }
}
