using UnityEngine;

public class BackgroundMusic : MonoBehaviour
{
    // Plug in music here
    public AudioClip musicClip;

    // Volume (0 = silent, 1 = max)
    public float volume = 0.5f;

    // Loop music
    public bool loop = true;

    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = loop;
        audioSource.volume = volume;

        if (musicClip != null)
        {
            audioSource.clip = musicClip;
            audioSource.Play();
        }
    }
}
