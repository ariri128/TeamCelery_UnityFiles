using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelLoader : MonoBehaviour
{
    public string nextSceneName;

    public void LoadNextLevel()
    {
        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName, LoadSceneMode.Single);
        else
            Debug.LogError("LevelLoader nextSceneName is empty. Set it in the Inspector.");
    }
}