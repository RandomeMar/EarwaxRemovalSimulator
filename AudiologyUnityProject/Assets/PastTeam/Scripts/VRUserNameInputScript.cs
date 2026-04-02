using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VRUserNameInputScript : MonoBehaviour 
{
    [SerializeField] InputField inputField;
    [SerializeField] TimerScript timerScript;
    [SerializeField] ClockScript clockScript;
    //[SerializeField] GameObject statsManager;
    [SerializeField] GameObject keyboardUI;
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
                keyboardUI.gameObject.SetActive(false);
                timerUI.gameObject.SetActive(true);

                timerScript.StartTimer();
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
