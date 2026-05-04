using UnityEngine;
using TMPro; 

/// <summary>
/// Clock display updater.
/// </summary>
public class ClockDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text pauseText;
    [SerializeField] private TMP_Text pauseButtonText;


    /// <summary>
    /// When the simulation is running, updates the clock's text to match the simulation's elapsed time.
    /// </summary>
    void Update()
    {
        if (!SimulationManager.Instance.IsRunning) return;

        timerText.text = string.Format("{0:00}:{1:00}", SimulationManager.Instance.ElapsedMinutes, SimulationManager.Instance.ElapsedSeconds);
    }
}
