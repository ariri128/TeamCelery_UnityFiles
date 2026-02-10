using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShooter : MonoBehaviour
{
    public int maxTries = 3;

    private int triesUsed = 0;
    private IShootableTarget currentTarget;

    public UIUpdateScript uiUpdate;

    public HitMarkerDetector hitDetector;
    private bool canShoot = true;

    // Gunshot audio
    public AudioClip gunShotClip;
    public float gunShotVolume = 0.6f;

    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }

    public void RegisterTarget(IShootableTarget target)
    {
        currentTarget = target;
        triesUsed = 0;
    }

    void Update()
    {
        if (!canShoot) return;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            HandleClick();
        }
    }

    private void HandleClick()
    {
        // Play gunshot sound
        if (gunShotClip != null)
        {
            audioSource.PlayOneShot(gunShotClip, gunShotVolume);
        }

        triesUsed++;

        // Hit marker based hit check (instead of mouse raycast)
        if (hitDetector != null && hitDetector.currentTarget != null)
        {
            IShootableTarget shootable = hitDetector.currentTarget;

            // Only count as a hit if it's the registered currentTarget
            if (shootable != null && shootable == currentTarget)
            {
                shootable.OnShot();

                if (ScoreHolder.Instance != null)
                {
                    ScoreHolder.Instance.AddScore(300);
                    uiUpdate.score = ScoreHolder.Instance.TotalScore;
                }
                else
                {
                    // Fallback
                    uiUpdate.score += 300;
                }
                uiUpdate.ScoreUpdate();

                if (uiUpdate.currentlevel == 1)
                {
                    if (uiUpdate.currentCollections <= 10)
                        uiUpdate.ShowCollectedDucks();
                }
                else if (uiUpdate.currentlevel == 2)
                {
                    if (uiUpdate.currentCollections <= 10)
                        uiUpdate.ShowCitronautHits();
                }

                currentTarget = null;
                triesUsed = 0;
                return;
            }
        }

        // If player misses
        uiUpdate.bulletsUsed = triesUsed;
        uiUpdate.BulletShot();

        if (currentTarget != null && triesUsed >= maxTries)
        {
            currentTarget.OnOutOfTries();
            currentTarget = null;
            triesUsed = 0;
        }
    }

    public void EnableShooting()
    {
        canShoot = true;

        if (hitDetector != null)
            hitDetector.enabled = true;
    }

    public void DisableShooting()
    {
        canShoot = false;

        if (hitDetector != null)
            hitDetector.enabled = false;
    }
}

public interface IShootableTarget
{
    void OnShot();
    void OnOutOfTries();
}
