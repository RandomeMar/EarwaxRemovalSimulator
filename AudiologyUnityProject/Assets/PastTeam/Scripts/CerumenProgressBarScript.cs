using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CerumenProgressBarScript : MonoBehaviour
{
    public GameObject bar;

    [SerializeField] TimerScript timerScript;

    private float elapsedTime = 0f;

    public float maxTime = 10f;

    void Update()
    {
        if (timerScript.IsTimerRunning()) 
        {
            elapsedTime = timerScript.GetElapsedTime();
            UpdateProgressBar();
        }
    }

    void UpdateProgressBar()
    {
        float progress = Mathf.Clamp01(elapsedTime / maxTime);

        bar.transform.localScale = new Vector3(progress, 1f, 1f);  
    }

}
