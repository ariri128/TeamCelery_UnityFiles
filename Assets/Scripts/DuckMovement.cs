using UnityEngine;

public class DuckMovement : MonoBehaviour
{
    // Horizontal movement
    public float speed = 2f;
    public float edgePadding = 0.4f;

    // Bobbing
    public float bobAmplitude = 0.12f;
    public float bobFrequency = 2f;

    // Sway
    public float swayRotation = 6f;
    public float swayFrequency = 2f;

    // Direction: 1 = right, -1 = left
    public int direction = 1;

    // Jump settings
    public float minJumpInterval = 2.5f;   // minimum time between jump attempts
    public float maxJumpInterval = 6.0f;   // maximum time between jump attempts
    public float jumpHeight = 1.2f;        // how high above the waterline
    public float jumpDuration = 0.8f;      // how long the arc lasts

    private Camera cam;
    private SpriteRenderer sr;

    private float baseY;
    private float timeOffset;

    // Jump state
    private bool isJumping = false;
    private float jumpStartTime = 0f;
    private float nextJumpTime = 0f;

    // Miss / dive animation
    public float missHopHeight = 0.35f; // small hop up before diving
    public float missDiveDepth = 0.6f; // how far below waterline it dives
    public float missDiveDuration = 0.45f; // total time of hop + dive
    private bool isDiving = false;

    // Splash audio
    public AudioClip jumpLandSplashClip; // plug in splash sound
    public float jumpLandSplashVolume = 0.35f;

    // Quack audio
    // Quack sound
    public AudioClip quackSound;
    public float minQuackInterval = 3f;
    public float maxQuackInterval = 7f;
    public float quackVolume = 0.6f;

    private AudioSource audioSource;
    private float nextQuackTime;

    void Start()
    {
        cam = Camera.main;
        sr = GetComponent<SpriteRenderer>();

        baseY = transform.position.y;
        timeOffset = Random.Range(0f, 1000f);

        ScheduleNextJump();
        UpdateFlip();

        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;

        ScheduleNextQuack();
    }

    void Update()
    {
        if (isDiving) return;

        // Random quack
        if (quackSound != null && Time.time >= nextQuackTime)
        {
            audioSource.PlayOneShot(quackSound, quackVolume);
            ScheduleNextQuack();
        }

        // Move left/right
        transform.position += Vector3.right * (direction * speed * Time.deltaTime);

        float t = Time.time + timeOffset;

        // RANDOM JUMP TRIGGER
        if (!isJumping && Time.time >= nextJumpTime)
        {
            StartJump();
        }

        // Vertical motion
        Vector3 pos = transform.position;

        if (isJumping)
        {
            float jumpT = (Time.time - jumpStartTime) / jumpDuration;

            if (jumpT >= 1f)
            {
                isJumping = false;
                ScheduleNextJump();

                pos.y = baseY; // return to waterline
                transform.position = pos;

                // Landing splash
                PlayOneShot(jumpLandSplashClip, jumpLandSplashVolume);
            }
            else
            {
                // Parabolic arc: 0 -> up -> 0 (like a dolphin jump)
                // 4t(1-t) gives a nice symmetrical arc peaking at t = 0.5
                float arc = 4f * jumpT * (1f - jumpT) * jumpHeight;
                pos.y = baseY + arc;
                transform.position = pos;
            }

            // Optional: slight tilt during jump still looks nice
            float tilt = Mathf.Sin(t * swayFrequency) * swayRotation;
            transform.rotation = Quaternion.Euler(0f, 0f, tilt);
        }
        else
        {
            // Normal bobbing + tilt
            float bob = Mathf.Sin(t * bobFrequency) * bobAmplitude;
            pos.y = baseY + bob;
            transform.position = pos;

            float tilt = Mathf.Sin(t * swayFrequency) * swayRotation;
            transform.rotation = Quaternion.Euler(0f, 0f, tilt);
        }

        // Bounce off screen edges
        float leftEdge = cam.ViewportToWorldPoint(new Vector3(0f, 0.5f, 0f)).x + edgePadding;
        float rightEdge = cam.ViewportToWorldPoint(new Vector3(1f, 0.5f, 0f)).x - edgePadding;

        pos = transform.position;

        if (pos.x <= leftEdge && direction < 0)
        {
            pos.x = leftEdge;
            transform.position = pos;

            direction = 1;
            UpdateFlip();
        }
        else if (pos.x >= rightEdge && direction > 0)
        {
            pos.x = rightEdge;
            transform.position = pos;

            direction = -1;
            UpdateFlip();
        }
    }

    void StartJump()
    {
        isJumping = true;
        jumpStartTime = Time.time;
    }

    void ScheduleNextJump()
    {
        nextJumpTime = Time.time + Random.Range(minJumpInterval, maxJumpInterval);
    }

    void ScheduleNextQuack()
    {
        nextQuackTime = Time.time + Random.Range(minQuackInterval, maxQuackInterval);
    }

    void UpdateFlip()
    {
        // If your duck faces the wrong way, flip this logic.
        if (sr != null)
            sr.flipX = (direction < 0);
    }

    void PlayOneShot(AudioClip clip, float volume)
    {
        if (clip == null) return;

        AudioSource a = GetComponent<AudioSource>();
        if (a == null) a = gameObject.AddComponent<AudioSource>();

        a.playOnAwake = false;
        a.loop = false;
        a.spatialBlend = 0f;
        a.volume = volume;

        a.PlayOneShot(clip);
    }

    // Spawner calls this after spawning so bobbing uses the correct "waterline" Y
    public void SetBaseY(float newBaseY)
    {
        baseY = newBaseY;

        Vector3 pos = transform.position;
        pos.y = baseY;
        transform.position = pos;
    }

    public void PlayMissDive(System.Action onDone = null)
    {
        if (isDiving) return;
        StartCoroutine(MissDiveRoutine(onDone));
    }

    private System.Collections.IEnumerator MissDiveRoutine(System.Action onDone)
    {
        isDiving = true;

        // Stop normal jump cycle so it doesn't fight the animation
        isJumping = false;

        transform.rotation = Quaternion.identity;

        Vector3 start = transform.position;
        Vector3 peak = new Vector3(start.x, baseY + missHopHeight, start.z);
        Vector3 end = new Vector3(start.x, baseY - missDiveDepth, start.z);

        float half = Mathf.Max(0.01f, missDiveDuration * 0.5f);

        // Hop up
        float t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / half);
            transform.position = Vector3.Lerp(start, peak, p);
            yield return null;
        }

        // Dive down
        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / half);
            transform.position = Vector3.Lerp(peak, end, p);
            yield return null;
        }

        onDone?.Invoke();
    }
}
