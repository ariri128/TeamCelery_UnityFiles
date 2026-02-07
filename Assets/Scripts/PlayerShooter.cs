using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShooter : MonoBehaviour
{
    public int maxTries = 3;

    private int triesUsed = 0;
    private IShootableTarget currentTarget;

    public UIUpdateScript uiUpdate;

    public void RegisterTarget(IShootableTarget target)
    {
        currentTarget = target;
        triesUsed = 0;
    }

    void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            HandleClick();
        }
    }

    private void HandleClick()
    {
        triesUsed++;

        if (Camera.main == null) return;

        Vector2 worldPoint = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        RaycastHit2D hit = Physics2D.Raycast(worldPoint, Vector2.zero);

        if (hit.collider != null)
        {
            IShootableTarget shootable = hit.collider.GetComponent<IShootableTarget>();
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

        uiUpdate.bulletsUsed = triesUsed;
        uiUpdate.BulletShot();

        if (currentTarget != null && triesUsed >= maxTries)
        {
            currentTarget.OnOutOfTries();
            currentTarget = null;
            triesUsed = 0;
        }
    }
}

public interface IShootableTarget
{
    void OnShot();
    void OnOutOfTries();
}
