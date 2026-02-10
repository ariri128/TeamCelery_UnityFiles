using UnityEngine;

public class ClickButtonAction : MonoBehaviour
{
    public LevelLoader levelLoader;
    public QuitFunc quitFunc;

    public void StartGame()
    {
        if (UIClickSFX.Instance == null)
        {
            levelLoader.LoadNextLevel();
            return;
        }

        UIClickSFX.Instance.PlayClickAndRun(() =>
        {
            levelLoader.LoadNextLevel();
        });
    }

    public void QuitGame()
    {
        if (UIClickSFX.Instance == null)
        {
            // Application.Quit();
            quitFunc.QuitGame();
            return;
        }

        UIClickSFX.Instance.PlayClickAndRun(() =>
        {
            // Application.Quit();
            quitFunc.QuitGame();
        });
    }

    public void RestartGame()
    {
        if (UIClickSFX.Instance == null)
        {
            levelLoader.LoadNextLevel();
            return;
        }

        UIClickSFX.Instance.PlayClickAndRun(() =>
        {
            levelLoader.LoadNextLevel();
        });
    }

    public void MainMenu()
    {
        if (UIClickSFX.Instance == null)
        {
            levelLoader.LoadNextLevel();
            return;
        }

        UIClickSFX.Instance.PlayClickAndRun(() =>
        {
            levelLoader.LoadNextLevel();
        });
    }
}
