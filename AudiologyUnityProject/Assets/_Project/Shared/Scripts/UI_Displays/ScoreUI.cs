using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Score UI element.
/// </summary>
public class ScoreUI : MonoBehaviour
{
    [SerializeField] Text scoreText;

    /// <summary>
    /// If simulation is running, updates score UI text.
    /// </summary>
    void Update()
    {
        if (SimulationManager.Instance.IsRunning)
        {
            scoreText.text = "Score: " + SimulationManager.Instance.Score.ToString("F1"); // Read score from ScoreManager
        }
    }


}
