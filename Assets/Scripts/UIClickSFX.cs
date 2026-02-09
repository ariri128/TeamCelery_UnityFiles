using UnityEngine;
using UnityEngine.UI;

public class UIClickSFX : MonoBehaviour
{
    public AudioClip clickClip; // Insert audio clip here
    public float volume = 0.3f;

    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f; // 2D
    }

    void Start()
    {
        // Auto-hook every UI Button in the scene
        Button[] buttons = FindObjectsByType<Button>(FindObjectsSortMode.None);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button b = buttons[i];
            b.onClick.AddListener(PlayClick);
        }
    }

    public void PlayClick()
    {
        if (clickClip == null) return;
        audioSource.PlayOneShot(clickClip, volume);
    }
}
