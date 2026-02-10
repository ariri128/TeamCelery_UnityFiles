using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class UIUpdateScript : MonoBehaviour
{
    public TMP_Text scoreText;
    public TMP_Text roundText;

    public GameObject roundPanel;
    public TMP_Text roundPanelText;
    public float roundPanelShowSeconds = 1.5f;

    public GameObject gameHUDRoot;
    public GameObject levelPanel;
    public TMP_Text levelPanelText;
    public float levelIntroShowSeconds = 3f;

    public GameObject knightroWhistle;
    public GameObject pegasusJump;
    public GameObject pegasusWing;
    public GameObject hitMarker;

    public GameObject[] bulletsUI = new GameObject[3]; //All bullet UI
    public Image[] hitUI = new Image[10]; //All hit UI

    public Sprite duckUncollected;
    public Sprite duckCollected;

    public Sprite citronautMiss;
    public Sprite citronautHit;

    public int currentCollections = 0; //Tracks number of collected items
    public int score = 0;
    public int bulletsUsed = 0;
    public int round = 0;
    public int currentlevel = 1;

    public DuckSpawner duckSpawner;
    public PlayerShooter playerShooter;

    // Whistle audio for the level intro
    public AudioClip whistleClip;
    public float whistleVolume = 0.6f;

    private AudioSource whistleSource;

    // Loop toggle
    public bool loopWhistleDuringIntro = true;

    // Knightro vibrate settings
    public float knightroShakeAmount = 2f;
    public float knightroShakeSpeed = 40f; // higher = faster shake

    // Pegasus wing flap settings (NOW GAMEOBJECTS)
    public GameObject pegasusWingOpen;    // assign the OPEN wing GameObject
    public GameObject pegasusWingClosed;  // assign the CLOSED wing GameObject
    public float wingFlapInterval = 0.12f; // seconds between toggles

    private Coroutine introShakeCoroutine;
    private Coroutine wingFlapCoroutine;

    // Store original positions to be able to reset cleanly
    private Vector3 knightroOriginalPos;
    private bool hasKnightroOriginalPos = false;

    // Start is called before the first frame update
    void Awake()
    {
        Scene activeLevel = SceneManager.GetActiveScene();
        string levelName = activeLevel.name;

        if (levelName == "Level1_Ducks")
        {
            currentlevel = 1;
            ResetHitMeter();
        }
        else if (levelName == "Level2_Citronaut")
        {
            currentlevel = 2;
            ResetHitMeter();

            // Store Level 1 score as the Level 2 restart checkpoint (only once)
            if (ScoreHolder.Instance != null)
                ScoreHolder.Instance.SaveLevel2CheckpointIfNeeded();
        }

        if (ScoreHolder.Instance != null)
        {
            score = ScoreHolder.Instance.TotalScore;
            ScoreUpdate();
        }

        round = 1;
        RoundUpdate();

        if (roundPanel != null)
            roundPanel.SetActive(false);

        if (levelPanel != null)
            levelPanel.SetActive(false);
    }

    public void ScoreUpdate()
    {
        scoreText.text = "" + score;
    }

    public void RoundUpdate()
    {
        roundText.text = "R = " + round;
    }

    public void BulletShot()
    {
        switch (bulletsUsed)
        {
            case 3:
                bulletsUI[0].SetActive(false);
                break;

            case 2:
                bulletsUI[1].SetActive(false);
                break;

            case 1:
                bulletsUI[2].SetActive(false);
                break;
        }
    }

    public void ReloadBullets()
    {
        for (int i = 0; i < 3; i++)
        {
            bulletsUI[i].SetActive(true);
        }
    }

    public void ShowCollectedDucks()
    {
        for (int i = 0; i < currentCollections; i++)
        {
            hitUI[i].sprite = duckCollected;
        }
    }

    public void HideAllDucks()
    {
        for (int i = 0; i < 10; i++)
        {
            hitUI[i].sprite = duckUncollected;
        }
    }

    public void ShowCitronautHits()
    {
        for (int i = 0; i < currentCollections; i++)
        {
            hitUI[i].sprite = citronautHit;
        }
    }

    public void HideCitronautHits()
    {
        for (int i = 0; i < 10; i++)
        {
            hitUI[i].sprite = citronautMiss;
        }
    }

    public void UIResetLevel1()
    {
        ReloadBullets();
    }

    public void ShowRoundPanel(int roundNumber)
    {
        if (roundPanel == null || roundPanelText == null) return;

        StopAllCoroutines();
        StartCoroutine(ShowRoundPanelRoutine(roundNumber));
    }

    IEnumerator ShowRoundPanelRoutine(int roundNumber)
    {
        roundPanel.SetActive(true);
        roundPanelText.text = "Round " + roundNumber;

        yield return new WaitForSeconds(roundPanelShowSeconds);

        roundPanel.SetActive(false);
    }

    public void PlayLevelIntro(System.Action onDone)
    {
        StopAllCoroutines();
        StartCoroutine(LevelIntroRoutine(onDone));
    }

    IEnumerator LevelIntroRoutine(System.Action onDone)
    {
        // Hide HUD + hitmarker during intro
        if (gameHUDRoot != null) gameHUDRoot.SetActive(false);
        if (hitMarker != null) hitMarker.SetActive(false);

        // Show only level panel + mascots
        if (levelPanel != null) levelPanel.SetActive(true);
        if (knightroWhistle != null) knightroWhistle.SetActive(true);
        if (pegasusJump != null) pegasusJump.SetActive(true);
        if (pegasusWing != null) pegasusWing.SetActive(true);

        // Ensure wing starts in OPEN state (GameObjects)
        if (pegasusWingOpen != null) pegasusWingOpen.SetActive(true);
        if (pegasusWingClosed != null) pegasusWingClosed.SetActive(false);

        // Capture originals for restoring
        CacheKnightroOriginalPos();

        // Start effects
        StartIntroShake();
        StartWingFlap();

        // Play whistle sound during intro (loop if desired)
        if (whistleClip != null)
        {
            EnsureWhistleSource();
            whistleSource.Stop();
            whistleSource.volume = whistleVolume;
            whistleSource.clip = whistleClip;
            whistleSource.loop = loopWhistleDuringIntro;
            whistleSource.Play();
        }

        // Set correct level number
        if (levelPanelText != null)
            levelPanelText.text = "Level " + currentlevel;

        yield return new WaitForSeconds(levelIntroShowSeconds);

        // Stop audio + effects
        StopIntroShake();
        StopWingFlap();

        if (whistleSource != null)
            whistleSource.Stop();

        // Hide the intro stuff
        if (levelPanel != null) levelPanel.SetActive(false);
        if (knightroWhistle != null) knightroWhistle.SetActive(false);
        if (pegasusJump != null) pegasusJump.SetActive(false);
        if (pegasusWing != null) pegasusWing.SetActive(false);

        // Also hide both wing states cleanly
        if (pegasusWingOpen != null) pegasusWingOpen.SetActive(false);
        if (pegasusWingClosed != null) pegasusWingClosed.SetActive(false);

        // Bring back HUD + hitmarker
        if (gameHUDRoot != null) gameHUDRoot.SetActive(true);
        if (hitMarker != null) hitMarker.SetActive(true);

        if (onDone != null)
            onDone();
    }

    void CacheKnightroOriginalPos()
    {
        if (knightroWhistle == null) return;
        if (hasKnightroOriginalPos) return;

        knightroOriginalPos = knightroWhistle.transform.localPosition;
        hasKnightroOriginalPos = true;
    }

    void StartIntroShake()
    {
        if (knightroWhistle == null) return;
        StopIntroShake();
        introShakeCoroutine = StartCoroutine(IntroShakeRoutine());
    }

    void StopIntroShake()
    {
        if (introShakeCoroutine != null)
        {
            StopCoroutine(introShakeCoroutine);
            introShakeCoroutine = null;
        }

        if (knightroWhistle != null && hasKnightroOriginalPos)
            knightroWhistle.transform.localPosition = knightroOriginalPos;
    }

    IEnumerator IntroShakeRoutine()
    {
        if (knightroWhistle == null) yield break;
        if (!hasKnightroOriginalPos) CacheKnightroOriginalPos();

        while (true)
        {
            float ox = Mathf.Sin(Time.time * knightroShakeSpeed) * knightroShakeAmount;
            float oy = Mathf.Cos(Time.time * knightroShakeSpeed * 1.1f) * knightroShakeAmount;

            knightroWhistle.transform.localPosition = knightroOriginalPos + new Vector3(ox, oy, 0f);
            yield return null;
        }
    }

    void StartWingFlap()
    {
        StopWingFlap();
        wingFlapCoroutine = StartCoroutine(WingFlapRoutine());
    }

    void StopWingFlap()
    {
        if (wingFlapCoroutine != null)
        {
            StopCoroutine(wingFlapCoroutine);
            wingFlapCoroutine = null;
        }

        // Restore to OPEN wing when done
        if (pegasusWingOpen != null) pegasusWingOpen.SetActive(true);
        if (pegasusWingClosed != null) pegasusWingClosed.SetActive(false);
    }

    IEnumerator WingFlapRoutine()
    {
        if (pegasusWingOpen == null || pegasusWingClosed == null) yield break;

        bool open = true;

        while (true)
        {
            pegasusWingOpen.SetActive(open);
            pegasusWingClosed.SetActive(!open);

            open = !open;
            yield return new WaitForSeconds(wingFlapInterval);
        }
    }

    public void ResetHitMeter()
    {
        for (int i = 0; i < hitUI.Length; i++)
        {
            if (currentlevel == 1)
                hitUI[i].sprite = duckUncollected;
            else
                hitUI[i].sprite = citronautMiss;
        }
    }

    public void SetHitSlot(int slotIndex, bool wasHit)
    {
        if (slotIndex < 0 || slotIndex >= hitUI.Length) return;

        if (currentlevel == 1)
            hitUI[slotIndex].sprite = wasHit ? duckCollected : duckUncollected;
        else
            hitUI[slotIndex].sprite = wasHit ? citronautHit : citronautMiss;
    }

    void EnsureWhistleSource()
    {
        if (whistleSource != null) return;

        whistleSource = GetComponent<AudioSource>();
        if (whistleSource == null)
            whistleSource = gameObject.AddComponent<AudioSource>();

        whistleSource.playOnAwake = false;
        whistleSource.loop = false;
        whistleSource.spatialBlend = 0f; // 2D
    }
}
