using UnityEngine;
using TMPro;

public class FinalScoreUI : MonoBehaviour
{
    public TMP_Text finalScoreText;

    void Start()
    {
        int score = (ScoreHolder.Instance != null) ? ScoreHolder.Instance.TotalScore : 0;

        if (finalScoreText != null)
            finalScoreText.text = score.ToString();
    }
}
