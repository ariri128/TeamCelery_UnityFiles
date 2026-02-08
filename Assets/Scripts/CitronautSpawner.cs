using UnityEngine;

public class CitronautSpawner : MonoBehaviour
{
    public GameObject citronautPrefab;
    public PlayerShooter shooter;
    public KnightroController knightro;
    public UIUpdateScript uiUpdate;

    public LevelLoader winLevelLoader;     // set nextSceneName to your WinScene (if using a scene)
    public bool useWinScene = true;        // if false, it will just stop and log win (if using UI panel)

    public Sprite[] citronautRoundSprites; // different Citronauts for each round

    public Transform spawnLine;              // Set this to your SpawnLine object
    public float spawnInterval = 1.25f;

    public int maxAlive = 1;

    public float firstSpawnDelay = 1.5f;   // fast spawn at level start
    public float respawnDelay = 3f;       // gap for Knightro animation after Citronaut ends

    private bool respawnQueued = false;

    public float horizontalPadding = 0.5f;

    private float timer;

    private float lastHitX;

    public int killsPerRound = 10;
    public int totalRounds = 3;

    private int currentRound = 1;
    private int killsThisRound = 0;

    void Start()
    {
        currentRound = 1;
        killsThisRound = 0;

        if (uiUpdate != null)
        {
            uiUpdate.round = currentRound;
            uiUpdate.currentCollections = 0;
            uiUpdate.HideCitronautHits();
            uiUpdate.RoundUpdate();
            uiUpdate.ReloadBullets();
        }

        Invoke(nameof(TrySpawnOne), firstSpawnDelay);
    }

    void Update()
    {
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

        // Set sprite based on current round
        SpriteRenderer sr = citronaut.GetComponent<SpriteRenderer>();
        if (sr != null && citronautRoundSprites != null && citronautRoundSprites.Length > 0)
        {
            int index = Mathf.Clamp(currentRound - 1, 0, citronautRoundSprites.Length - 1);
            if (citronautRoundSprites[index] != null)
                sr.sprite = citronautRoundSprites[index];
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

        if (wasKilled)
        {
            if (respawnQueued) return;

            respawnQueued = true;
            timer = 0f;

            //uiUpdate.currentCollections++;
            /*if (uiUpdate.currentCollections <= 10)
                uiUpdate.ShowCitronautHits();*/

            killsThisRound++;

            if (uiUpdate != null)
            {
                uiUpdate.currentCollections = killsThisRound;
                uiUpdate.ShowCitronautHits();
            }

            // Rounds
            if (killsThisRound >= killsPerRound)
            {
                if (currentRound >= totalRounds)
                {
                    // WIN after Round 3
                    Invoke(nameof(PlayKnightroThenWin), 1f);
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
                        uiUpdate.HideCitronautHits();
                        uiUpdate.RoundUpdate();
                    }
                }
            }

            Invoke(nameof(PlayKnightroAndRespawn), 1f);
        }
        else
        {
            Invoke(nameof(RespawnOne), spawnInterval);
        }
    }

    void RespawnOne()
    {
        respawnQueued = false;
        TrySpawnOne();
    }

    void PlayKnightroAndRespawn()
    {
        if (knightro != null)
        {
            knightro.PlayLevel2(lastHitX, () =>
            {
                Invoke(nameof(RespawnOne), respawnDelay);
            });
        }
        else
        {
            Invoke(nameof(RespawnOne), respawnDelay);
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

    void WinNow()
    {
        Debug.Log("YOU WIN!");

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
}
