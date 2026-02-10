using UnityEngine;

public class ResetScore : MonoBehaviour
{
    public void ResetScoreForNewRun()
    {
        if (ScoreHolder.Instance != null)
            ScoreHolder.Instance.ResetAllForNewRun();
    }
}
