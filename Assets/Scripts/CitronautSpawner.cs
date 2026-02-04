using UnityEngine;

public class CitronautSpawner : MonoBehaviour
{
    public GameObject citronautPrefab;
    public PlayerShooter shooter;
    public KnightroController knightro;

    public Transform spawnLine;              // Set this to your SpawnLine object
    public float spawnInterval = 1.25f;

    public int maxAlive = 1;

    public float firstSpawnDelay = 1.5f;   // fast spawn at level start
    public float respawnDelay = 3f;       // gap for Knightro animation after citronaut ends

    private bool respawnQueued = false;

    // Optional: If your spaceship blocks part of the bottom, you can shrink width a bit
    public float horizontalPadding = 0.5f;

    private float timer;

    private float lastHitX;

    void Start()
    {
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
                SpawnOne();
        }
    }

    int CountAlive()
    {
        // Simple approach: find active floaters
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
            SpawnOne();
    }

    public void OnTargetFinished(bool wasKilled, float hitX)
    {
        lastHitX = hitX;

        if (wasKilled)
        {
            if (respawnQueued) return;

            respawnQueued = true;
            timer = 0f;

            Invoke(nameof(PlayKnightroAndRespawn), 1f);
        }
        else
        {
            // Missed all 3 shots -> no Knightro, normal pacing
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
}
