using UnityEngine;

public class ResetScoreLvl2 : MonoBehaviour
{
    public void RestartLevel2()
    {
        if (ScoreHolder.Instance != null)
            ScoreHolder.Instance.RestoreLevel2Checkpoint();
    }
}
