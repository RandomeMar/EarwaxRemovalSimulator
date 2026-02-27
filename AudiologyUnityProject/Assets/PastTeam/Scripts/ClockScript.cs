using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; 

public class ClockScript : MonoBehaviour
{
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text pauseText;
    [SerializeField] private TMP_Text pauseButtonText;
    private float elapsedTime;
    private bool isRunning = false;

    public string endGameScene;

    //[SerializeField] private GameObject statsManager;
    
    public string startGameScene;

    public void StartGame()
    {
        SceneManager.LoadScene(startGameScene);
    }

    void Start() {
        
    }

    public void ResetButton()
    {
/*         StatsManager[] allManagers = FindObjectsByType<StatsManager>(FindObjectsSortMode.None);

        foreach (StatsManager sm in allManagers)
        {
            DestroyImmediate(sm.gameObject);  // instead of Destroy()
        } */
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        Time.timeScale = 1;
    }

    void Update()
    {
        if (isRunning)
        {
            GameObject statsManager = GameObject.Find("StatsManager");
            pauseText.text = "Pause";
            elapsedTime += Time.deltaTime;

            int minutes = Mathf.FloorToInt(elapsedTime / 60);
            int seconds = Mathf.FloorToInt(elapsedTime % 60);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);


            statsManager.GetComponent<StatsManager>().setElapsedTime(elapsedTime);


    
        }
    }

    public void StartClock()
    {
        isRunning = true;
    }

    public bool IsClockRunning()
    {
        return isRunning;
    }

    public float GetElapsedTime()
    {
        return elapsedTime;
    }

    public void ExitSimulation()
    {
        GameObject statsManager = GameObject.Find("StatsManager");
        Debug.Log("Exiting simulation");
        isRunning = false;
        statsManager.GetComponent<StatsManager>().SaveCurrentRecord();
        SceneManager.LoadScene(endGameScene);
    }

    public void PauseClock()
    {
        isRunning = !isRunning;

        if (isRunning)
        {
            Debug.Log("Clock resumed");
            pauseText.text = "Pause";
            pauseButtonText.text = "Pause";
            pauseText.color = Color.green;
        }
        else
        {
            Debug.Log("Clock paused");
            pauseText.text = "Resume";
            pauseButtonText.text = "Resume";
            pauseText.color = Color.yellow;
        }
    }
}
