using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UI;
using UnityEngine.Windows;

/// <summary>
/// Handler for the simulation.
/// </summary>
/// <remarks>Responsible for setting up game objects when the sim begins running and managing the simulation's time.</remarks>
public class SimulationManager : MonoBehaviour
{
    public static SimulationManager Instance {  get; private set; }

    public float ElapsedTime { get; private set; } = 0f; // Total elapsed time in seconds
    public int ElapsedMinutes => (int)ElapsedTime / 60; // Minutes component of ElapsedTime
    public int ElapsedSeconds => (int)ElapsedTime % 60; // Seconds component of ElapsedTime

    [SerializeField] GameObject jake;
    [SerializeField] GameObject keyboardUI;
    [SerializeField] GameObject scoreUI;
    [SerializeField] GameObject timerUI;

    [SerializeField] GameObject XRCameraInitial;
    [SerializeField] GameObject XRCameraManagerInitial;
    [SerializeField] GameObject XRCameraEar;


    public bool IsRunning { get; private set; } = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        SimulationManager.Instance = this;
    }

    /// <summary>
    /// Disables and enables specific game objects to prepare the scene for running.
    /// </summary>
    public void StartSimulation()
    {
        // Disable keyboard, jake, and XR Camera
        keyboardUI.gameObject.SetActive(false);
        XRCameraManagerInitial.gameObject.SetActive(false);
        XRCameraInitial.gameObject.SetActive(false);
        jake.gameObject.SetActive(false);

        XRCameraEar.gameObject.SetActive(true);
        timerUI.gameObject.SetActive(true);
        scoreUI.gameObject.SetActive(true);

        Time.timeScale = 1;

        IsRunning = true;
    }

    /// <summary>
    /// Updates the elapsed time and ends the game after a set amount of time.
    /// </summary>
    private void Update()
    {
        if (!IsRunning) return;

        ElapsedTime += Time.deltaTime;

        // Game ends in 180 seconds
        if (ElapsedSeconds >= 180)
        {
            IsRunning = false;
            GameManager.Instance.EndSimulationRun(ScoreManager.Instance.Score);
        }
    }


}
