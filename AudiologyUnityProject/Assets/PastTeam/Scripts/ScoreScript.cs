using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ScoreScript : MonoBehaviour
{

    [SerializeField] Text scoreText;
    private bool isRunning = false;
    // float elapsedTime;
    float score;
    public string endGameScene;

    [SerializeField] GameObject statsManager;

    void Update()
    {
        if (isRunning)
        {
            scoreText.text = "Score: " + score.ToString("F2");
        }
    }

    public void StartScore()
    {
        isRunning = true;
        score = statsManager.GetComponent<StatsManager>().getScore();
    }

    public bool IsScoreRunning()
    {
        return isRunning;
    }

    public float GetScore()
    {
        return score;
    }


}
