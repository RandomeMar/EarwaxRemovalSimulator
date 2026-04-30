using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ScoreUI : MonoBehaviour
{
    public ScoreManager scoreManager;

    [SerializeField] Text scoreText;
    private bool isRunning = false;
    float score; // TODO: Remove this

    [SerializeField] GameObject statsManager;

    void Update()
    {
        if (isRunning)
        {
            scoreText.text = "Score: " + scoreManager.score.ToString("F1"); // Read score from scoreManager
        }
    }

    public void StartScore()
    {
        isRunning = true;
        score = statsManager.GetComponent<StatsManager>().Score;
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
