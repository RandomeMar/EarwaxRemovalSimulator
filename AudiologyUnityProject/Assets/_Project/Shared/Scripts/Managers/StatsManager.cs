using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEngine.LowLevelPhysics2D.PhysicsLayers;

/// <summary>
/// Single player stat record.
/// </summary>
[System.Serializable]
public class PlayerStatRecord
{
    public string name;
    public float score;
    public float time;

    public PlayerStatRecord() { }
    public PlayerStatRecord(string name, float score, float time)
    {
        this.name = name;
        this.score = score;
        this.time = time;
    }
}

/// <summary>
/// List of player stat records.
/// </summary>
[System.Serializable]
public class StatsData
{
    public List<PlayerStatRecord> playerStatRecords = new List<PlayerStatRecord>();
}


/// <summary>
/// Responsible for loading and saving player stats to disk.
/// </summary>
public class StatsManager : MonoBehaviour
{
    public static StatsManager Instance { get; private set; } // Current running stats manager
    private StatsData statsData = new();

    private string filePath;

    /// <summary>
    /// Initializes StatsManager singleton and establishes path to stats file.
    /// </summary>
    void Awake() {
        // Deletes duplicate StatsManagers
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        StatsManager.Instance = this;
        DontDestroyOnLoad(gameObject); // Don't destroy statmanager so it can carry on to next scene.

        filePath = Path.Combine(Application.persistentDataPath, "stats.json");
        Debug.Log("Stats saved at: " + filePath);
    }

    /// <summary>
    /// Saves player stats to disk.
    /// </summary>
    /// <param name="playerName">Name of the player.</param>
    /// <param name="score">Player's final score.</param>
    /// <param name="elapsedTime">Player's final elapsed time.</param>
    /// <remarks>Saves player stats in JSON format.</remarks>
    public void SaveRecord(string playerName, float score, float elapsedTime)
    {
        statsData.playerStatRecords.Add(new PlayerStatRecord(playerName, score, elapsedTime));

        string json = JsonUtility.ToJson(statsData, true);
        File.WriteAllText(filePath, json);
        Debug.Log("Saved Stats: " + json + "at location: " + filePath);
    }

    /// <summary>
    /// Loads player data for the last player who was saved.
    /// </summary>
    /// <param name="lastPlayer">A collection of stats about the last player.</param>
    /// <returns>Whether or not the load was successful.</returns>
    public bool LoadLastPlayer(out PlayerStatRecord lastPlayer)
    {
        lastPlayer = null;

        if (!File.Exists(filePath)) return false;

        string json = File.ReadAllText(filePath);
        StatsData statsData = JsonUtility.FromJson<StatsData>(json);

        if (statsData == null || statsData.playerStatRecords.Count <= 0) return false;

        // Load the last player's stats
        lastPlayer = statsData.playerStatRecords[statsData.playerStatRecords.Count - 1];

        return true;
    }

    /// <summary>
    /// Loads all player data.
    /// </summary>
    /// <param name="statsData">List of player stats.</param>
    /// <returns>Whether or not the load was successful.</returns>
    public bool LoadStatsData(out StatsData statsData)
    {
        statsData = null;

        if (!File.Exists(filePath)) return false;

        string json = File.ReadAllText(filePath);
        statsData = JsonUtility.FromJson<StatsData>(json);

        if (statsData == null || statsData.playerStatRecords.Count <= 0) return false;

        return true;
    }

    /// <summary>
    /// Deletes stats file.
    /// </summary>
    public void ResetStats()
    {
        if (!File.Exists(filePath))
        {
            Debug.Log("No saved data file to delete!");
            return;
        }

        File.Delete(filePath);
        Debug.Log("Saved data file cleared at: " + filePath);
        statsData = new StatsData();
    }
}
