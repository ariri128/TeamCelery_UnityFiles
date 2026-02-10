using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class UIClickSFX : MonoBehaviour
{
    public static UIClickSFX Instance;

    public AudioClip clickClip;
    public float volume = 0.3f;

    private AudioSource audioSource;

    void Awake()
    {
        // Singleton + persist across scene loads
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;

        // Hook current scene + future scenes
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        HookAllButtonsInScene();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        HookAllButtonsInScene();
    }

    void HookAllButtonsInScene()
    {
        // Include inactive so X buttons inside hidden panels get hooked too
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < buttons.Length; i++)
        {
            Button b = buttons[i];

            // Prevent double-adding if HookAllButtonsInScene runs multiple times
            b.onClick.RemoveListener(PlayClick);
            b.onClick.AddListener(PlayClick);
        }
    }

    public void PlayClick()
    {
        if (clickClip == null) return;
        audioSource.PlayOneShot(clickClip, volume);
    }

    // Use this for buttons that immediately load scenes or quit:
    public void PlayClickAndRun(System.Action action)
    {
        StartCoroutine(PlayThen(action));
    }

    private IEnumerator PlayThen(System.Action action)
    {
        PlayClick();

        // Small delay so the click is audible even if the next action is instant.
        // Use a tiny minimum so it still works if clickClip is very short.
        float wait = (clickClip != null) ? Mathf.Max(0.05f, clickClip.length * 0.25f) : 0.05f;
        yield return new WaitForSecondsRealtime(wait);

        action?.Invoke();
    }

    /*
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
    */
}
