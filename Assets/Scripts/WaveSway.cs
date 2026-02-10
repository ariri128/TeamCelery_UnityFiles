using UnityEngine;

public class WaveSway : MonoBehaviour
{
    public float swayAmount = 0.15f;
    public float secondsPerHalf = 1.0f; // time to go from center -> left or center -> right

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.localPosition;
    }

    void Update()
    {
        float t = Mathf.PingPong(Time.time / secondsPerHalf, 1f); // 0..1..0..1
        float eased = t * t * (3f - 2f * t); // smoothstep easing
        float x = Mathf.Lerp(-swayAmount, swayAmount, eased);
        transform.localPosition = startPos + new Vector3(x, 0f, 0f);
    }
}
