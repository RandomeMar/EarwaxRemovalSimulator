using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VRUserNameInputScript : MonoBehaviour 
{
    [SerializeField] InputField inputField;
    [SerializeField] TimerScript timerScript;
    [SerializeField] ScoreScript scoreScript;
    [SerializeField] ClockScript clockScript;
    //[SerializeField] GameObject statsManager;
    [SerializeField] GameObject keyboardUI;
    [SerializeField] GameObject scoreUI;
    [SerializeField] GameObject timerUI;

    public void ValidateInput()
    {
        GameObject statsManager = GameObject.Find("StatsManager");
        string input = inputField.text;

        if (!string.IsNullOrEmpty(input))
        {
            if (statsManager != null)
            {
                statsManager.GetComponent<StatsManager>().setName(input);
                statsManager.GetComponent<StatsManager>().setScore(0);
                keyboardUI.gameObject.SetActive(false);

                timerUI.gameObject.SetActive(true);
                scoreUI.gameObject.SetActive(true);

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
