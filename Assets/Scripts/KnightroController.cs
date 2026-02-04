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

    void Start()
    {
        // Start hidden (down) so they don't show at the beginning
        if (knightro1 != null) knightro1.gameObject.SetActive(false);
        if (knightro2 != null) knightro2.gameObject.SetActive(false);
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

        knightro.gameObject.SetActive(true);

        // Put him at the SpawnLine position to start (your "behind waves/spaceship" line)
        Vector3 downPos = new Vector3(targetX, spawnLine.position.y, knightro.position.z);
        Vector3 upPos = new Vector3(targetX, spawnLine.position.y + popUpDistance, knightro.position.z);

        knightro.position = downPos;

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
