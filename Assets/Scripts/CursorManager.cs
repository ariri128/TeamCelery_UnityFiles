using UnityEngine;

public class CursorManager : MonoBehaviour
{
    public static CursorManager instance;

    public Texture2D cursorTexture; // Put cursor sprite
    public Vector2 hotspotPixels = Vector2.zero; // Click point in pixels
    public CursorMode cursorMode = CursorMode.Auto;

    // Cursor size
    public float cursorScale = 1.5f; // 1 = original size

    public bool useCustomCursor = true;

    void Awake()
    {
        // Make sure only one exists
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        DontDestroyOnLoad(gameObject); // Keeps cursor in throughout entire game
    }

    void OnEnable()
    {
        ApplyCursor();
    }

    void OnDisable()
    {
        Cursor.SetCursor(null, Vector2.zero, cursorMode);
    }

    void ApplyCursor()
    {
        if (!useCustomCursor || cursorTexture == null)
        {
            Cursor.SetCursor(null, Vector2.zero, cursorMode);
            return;
        }

        Texture2D scaled = GetScaledCursor(cursorTexture, cursorScale);

        Vector2 scaledHotspot = hotspotPixels * cursorScale;

        Cursor.SetCursor(scaled, scaledHotspot, cursorMode);
    }

    Texture2D GetScaledCursor(Texture2D source, float scale)
    {
        int width = Mathf.RoundToInt(source.width * scale);
        int height = Mathf.RoundToInt(source.height * scale);

        Texture2D result = new Texture2D(
            width,
            height,
            TextureFormat.RGBA32,
            false
        );

        result.filterMode = FilterMode.Point;
        result.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float u = (float)x / width;
                float v = (float)y / height;
                result.SetPixel(x, y, source.GetPixelBilinear(u, v));
            }
        }

        result.Apply();
        return result;
    }
}
