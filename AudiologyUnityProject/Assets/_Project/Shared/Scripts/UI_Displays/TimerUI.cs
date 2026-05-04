using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Timer UI element.
/// </summary>
public class TimerUI : MonoBehaviour
{
    [SerializeField] Text timerText;

    /// <summary>
    /// When the simulation is running, updates the UI text to match the simulation's elapsed time.
    /// </summary>
    void Update()
    {
        if (SimulationManager.Instance.IsRunning)
        {
            timerText.text = string.Format("Time: {0:00}:{1:00}", SimulationManager.Instance.ElapsedMinutes, SimulationManager.Instance.ElapsedSeconds);
        }
    }

    /// <summary>
    /// DEPRECATED
    /// </summary>
    public bool IsTimerRunning()
    {
        return false;
    }

    /// <summary>
    /// DEPRECATED
    /// </summary>
    public float GetElapsedTime()
    {
        return 0;
    }

}
