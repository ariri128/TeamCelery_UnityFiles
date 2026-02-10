using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public class CutToNextScene : MonoBehaviour
{
    public PlayableDirector director; // drag Timeline object’s PlayableDirector here
    public string nextSceneName = "Level2_Citronaut";

    private bool loading = false;

    void Awake()
    {
        if (director == null)
            director = GetComponent<PlayableDirector>();
    }

    void OnEnable()
    {
        if (director != null)
            director.stopped += OnTimelineStopped;
    }

    void OnDisable()
    {
        if (director != null)
            director.stopped -= OnTimelineStopped;
    }

    void OnTimelineStopped(PlayableDirector d)
    {
        if (loading) return;
        loading = true;

        SceneManager.LoadScene(nextSceneName);
    }
}
