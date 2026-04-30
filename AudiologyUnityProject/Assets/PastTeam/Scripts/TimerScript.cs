using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TimerScript : MonoBehaviour
{
    [SerializeField] Text timerText;
    public GameObject XRCameraEar;
    float elapsedTime;
    private bool isRunning = false;


    public string endGameScene;

    [SerializeField] GameObject statsManager;

    void Update()
    {
        if (isRunning)
        {
            elapsedTime += Time.deltaTime;
            int minutes = Mathf.FloorToInt(elapsedTime / 60);
            int seconds = Mathf.FloorToInt(elapsedTime % 60);
            timerText.text = string.Format("Time: {0:00}:{1:00}", minutes, seconds);

            // Game ends in 20 seconds
            if (elapsedTime >= 20f)
            {
                isRunning = false;

                statsManager.GetComponent<StatsManager>().ElapsedTime = elapsedTime;
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
