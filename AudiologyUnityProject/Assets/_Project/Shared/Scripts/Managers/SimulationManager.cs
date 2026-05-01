using System.Collections.Generic;
using EarwaxSim;
using UnityEngine;

/// <summary>
/// Handler for the simulation.
/// </summary>
/// <remarks>Responsible for setting up game objects when the sim begins running and managing the simulation's time.</remarks>
public class SimulationManager : MonoBehaviour
{
    public static SimulationManager Instance {  get; private set; } // Current running instance

    // ------ Simulation State ------
    public bool IsRunning { get; private set; } = false;

    public string PlayerName { get; private set; } = "";

    public float ElapsedTime { get; private set; } = 0f; // Total elapsed time in seconds
    public int ElapsedMinutes => (int)ElapsedTime / 60; // Minutes component of ElapsedTime
    public int ElapsedSeconds => (int)ElapsedTime % 60; // Seconds component of ElapsedTime

    public float Score { get; private set; } = 0f;


    // ------ Public Parameters ------
    [Header("Score Effectors")]
    public XPBDSim xpbdSim;
    public NewHapticManager hapticManager;
    public float forceLimit = 100f;

    [Header("Disabled/Enabled on Startup")]
    public List<GameObject> disabledOnStartup = new List<GameObject>(4);
    public List<GameObject> enabledOnStartup = new List<GameObject>(3);


    /// <summary>
    /// Initializes singleton.
    /// </summary>
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
    public void StartSimulation(string playerName)
    {
        PlayerName = playerName;

        foreach (GameObject obj in disabledOnStartup)
        {
            obj.SetActive(false);
        }

        foreach (GameObject obj in enabledOnStartup)
        {
            obj.SetActive(true);
        }

        Time.timeScale = 1;

        IsRunning = true;
    }

    /// <summary>
    /// Updates the elapsed time and ends the game after a set amount of time.
    /// </summary>
    private void Update()
    {
        if (!IsRunning) return;

        ElapsedTime += Time.deltaTime; // Increment time

        float percentWaxRemoved = 0f;
        Vector3 force = Vector3.zero;

        // Get input values
        if (xpbdSim != null) percentWaxRemoved = xpbdSim.GetPercentWaxRemoved();
        if (hapticManager != null) force = hapticManager.GetForce();

        // --- END STATE: Force too high ---
        if (force.magnitude > forceLimit)
        {
            Debug.Log("YOU PRESSED TOO HARD!!!");
            Score = 0f;
            EndSimulation();
        }

        Score = CalculateScore(percentWaxRemoved);

        // --- END STATE: Cleared all wax ---
        if (xpbdSim.ps.count == 0)
        {
            Debug.Log("YOU CLEARED ALL THE WAX!!!");
            EndSimulation();
        }

        // --- END STATE: Game lasts 180 seconds ---
        if (ElapsedTime >= 10f)
        {
            IsRunning = false;
            EndSimulation();
        }
    }

    /// <summary>
    /// Ends sim by saving player stats and loading the next scene.
    /// </summary>
    private void EndSimulation()
    {
        StatsManager.Instance.SaveRecord(PlayerName, Score, ElapsedTime);
        Debug.Log("EAR SIM OFF\n");

        GameManager.Instance.LoadResultsMenu();
    }

    /// <summary>
    /// Calculates player score.
    /// </summary>
    /// <param name="percentWaxRemoved">Percentage of wax removed.</param>
    /// <param name="elapsedTime">How much time has passed.</param>
    /// <returns>Score.</returns>
    private float CalculateScore(float percentWaxRemoved)
    {
        // A player can get 100 points for removing all of the wax.
        float score = percentWaxRemoved;

        // A player can receive a max of +10 points for finishing quickly.
        score += ElapsedTime < 60f ? (1 - ElapsedTime / 60f) * 10 : 0f;

        return score;
    }
}
