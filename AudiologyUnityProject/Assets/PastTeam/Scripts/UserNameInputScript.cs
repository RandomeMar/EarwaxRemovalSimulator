using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UserNameInputScript : MonoBehaviour 
{
    [SerializeField] InputField inputField;
    //[SerializeField] Text testText;
    [SerializeField] Text resultText;
    [SerializeField] Button submitButton;
    [SerializeField] TimerScript timerScript;
    [SerializeField] GameObject statsManager;
    [SerializeField] ClockScript clockScript;
    

    public void ValidateInput()
    {
        string input = inputField.text;
        //testText.text = PlayerPrefs.GetString("headType");

        if (!string.IsNullOrEmpty(input))
        {
            if (statsManager != null)
            {
                //resultText.text = input;
                statsManager.GetComponent<StatsManager>().setName(input);
                resultText.text = statsManager.GetComponent<StatsManager>().getName();
                inputField.gameObject.SetActive(false);
                submitButton.gameObject.SetActive(false);

                clockScript.StartClock();
                timerScript.StartTimer();
                Time.timeScale = 1;
            }
            else
            {
                Debug.LogWarning("StatsManager not found or has been destroyed.");
            }
        }

   
    }
    
}
