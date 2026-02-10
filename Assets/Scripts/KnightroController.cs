using System.Collections;
using UnityEngine;

public class KnightroController : MonoBehaviour
{
    public Transform spawnLine;          // your empty SpawnLine
    public Transform knightro1;          // level 1 sprite object
    public Transform knightro2;          // level 2 sprite object

    public float popUpDistance = 1.5f;   // how far above SpawnLine he rises
    public float slideDuration = 0.35f;  // how fast he slides up/down
    public float holdDuration = 1.2f;    // how long he stays up

    private float targetX;

    bool playing = false;

    // Splash sound for Knightro Level 1 only
    public AudioClip knightroSplashClip;
    public float knightroSplashVolume = 1f;

    private AudioSource audioSource;

    void Start()
    {
        // Start hidden (down) so they don't show at the beginning
        if (knightro1 != null) knightro1.gameObject.SetActive(false);
        if (knightro2 != null) knightro2.gameObject.SetActive(false);

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f; // 2D sound
        }
    }

    public void PlayLevel1(float xPos, System.Action onDone)
    {
        targetX = xPos;

        if (playing) return;
        StartCoroutine(PopRoutine(knightro1, onDone));
    }

    public void PlayLevel2(float xPos, System.Action onDone)
    {
        targetX = xPos;

        if (playing) return;
        StartCoroutine(PopRoutine(knightro2, onDone));
    }

    IEnumerator PopRoutine(Transform knightro, System.Action onDone)
    {
        if (knightro == null || spawnLine == null)
        {
            if (onDone != null) onDone();
            yield break;
        }

        playing = true;

        // Randomize facing each time he appears
        SpriteRenderer sr = knightro.GetComponent<SpriteRenderer>();
        if (sr == null) sr = knightro.GetComponentInChildren<SpriteRenderer>(); // in case renderer is on a child

        if (sr != null)
        {
            sr.flipX = (Random.value < 0.5f);
        }

        knightro.gameObject.SetActive(true);

        // Put him at the SpawnLine position to start (your "behind waves/spaceship" line)
        Vector3 downPos = new Vector3(targetX, spawnLine.position.y, knightro.position.z);
        Vector3 upPos = new Vector3(targetX, spawnLine.position.y + popUpDistance, knightro.position.z);

        knightro.position = downPos;

        // Play splash ONLY for Knightro Level 1 when sliding up
        if (knightro == knightro1 && knightroSplashClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(knightroSplashClip, knightroSplashVolume);
        }

        // Slide up
        yield return Slide(knightro, downPos, upPos, slideDuration);

        // Hold
        yield return new WaitForSeconds(holdDuration);

        // Slide down
        yield return Slide(knightro, upPos, downPos, slideDuration);

        knightro.gameObject.SetActive(false);

        playing = false;

        if (onDone != null) onDone();
    }

    IEnumerator Slide(Transform obj, Vector3 from, Vector3 to, float duration)
    {
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float p = t / duration;

            // Smoothstep for a nice easing (no snapping)
            p = p * p * (3f - 2f * p);

            obj.position = Vector3.Lerp(from, to, p);
            yield return null;
        }

        obj.position = to;
    }
}
