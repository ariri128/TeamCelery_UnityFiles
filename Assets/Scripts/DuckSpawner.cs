using UnityEngine;

public class DuckSpawner : MonoBehaviour
{
    public GameObject duckPrefab;
    public Sprite[] duckSprites;
    public float targetVisibleDuckHeight = 0.35f;

    public PlayerShooter shooter;
    public KnightroController knightro;
    public LevelLoader levelLoader;
    public UIUpdateScript uiUpdate;

    public float spawnInterval = 1.5f;
    public int maxDucksAlive = 1;

    public float firstSpawnDelay = 1.5f;   // fast spawn when level starts
    public float respawnDelay = 3f;       // gap for Knightro animation after a duck ends

    private bool respawnQueued = false;

    // The world Y where the ducks float (top of waves)
    public float waterLineY = -2f;

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

    void Start()
    {
        cam = Camera.main;

        currentRound = 1;
        killsThisRound = 0;

        if (uiUpdate != null)
        {
            uiUpdate.round = currentRound;
            uiUpdate.currentCollections = killsThisRound;
            uiUpdate.HideAllDucks();
            uiUpdate.RoundUpdate();
            uiUpdate.ReloadBullets();
        }

        // Spawn quickly at the start of the level
        Invoke(nameof(TrySpawnDuck), firstSpawnDelay);
    }

    void Update()
    {
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
        float y = waterLineY + Random.Range(-waterLineRandomRange, waterLineRandomRange);

        GameObject duck = Instantiate(duckPrefab, new Vector3(x, y, 0f), Quaternion.identity);

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

        if (wasKilled)
        {
            if (respawnQueued) return;

            respawnQueued = true;
            timer = 0f;

            killsThisRound++;

            if (uiUpdate != null)
            {
                uiUpdate.currentCollections = killsThisRound;
                uiUpdate.ShowCollectedDucks();
            }

            if (killsThisRound >= killsPerRound)
            {
                if (currentRound >= totalRounds)
                {
                    // Finished Level 1 Round 3 -> go to Level 2
                    Invoke(nameof(PlayKnightroThenLoadNext), 1f);
                    return;
                }
                else
                {
                    // Next round
                    currentRound++;
                    killsThisRound = 0;

                    if (uiUpdate != null)
                    {
                        uiUpdate.round = currentRound;
                        uiUpdate.currentCollections = 0;
                        uiUpdate.HideAllDucks();
                        uiUpdate.RoundUpdate();
                    }
                }
            }

            Invoke(nameof(PlayKnightroAndRespawn), 1f);
        }
        else
        {
            // Missed all 3 shots -> no Knightro
            Invoke(nameof(RespawnDuck), spawnInterval);
        }
    }

    void RespawnDuck()
    {
        respawnQueued = false;
        TrySpawnDuck();
    }

    void PlayKnightroAndRespawn()
    {
        if (knightro != null)
        {
            knightro.PlayLevel1(lastHitX, () =>
            {
                Invoke(nameof(RespawnDuck), respawnDelay);
            });
        }
        else
        {
            Invoke(nameof(RespawnDuck), respawnDelay);
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
