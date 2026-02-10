using UnityEngine;

public class DuckSpawner : MonoBehaviour
{
    public GameObject duckPrefab;
    public Sprite[] duckSprites;
    public float targetVisibleDuckHeight = 0.35f;
    public float duckHitboxScale = 0.85f; // 1 = full sprite bounds, smaller = tighter

    public PlayerShooter shooter;
    public KnightroController knightro;
    public LevelLoader levelLoader;
    public UIUpdateScript uiUpdate;

    public float spawnInterval = 1.5f;
    public int maxDucksAlive = 1;

    public float firstSpawnDelay = 1.5f;   // fast spawn when level starts
    public float respawnDelay = 3f;       // gap for Knightro animation after a duck ends

    private bool respawnQueued = false;

    public Transform spawnLine;

    // Small random Y variation so it looks natural
    public float waterLineRandomRange = 0.15f;

    public float edgePadding = 0.4f;

    private Camera cam;
    private float timer;

    private float lastHitX;

    // Rounds per level
    public int killsPerRound = 10;
    public int totalRounds = 3;

    private int currentRound = 1;
    private int killsThisRound = 0;

    public int spawnsPerRound = 10; // round ends after 10 ducks appear (attempts)
    public int maxMissesToLose = 5;

    public LevelLoader loseLevelLoader; // set nextSceneName = LoseScene

    private int spawnsThisRound = 0; // counts attempts (hit OR miss)
    private int hitSlotIndex = 0; // 0..9 (consumes a slot every attempt)
    private int missesTotal = 0; // lose when reaches maxMissesToLose

    public float betweenRoundDelay = 0.75f; // delay after Knightro before round updates
    private bool pendingNextRound = false;

    private bool spawningEnabled = false;

    // Speed per round
    public float round1DuckSpeed = 4f; // speed for Round 1
    public float speedIncreasePerRound = 0.35f; // added each round (Round2 = +1x, Round3 = +2x)
    public float maxDuckSpeed = 6f; // To cap it just in case

    // Splash audio
    public AudioClip spawnSplashClip; // plug in splash sound
    public float spawnSplashVolume = 0.35f;

    // Quack audio
    public AudioClip spawnQuackClip; // plug in quack sound
    public float spawnQuackVolume = 1f;

    void Start()
    {
        cam = Camera.main;

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
            uiUpdate.currentCollections = killsThisRound;
            uiUpdate.HideAllDucks();
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
                Invoke(nameof(TrySpawnDuck), popupDelay + firstSpawnDelay);
            });
        }
        else
        {
            if (shooter != null)
                shooter.EnableShooting();

            spawningEnabled = true;
            Invoke(nameof(TrySpawnDuck), firstSpawnDelay);
        }
    }

    void Update()
    {
        if (!spawningEnabled) return;

        if (respawnQueued) return;

        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            timer = 0f;

            if (CountAliveDucks() < maxDucksAlive)
            {
                SpawnDuck();

                if (uiUpdate != null)
                    uiUpdate.ReloadBullets();
            }
        }
    }

    void SpawnDuck()
    {
        float leftEdge = cam.ViewportToWorldPoint(new Vector3(0f, 0.5f, 0f)).x + edgePadding;
        float rightEdge = cam.ViewportToWorldPoint(new Vector3(1f, 0.5f, 0f)).x - edgePadding;

        float x = Random.Range(leftEdge, rightEdge);
        float spawnY = (spawnLine != null) ? spawnLine.position.y : 0f;
        float y = spawnY + Random.Range(-waterLineRandomRange, waterLineRandomRange); ;

        GameObject duck = Instantiate(duckPrefab, new Vector3(x, y, 0f), Quaternion.identity);

        // Audio at start of spawn
        AudioSource a = duck.GetComponent<AudioSource>();
        if (a == null) a = duck.AddComponent<AudioSource>();

        a.playOnAwake = false;
        a.loop = false;
        a.spatialBlend = 0f; // 0 = 2D

        // Spawn splash
        if (spawnSplashClip != null)
        {
            a.PlayOneShot(spawnSplashClip, spawnSplashVolume);
        }

        // Spawn quack
        if (spawnQuackClip != null)
        {
            a.PlayOneShot(spawnQuackClip, spawnQuackVolume);
        }

        // Randomize which teammate duck sprite is used
        SpriteRenderer sr = duck.GetComponent<SpriteRenderer>();
        if (sr != null && duckSprites != null && duckSprites.Length > 0)
        {
            Sprite chosen = duckSprites[Random.Range(0, duckSprites.Length)];
            sr.sprite = chosen;

            Texture2D tex = chosen.texture;
            Rect r = GetVisiblePixelRect(tex, 10);

            if (r.width > 0f && r.height > 0f)
            {
                // Convert visible pixel height into world units using PPU
                float visibleHeightWorld = r.height / chosen.pixelsPerUnit;

                float targetVisibleHeightWorld = targetVisibleDuckHeight;

                float scale = targetVisibleHeightWorld / visibleHeightWorld;
                duck.transform.localScale = new Vector3(scale, scale, duck.transform.localScale.z);

                // Match collider to the chosen sprite's bounds
                BoxCollider2D box = duck.GetComponent<BoxCollider2D>();
                if (box == null) box = duck.AddComponent<BoxCollider2D>();

                Vector2 size = sr.sprite.bounds.size;
                Vector2 center = sr.sprite.bounds.center;

                size *= duckHitboxScale;

                box.size = size;
                box.offset = center;

                box.isTrigger = true;
            }
        }

        DuckTarget target = duck.GetComponent<DuckTarget>();
        if (target != null)
        {
            target.spawner = this;

            if (shooter != null)
                shooter.RegisterTarget(target);
        }

        // Tag it so the spawner can count ducks
        duck.tag = "Duck";

        // Set up mover
        DuckMovement mover = duck.GetComponent<DuckMovement>();
        if (mover != null)
        {
            mover.SetBaseY(y);

            // Speed scales per round (Round 1 = round1DuckSpeed)
            float scaledSpeed = round1DuckSpeed + (currentRound - 1) * speedIncreasePerRound;
            mover.speed = Mathf.Min(scaledSpeed, maxDuckSpeed);

            // Random start direction
            mover.direction = (Random.value < 0.5f) ? -1 : 1;
        }
    }

    int CountAliveDucks()
    {
        GameObject[] ducks = GameObject.FindGameObjectsWithTag("Duck");
        return ducks.Length;
    }

    void TrySpawnDuck()
    {
        // If the camera isn't ready yet, try again next frame-ish
        if (cam == null)
            cam = Camera.main;

        if (cam == null) return;

        if (spawnLine == null)
        {
            Debug.LogWarning("DuckSpawner: spawnLine not assigned.");
            return;
        }

        if (CountAliveDucks() < maxDucksAlive)
        {
            SpawnDuck();

            if (uiUpdate != null)
                uiUpdate.ReloadBullets();
        }
    }

    public void OnTargetFinished(bool wasKilled, float hitX)
    {
        lastHitX = hitX;

        // Every duck that finishes (hit OR miss) consumes one slot on the HIT meter
        if (uiUpdate != null)
        {
            uiUpdate.SetHitSlot(hitSlotIndex, wasKilled); // hit colors orange, miss stays empty
        }
        hitSlotIndex++;

        // Count attempts (10 spawns per round)
        spawnsThisRound++;

        // Miss tracking for lose condition
        if (!wasKilled)
        {
            missesTotal++;

            if (missesTotal >= maxMissesToLose)
            {
                LoseNow();
                return;
            }
        }

        // If hit, keep existing “Knightro pops up” flow
        if (wasKilled)
        {
            CancelInvoke(nameof(RespawnDuck));

            if (respawnQueued) return;

            respawnQueued = true;
            timer = 0f;

            killsThisRound++;

            // End of round based on spawns, not kills
            if (spawnsThisRound >= spawnsPerRound)
            {
                if (currentRound >= totalRounds)
                {
                    Invoke(nameof(PlayKnightroThenLoadNext), 1f);
                    return;
                }
                else
                {
                    currentRound++;
                    spawnsThisRound = 0;
                    hitSlotIndex = 0;
                    killsThisRound = 0;

                    pendingNextRound = true;
                }
            }

            Invoke(nameof(PlayKnightroAndRespawn), 1f);
        }
        else
        {
            // Miss = no Knightro

            // End of round flow if last target was a miss
            if (spawnsThisRound >= spawnsPerRound)
            {
                if (currentRound >= totalRounds)
                {
                    Invoke(nameof(PlayKnightroThenLoadNext), 1f);
                    return;
                }
                else
                {
                    currentRound++;
                    spawnsThisRound = 0;
                    hitSlotIndex = 0;
                    killsThisRound = 0;

                    // Show next round after delay (no Knightro)
                    Invoke(nameof(BeginNextRound), betweenRoundDelay);
                    return;
                }
            }

            // Normal respawn timing on a miss
            Invoke(nameof(RespawnDuck), spawnInterval);
        }
    }

    void RespawnDuck()
    {
        respawnQueued = false;
        TrySpawnDuck();
    }

    void BeginNextRound()
    {
        if (uiUpdate != null)
        {
            uiUpdate.round = currentRound;
            uiUpdate.currentCollections = 0;
            uiUpdate.ResetHitMeter();

            uiUpdate.RoundUpdate();               // updates small "R = #"
            uiUpdate.ShowRoundPanel(currentRound); // shows big "Round #"
        }

        float popupDelay = (uiUpdate != null) ? uiUpdate.roundPanelShowSeconds : 0f;

        // keep spawning paused while panel is up
        Invoke(nameof(RespawnDuck), popupDelay);
    }

    void PlayKnightroAndRespawn()
    {
        if (knightro != null)
        {
            knightro.PlayLevel1(lastHitX, () =>
            {
                if (pendingNextRound)
                {
                    pendingNextRound = false;
                    Invoke(nameof(BeginNextRound), betweenRoundDelay);
                }
                else
                {
                    Invoke(nameof(RespawnDuck), respawnDelay);
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
                Invoke(nameof(RespawnDuck), respawnDelay);
            }
        }
    }

    void PlayKnightroThenLoadNext()
    {
        if (knightro != null)
        {
            knightro.PlayLevel1(lastHitX, () =>
            {
                if (levelLoader != null) levelLoader.LoadNextLevel();
            });
        }
        else
        {
            if (levelLoader != null) levelLoader.LoadNextLevel();
        }
    }

    void EnableSpawning()
    {
        spawningEnabled = true;
        timer = 0f; // reset so Update doesn’t instantly spawn
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
            Debug.LogError("DuckSpawner loseLevelLoader not assigned (set it in Inspector).");
    }

    Rect GetVisiblePixelRect(Texture2D tex, byte alphaThreshold = 10)
    {
        // Returns the smallest rectangle that contains all pixels with alpha > threshold.
        // alphaThreshold: 0–255 (10 is a good default)

        if (tex == null) return new Rect(0, 0, 0, 0);

        Color32[] pixels = tex.GetPixels32();
        int w = tex.width;
        int h = tex.height;

        int minX = w, minY = h, maxX = -1, maxY = -1;

        for (int y = 0; y < h; y++)
        {
            int row = y * w;
            for (int x = 0; x < w; x++)
            {
                byte a = pixels[row + x].a;
                if (a > alphaThreshold)
                {
                    if (x < minX) minX = x;
                    if (y < minY) minY = y;
                    if (x > maxX) maxX = x;
                    if (y > maxY) maxY = y;
                }
            }
        }

        if (maxX < minX || maxY < minY)
            return new Rect(0, 0, 0, 0); // fully transparent image

        return new Rect(minX, minY, (maxX - minX + 1), (maxY - minY + 1));
    }
}
