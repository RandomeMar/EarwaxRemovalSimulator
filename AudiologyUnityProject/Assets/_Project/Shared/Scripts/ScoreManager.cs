using UnityEngine;
using EarwaxSim;
using System;

public class ScoreManager : MonoBehaviour
{
    public XPBDSim xpbdSim;
    public NewHapticManager hapticManager;
    public StatsManager statsManager;

    public float forceLimit = 100f;

    public float score = 0f;


    private void Update()
    {
        float percentWaxRemoved = 0f;
        Vector3 force = Vector3.zero;

        // Get input values
        if (xpbdSim != null)
        {
            percentWaxRemoved = xpbdSim.GetPercentWaxRemoved();
        }

        if (hapticManager != null)
        {
            force = hapticManager.GetForce();
        }

        if (force.magnitude > forceLimit)
        {
            // Do what ever happens when you mess up
        }

        score = CalculateScore(percentWaxRemoved, 0f);
        statsManager.setScore(score);
    }

    private float CalculateScore(float percentWaxRemoved, float elapsedTime)
    {
        return percentWaxRemoved;
    }
}
