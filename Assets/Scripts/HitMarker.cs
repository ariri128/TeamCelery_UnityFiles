using UnityEngine;
using UnityEngine.InputSystem;

public class HitMarker : MonoBehaviour
{
    // How far the hit marker is from the real mouse (in SCREEN pixels)
    public Vector2 screenOffsetPixels = new Vector2(-200f, 120f);

    // Still keep a little “floaty” feel (set to 0 for none)
    public float followSmoothTime = 0.0f;

    // Keep marker inside camera view
    public float screenPadding = 0.2f;

    Camera cam;
    Vector3 velocity;

    void Awake()
    {
        cam = Camera.main;
    }

    void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;
        if (Mouse.current == null) return;

        // 1) Read mouse position in SCREEN space
        Vector2 mouseScreen = Mouse.current.position.ReadValue();

        // 2) Apply the Duck Hunt-style offset in SCREEN space
        Vector2 markerScreen = mouseScreen + screenOffsetPixels;

        // 3) Convert markerScreen -> WORLD
        float zDist = -cam.transform.position.z; // for a 2D camera at z = -10, this is 10
        Vector3 markerWorld = cam.ScreenToWorldPoint(new Vector3(markerScreen.x, markerScreen.y, zDist));
        markerWorld.z = 0f;

        // 4) Clamp inside camera view (world-space clamp)
        Vector3 bottomLeft = cam.ViewportToWorldPoint(new Vector3(0f, 0f, zDist));
        Vector3 topRight = cam.ViewportToWorldPoint(new Vector3(1f, 1f, zDist));

        markerWorld.x = Mathf.Clamp(markerWorld.x, bottomLeft.x + screenPadding, topRight.x - screenPadding);
        markerWorld.y = Mathf.Clamp(markerWorld.y, bottomLeft.y + screenPadding, topRight.y - screenPadding);

        // 5) Move marker (optionally smoothed)
        if (followSmoothTime <= 0f)
        {
            transform.position = markerWorld;
        }
        else
        {
            transform.position = Vector3.SmoothDamp(transform.position, markerWorld, ref velocity, followSmoothTime);
        }
    }

    /*
    public float followSmoothTime = 0.08f; // higher = more lag
    public float maxLagDistance = 0.8f; // clamp so it can't drift too far behind

    // Clamp to camera
    public float screenPadding = 0.2f;

    private Camera cam;
    private Vector3 velocity;

    void Awake()
    {
        cam = Camera.main;
    }

    void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;
        if (Mouse.current == null) return;

        Vector2 mouseScreen = Mouse.current.position.ReadValue();

        Vector3 mouseWorld = cam.ScreenToWorldPoint(
            new Vector3(mouseScreen.x, mouseScreen.y, -cam.transform.position.z)
        );
        mouseWorld.z = 0f;

        // Clamp inside camera view
        Vector3 bottomLeft = cam.ViewportToWorldPoint(new Vector3(0f, 0f, -cam.transform.position.z));
        Vector3 topRight = cam.ViewportToWorldPoint(new Vector3(1f, 1f, -cam.transform.position.z));

        mouseWorld.x = Mathf.Clamp(mouseWorld.x, bottomLeft.x + screenPadding, topRight.x - screenPadding);
        mouseWorld.y = Mathf.Clamp(mouseWorld.y, bottomLeft.y + screenPadding, topRight.y - screenPadding);

        // Smooth lag follow
        Vector3 newPos = Vector3.SmoothDamp(transform.position, mouseWorld, ref velocity, followSmoothTime);

        // Max lag clamp
        Vector3 delta = newPos - mouseWorld;
        if (delta.magnitude > maxLagDistance)
            newPos = mouseWorld + delta.normalized * maxLagDistance;

        newPos.z = 0f;
        transform.position = newPos;
    }
    */
}
