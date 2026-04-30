using UnityEngine;
using EarwaxSim;
using System;

public class ScoreManager : MonoBehaviour
{
    public XPBDSim xpbdSim;
    public NewHapticManager hapticManager;

    public float forceLimit = 100f;

    public float Score {  get; private set; }

    public static ScoreManager Instance {  get; private set; } // Current running ScoreManager

    // Calculates score.
    private float CalculateScore(float percentWaxRemoved, float elapsedTime)
    {
        // A player can get 100 points for removing all of the wax.
        float score = percentWaxRemoved;

        // A player can receive a max of +10 points for finishing quickly.
        score += elapsedTime < 60f ? elapsedTime / 60f : 0f;

        return score;
    }


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // Constantly checks for if the wax count was updated and if the force against the wall of the canal was too high.
    private void Update()
    {
        float percentWaxRemoved = 0f;
        Vector3 force = Vector3.zero;

        // Get input values
        if (xpbdSim != null) percentWaxRemoved = xpbdSim.GetPercentWaxRemoved();
        if (hapticManager != null) force = hapticManager.GetForce();

        if (force.magnitude > forceLimit)
        {
            // Do what ever happens when you mess up
            Debug.Log("YOU PRESSED TOO HARD!!!");


        }

        Score = CalculateScore(percentWaxRemoved, StatsManager.Instance.ElapsedTime);
        StatsManager.Instance.Score = Score;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
