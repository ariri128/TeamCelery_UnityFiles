using UnityEngine;

public class ScoreHolder : MonoBehaviour
{
    public static ScoreHolder Instance { get; private set; }

    public int TotalScore { get; private set; }

    // Checkpoint for restarting Level 2
    public int Level2CheckpointScore { get; private set; }
    private bool hasLevel2Checkpoint = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ResetScore()
    {
        TotalScore = 0;
    }

    public void AddScore(int amount)
    {
        TotalScore += amount;
    }

    // Call this when Level 2 is entered the first time
    public void SaveLevel2CheckpointIfNeeded()
    {
        if (hasLevel2Checkpoint) return;

        Level2CheckpointScore = TotalScore; // Score after finishing Level 1
        hasLevel2Checkpoint = true;
    }

    // Call this when restarting Level 2 from Lose Scene 2
    public void RestoreLevel2Checkpoint()
    {
        if (!hasLevel2Checkpoint) return;

        TotalScore = Level2CheckpointScore;
    }

    // Use this for Start Screen / New Run
    public void ResetAllForNewRun()
    {
        TotalScore = 0;
        Level2CheckpointScore = 0;
        hasLevel2Checkpoint = false;
    }
}
