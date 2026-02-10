using UnityEngine;

public class CitronautSpawner : MonoBehaviour
{
    public GameObject citronautPrefab;
    public PlayerShooter shooter;
    public KnightroController knightro;
    public UIUpdateScript uiUpdate;

    public LevelLoader winLevelLoader; // set nextSceneName to your WinScene (if using a scene)
    public bool useWinScene = true; // if false, it will just stop and log win (if using UI panel)

    public Sprite[] citronautRoundSprites; // different Citronauts for each round

    public Transform spawnLine; // Set this to your SpawnLine object
    public float spawnInterval = 1.25f;

    public int maxAlive = 1;

    public float firstSpawnDelay = 1.5f; // fast spawn at level start
    public float respawnDelay = 3f; // gap for Knightro animation after Citronaut ends

    private bool respawnQueued = false;

    public float horizontalPadding = 0.5f;

    private float timer;

    private float lastHitX;

    // Rounds per level
    public int killsPerRound = 10;
    public int totalRounds = 3;

    private int currentRound = 1;
    private int killsThisRound = 0;

    public int spawnsPerRound = 10;
    public int maxMissesToLose = 5;

    public LevelLoader loseLevelLoader; // set nextSceneName = LoseScene

    private int spawnsThisRound = 0;
    private int hitSlotIndex = 0;
    private int missesTotal = 0;

    public float betweenRoundDelay = 0.75f;
    private bool pendingNextRound = false;

    private bool spawningEnabled = false;

    // Size change per round
    public float round1Scale = 1f; // Round 1 size
    public float scaleDecreasePerRound = 0.08f; // Round 2 = 1 - 0.08, Round 3 = 1 - 0.16
    public float minScale = 0.7f; // Safety cap

    void Start()
    {
        spawningEnabled = false;
        timer = 0f;
        CancelInvoke();

        currentRound = 1;
        killsThisRound = 0;
        spawnsThisRound = 0;
        hitSlotIndex = 0;
        missesTotal = 0;

        if (uiUpdate != null)
            uiUpdate.ResetHitMeter();

        if (uiUpdate != null)
        {
            uiUpdate.round = currentRound;
            uiUpdate.currentCollections = 0;
            uiUpdate.HideCitronautHits();
            uiUpdate.RoundUpdate();
            uiUpdate.ReloadBullets();
        }

        if (shooter != null)
            shooter.DisableShooting();

        if (uiUpdate != null)
        {
            uiUpdate.PlayLevelIntro(() =>
            {
                if (shooter != null)
                    shooter.EnableShooting();

                // After level intro, show Round 1 stuff
                uiUpdate.round = currentRound;
                uiUpdate.RoundUpdate();
                uiUpdate.ShowRoundPanel(currentRound);

                float popupDelay = uiUpdate.roundPanelShowSeconds;

                // IMPORTANT: block ALL spawning until popup is finished
                spawningEnabled = false;
                timer = 0f;
                CancelInvoke();

                // Enable spawning ONLY after the round panel time is over
                Invoke(nameof(EnableSpawning), popupDelay);

                // First duck spawn after: popup + firstSpawnDelay
                Invoke(nameof(TrySpawnOne), popupDelay + firstSpawnDelay);
            });
        }
        else
        {
            if (shooter != null)
                shooter.EnableShooting();

            spawningEnabled = true;
            Invoke(nameof(TrySpawnOne), firstSpawnDelay);
        }
    }

    void Update()
    {
        if (!spawningEnabled) return;

        if (respawnQueued) return;

        if (citronautPrefab == null || spawnLine == null) return;

        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            timer = 0f;

            if (CountAlive() < maxAlive)
            {
                SpawnOne();

                if (uiUpdate != null)
                    uiUpdate.ReloadBullets();
            }
        }
    }

    int CountAlive()
    {
        return FindObjectsByType<CitronautMovement>(FindObjectsSortMode.None).Length;
    }

    void SpawnOne()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        float spawnY = spawnLine.position.y;

        // World-space left/right camera bounds at the spawn line height
        Vector3 leftWorld = cam.ViewportToWorldPoint(new Vector3(0f, 0.5f, 0f));
        Vector3 rightWorld = cam.ViewportToWorldPoint(new Vector3(1f, 0.5f, 0f));

        float minX = leftWorld.x + horizontalPadding;
        float maxX = rightWorld.x - horizontalPadding;

        float x = Random.Range(minX, maxX);
        Vector3 spawnPos = new Vector3(x, spawnY, 0f);

        GameObject citronaut = Instantiate(citronautPrefab, spawnPos, Quaternion.identity);

        // Set sprite and sprite size per round
        SpriteRenderer sr = citronaut.GetComponent<SpriteRenderer>();
        if (sr != null && citronautRoundSprites != null && citronautRoundSprites.Length > 0)
        {
            int index = Mathf.Clamp(currentRound - 1, 0, citronautRoundSprites.Length - 1);

            if (citronautRoundSprites[index] != null)
                sr.sprite = citronautRoundSprites[index];

            // Scale per round
            float scale = round1Scale - (currentRound - 1) * scaleDecreasePerRound;
            scale = Mathf.Max(scale, minScale);

            citronaut.transform.localScale = Vector3.one * scale;

            // Resize circle collider to match
            CircleCollider2D circle = citronaut.GetComponent<CircleCollider2D>();
            if (circle != null)
            {
                circle.radius = 0.5f; // normalized size
                circle.offset = Vector2.zero;
            }
        }

        CitronautTarget target = citronaut.GetComponent<CitronautTarget>();
        if (target != null)
        {
            target.spawner = this;

            if (shooter != null)
                shooter.RegisterTarget(target);
        }

        // Pass the spawn line Y into the floater so it knows the “do not go below” limit
        CitronautMovement floater = citronaut.GetComponent<CitronautMovement>();
        if (floater != null)
        {
            floater.minY = spawnY;
        }
    }

    void TrySpawnOne()
    {
        if (citronautPrefab == null || spawnLine == null) return;

        if (CountAlive() < maxAlive)
        {
            SpawnOne();

            if (uiUpdate != null)
                uiUpdate.ReloadBullets();
        }
    }

    public void OnTargetFinished(bool wasKilled, float hitX)
    {
        lastHitX = hitX;

        if (uiUpdate != null)
            uiUpdate.SetHitSlot(hitSlotIndex, wasKilled);

        hitSlotIndex++;
        spawnsThisRound++;

        if (!wasKilled)
        {
            missesTotal++;

            if (missesTotal >= maxMissesToLose)
            {
                LoseNow();
                return;
            }
        }

        if (wasKilled)
        {
            CancelInvoke(nameof(RespawnOne));

            if (respawnQueued) return;

            respawnQueued = true;
            timer = 0f;

            killsThisRound++;

            if (spawnsThisRound >= spawnsPerRound)
            {
                if (currentRound >= totalRounds)
                {
                    Invoke(nameof(PlayKnightroThenWin), 1f);
                    return;
                }
                else
                {
                    currentRound++;
                    spawnsThisRound = 0;
                    hitSlotIndex = 0;
                    killsThisRound = 0; // optional

                    pendingNextRound = true;
                }
            }

            Invoke(nameof(PlayKnightroAndRespawn), 1f);
        }
        else
        {
            if (spawnsThisRound >= spawnsPerRound)
            {
                if (currentRound >= totalRounds)
                {
                    Invoke(nameof(PlayKnightroThenWin), 1f);
                    return;
                }
                else
                {
                    currentRound++;
                    spawnsThisRound = 0;
                    hitSlotIndex = 0;
                    killsThisRound = 0; // optional

                    Invoke(nameof(BeginNextRound), betweenRoundDelay);
                    return;
                }
            }

            Invoke(nameof(RespawnOne), spawnInterval);
        }
    }

    void RespawnOne()
    {
        respawnQueued = false;
        TrySpawnOne();
    }

    void BeginNextRound()
    {
        if (uiUpdate != null)
        {
            uiUpdate.round = currentRound;
            uiUpdate.currentCollections = 0;
            uiUpdate.ResetHitMeter();

            uiUpdate.RoundUpdate();
            uiUpdate.ShowRoundPanel(currentRound);
        }

        float popupDelay = (uiUpdate != null) ? uiUpdate.roundPanelShowSeconds : 0f;

        Invoke(nameof(RespawnOne), popupDelay);
    }

    void PlayKnightroAndRespawn()
    {
        if (knightro != null)
        {
            knightro.PlayLevel2(lastHitX, () =>
            {
                if (pendingNextRound)
                {
                    pendingNextRound = false;
                    Invoke(nameof(BeginNextRound), betweenRoundDelay);
                }
                else
                {
                    Invoke(nameof(RespawnOne), respawnDelay);
                }
            });
        }
        else
        {
            if (pendingNextRound)
            {
                pendingNextRound = false;
                Invoke(nameof(BeginNextRound), betweenRoundDelay);
            }
            else
            {
                Invoke(nameof(RespawnOne), respawnDelay);
            }
        }
    }

    void PlayKnightroThenWin()
    {
        if (knightro != null)
        {
            knightro.PlayLevel2(lastHitX, () =>
            {
                WinNow();
            });
        }
        else
        {
            WinNow();
        }
    }

    void EnableSpawning()
    {
        spawningEnabled = true;
        timer = 0f; // reset so Update doesn’t instantly spawn
    }

    void WinNow()
    {
        Debug.Log("YOU WIN!");
        FindFirstObjectByType<PlayerShooter>()?.DisableShooting();

        if (useWinScene && winLevelLoader != null)
        {
            winLevelLoader.LoadNextLevel(); // set nextSceneName = "WinScene" later
        }
        else
        {
            // Temporary fallback since win scene/panel is not set yet
            enabled = false; // stops spawning
        }
    }

    void LoseNow()
    {
        Debug.Log("YOU LOSE!");

        spawningEnabled = false;
        respawnQueued = true;
        CancelInvoke();

        if (shooter != null)
            shooter.DisableShooting();

        if (loseLevelLoader != null)
            loseLevelLoader.LoadNextLevel();
        else
            Debug.LogError("CitronautSpawner loseLevelLoader not assigned (set it in Inspector).");
    }
}
