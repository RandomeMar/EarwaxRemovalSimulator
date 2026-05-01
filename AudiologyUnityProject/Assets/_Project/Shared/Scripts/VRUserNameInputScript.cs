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
        // NOTE: This should probably be handled by a SimulationSceneManager or something.

        GameObject statsManager = GameObject.Find("StatsManager");
        string input = inputField.text;

        if (!string.IsNullOrEmpty(input))
        {
            if (statsManager != null)
            {                
                keyboardUI.gameObject.SetActive(false);

                timerUI.gameObject.SetActive(true);
                scoreUI.gameObject.SetActive(true);

                // Disable inital camera and enable ear sim Camera and ear model
                XRCameraEar.gameObject.SetActive(true);
                XRCameraManagerInitial.gameObject.SetActive(false);
                XRCameraInitial.gameObject.SetActive(false);
                jake.gameObject.SetActive(false);

                timerScript.StartTimer();
                scoreScript.StartScore();
                clockScript.StartClock();

                GameManager.Instance.StartSimulationRun(input);
            }
            else
            {
                Debug.LogWarning("StatsManager not found or has been destroyed.");
            }
        }
    }
}
