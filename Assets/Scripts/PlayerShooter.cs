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
        triesUsed++;

        // HIT MARKER BASED HIT CHECK (instead of mouse raycast)
        if (hitDetector != null && hitDetector.currentTarget != null)
        {
            IShootableTarget shootable = hitDetector.currentTarget;

            // only count as a hit if it's the registered currentTarget
            if (shootable != null && shootable == currentTarget)
            {
                shootable.OnShot();

                uiUpdate.score += 300;
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
