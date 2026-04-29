using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class VRUserNameInputScript : MonoBehaviour 
{
    [SerializeField] InputField inputField;
    [SerializeField] TimerScript timerScript;
    [SerializeField] ScoreUI scoreScript;
    [SerializeField] ClockScript clockScript;
    //[SerializeField] GameObject statsManager;
    [SerializeField] GameObject keyboardUI;
    [SerializeField] GameObject jake;
    [SerializeField] GameObject scoreUI;
    [SerializeField] GameObject timerUI;

    public GameObject XRCameraInitial;
    public GameObject XRCameraManagerInitial;
    public GameObject XRCameraEar;

    public void ValidateInput()
    {

        GameObject statsManager = GameObject.Find("StatsManager");
        string input = inputField.text;

        if (!string.IsNullOrEmpty(input))
        {
            if (statsManager != null)
            {
                // After user inputs name into keyboardUI, set score to 0 and start clock and timer.
                // Activate clock and timer game ogjects and deactivate keyboardUI game object
                statsManager.GetComponent<StatsManager>().setName(input);
                statsManager.GetComponent<StatsManager>().setScore(0);
                keyboardUI.gameObject.SetActive(false);

                timerUI.gameObject.SetActive(true);
                scoreUI.gameObject.SetActive(true);

                // Disbale inital camera and enable ear sim Camera and ear model
                XRCameraEar.gameObject.SetActive(true);
                XRCameraManagerInitial.gameObject.SetActive(false);
                XRCameraInitial.gameObject.SetActive(false);
                jake.gameObject.SetActive(false);

                timerScript.StartTimer();
                scoreScript.StartScore();
                clockScript.StartClock();
                Time.timeScale = 1;
            }
            else
            {
                Debug.LogWarning("StatsManager not found or has been destroyed.");
            }
        }
    }
}
