using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TimerScript : MonoBehaviour
{
    public EarwaxSim.XPBDSim xpbdSim;
    private bool gameOver = false;
    [SerializeField] Text timerText;
    public GameObject XRCameraEar;
    float elapsedTime;
    private bool isRunning = false;


    public string endGameScene;

    [SerializeField] GameObject statsManager;

    void Update()
    {
        if (isRunning && !gameOver)
        {
            elapsedTime += Time.deltaTime;
            int minutes = Mathf.FloorToInt(elapsedTime / 60);
            int seconds = Mathf.FloorToInt(elapsedTime % 60);
            timerText.text = string.Format("Time: {0:00}:{1:00}", minutes, seconds);

            Debug.Log($"Wax removed: {xpbdSim.GetPercentWaxRemoved():F1}%");
            
            if (elapsedTime >= 60f || xpbdSim.GetPercentWaxRemoved() >= 99f)
            {
                elapsedTime = Mathf.Min(elapsedTime, 20f);
                isRunning = false;
                gameOver = true;

                float percentRemoved = xpbdSim.GetPercentWaxRemoved();
                float finalScore = statsManager.GetComponent<StatsManager>().CalculateFinalScore(percentRemoved, elapsedTime);

                statsManager.GetComponent<StatsManager>().setElapsedTime(elapsedTime);
                statsManager.GetComponent<StatsManager>().setScore(finalScore);
                statsManager.GetComponent<StatsManager>().SaveCurrentRecord();
                Debug.Log("EAR SIM OFF\n");

                Destroy(XRCameraEar.gameObject);
                SceneManager.LoadScene(endGameScene);
            }
        }

    }

    public void StartTimer()
    {
        isRunning = true;
    }

    public bool IsTimerRunning()
    {
        return isRunning;
    }

    public float GetElapsedTime()
    {
        return elapsedTime;
    }

}
