using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIUpdateScript : MonoBehaviour
{
    public TMP_Text scoreText;
    public TMP_Text roundText;

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

    //public GameControlScript gameControlScript; //Update with correct script name

    public DuckSpawner duckSpawner;
    public PlayerShooter playerShooter;


    // Start is called before the first frame update
    void Awake()
    {
        scoreText.text = "000000";
        ReloadBullets();
        ShowCollectedDucks(); //remove when testing level 2
        round = 1;
        RoundUpdate();
    }

    //Updates score text
    public void ScoreUpdate() 
    {
        scoreText.text = "" + score; //Update with correct call name
    }

    //Updates round text
    public void RoundUpdate() 
    {
        roundText.text = "R = " + round; //Update with correct call name
    }

    //Removes used bullet UI
    public void BulletShot()
    {
        switch (bulletsUsed) //update with bullet number call
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

    //Shows all bullet UI
    public void ReloadBullets()
    {
        for (int i = 0; i < 3; i++)
        {
            bulletsUI[i].SetActive(true);
        }
    }

    //Shows collected ducks
    public void ShowCollectedDucks()
    {
        for (int i = 0; i < currentCollections; i++)
        {
            hitUI[i].sprite = duckCollected;
        }
                
    }
       

    //Hides all ducks
    public void HideAllDucks()
    {
        for (int i = 0; i < 10; i++)
        {
            hitUI[i].sprite = duckUncollected;
        }
    }
    
    //Shows all Citronaut hits
    public void ShowCitronautHits()
    {
        for (int i = 0; i < currentCollections; i++)
        {
            hitUI[i].sprite = citronautHit;
        }
       
    }

    //Hides all Citronaut hit UI
    public void HideCitronautHits()
    {
        for (int i = 0; i < 10; i++)
        {
            hitUI[i].sprite = citronautMiss;
        }
    }

    public void UIResetLevel1()
    {
        //HideAllDucks();
        ReloadBullets();
        currentCollections = 0;
    }


}
