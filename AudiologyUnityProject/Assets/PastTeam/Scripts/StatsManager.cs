using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEngine.LowLevelPhysics2D.PhysicsLayers;


[System.Serializable]
// Single player stat record
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

[System.Serializable]
// List of player stat records
public class StatsData
{
    public List<PlayerStatRecord> playerStatRecords = new List<PlayerStatRecord>();
}


public class StatsManager : MonoBehaviour
{
    private StatsData statsData = new();

    private string filePath;
    private string filePathforDelete;

    public float maxScore = 100f;


    public static StatsManager Instance { get; set; } // Current running stats manager

    public string PlayerName { get; set; }
    public float Score { get; set; }
	public float ElapsedTime { get; set; }

    void Awake() {

        Debug.Log("Get Ear Type: " + int.Parse(PlayerPrefs.GetString("earType")));
        Debug.Log("Get Block Type: " + int.Parse(PlayerPrefs.GetString("blockType")));
        Debug.Log("Get Wax Type: " + int.Parse(PlayerPrefs.GetString("waxType")));

        // Deletes duplicate StatsManagers
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        StatsManager.Instance = this;
        DontDestroyOnLoad(gameObject); // Don't destroy statmanager so it can carry on to next scene.
        Debug.Log("StatsManager has been initialized.");

        filePath = Path.Combine(Application.persistentDataPath, "stats.json");

        Debug.Log("Stats saved at: " + filePath);
        LoadStats();
    }

    public void AddRecord(string playerName, float playerScore, float elapsedTime)
    {
        statsData.playerStatRecords.Add(new PlayerStatRecord(playerName, playerScore, elapsedTime));
        SaveStats();
    }

    public void SaveCurrentRecord()
    {
        if (!string.IsNullOrEmpty(PlayerName))
        {
            statsData.playerStatRecords.Add(new PlayerStatRecord(this.PlayerName, this.Score, this.ElapsedTime));
            SaveStats();
        }
    }

    // For saving player stats to disk
    private void SaveStats()
    {
        string json = JsonUtility.ToJson(statsData, true);
        File.WriteAllText(filePath, json);
        Debug.Log("Saved Stats: " + json + "at location: " + filePath);
    }

    private void LoadStats()
    {
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            statsData = JsonUtility.FromJson<StatsData>(json);

            if (statsData.playerStatRecords.Count > 0)
            {
                // Load the last player's stats
                PlayerStatRecord lastPlayer = statsData.playerStatRecords[statsData.playerStatRecords.Count - 1];

                PlayerName = lastPlayer.name;
                Score = lastPlayer.score;
                ElapsedTime = lastPlayer.time;
            }
        }
    }

    // For deleteing and reseting stats
    public void ResetStats()
    {
        filePathforDelete = Application.persistentDataPath + "/stats.json";
        if (File.Exists(filePathforDelete))
        {
            File.Delete(filePathforDelete);
            Debug.Log("Saved data file cleared at: " + filePathforDelete);
            statsData = new StatsData();
        }
        else
            Debug.Log("No saved data file to delete!");
    }

    // DEPRECATED
    //public float CalculateFinalScore(float percentWaxRemoved, float elapsedTime)
    //{
    //    if (disqualified)
    //        return 0f;

    //    float waxScore = (percentWaxRemoved / 100f) * 50f;
    //    float timeScore = Mathf.Clamp((1f - (elapsedTime / 20f)) * 50f, 0f, 50f);

    //    return Mathf.Clamp(waxScore + timeScore, 0f, maxScore);
    //}
}
