using UnityEngine;
using System.Collections.Generic;
using System.IO;
using UnityEngine.SceneManagement;

[System.Serializable]
public class StatsData
{
    public List<string> playerNames = new List<string>();
    public List<float> playerScores = new List<float>();
    public List<float> times = new List<float>();
}

public class StatsManager : MonoBehaviour
{
    private StatsData statsData = new StatsData();
    private string filePath;

    private bool disqualified = false;
    public float maxScore = 100f;

    private string filePathforDelete;

    public string currentName { get; set; }
    public float currentScore;
	public float currentElapsedTime;

    void Awake() {

        Debug.Log("Get Ear Type: " + int.Parse(PlayerPrefs.GetString("earType")));
        Debug.Log("Get Block Type: " + int.Parse(PlayerPrefs.GetString("blockType")));
        Debug.Log("Get Wax Type: " + int.Parse(PlayerPrefs.GetString("waxType")));

        // This is meant to destroy all old Statmanagers
        StatsManager[] all = FindObjectsByType<StatsManager>(FindObjectsSortMode.None);
        if (all.Length > 1)
        {
            for (int i = 0; i < all.Length - 1; i++)
            {
                Destroy(all[i].gameObject);
            }
        }

        // Don't destroy statmanager so it can carry on to next scene.
        DontDestroyOnLoad(gameObject);
        Debug.Log("StatsManager has been initialized.");
        filePath = Application.persistentDataPath + "/stats.json";
        Debug.Log("Stats saved at: " + filePath);
        LoadStats();
    }

    public void AddRecord(string playerName, float playerScore, float elapsedTime)
    {
        statsData.playerNames.Add(playerName);
        statsData.playerScores.Add(playerScore);
        statsData.times.Add(elapsedTime);
        SaveStats();
    }

    public void setName(string n) {
		currentName = n;
	}

    public void setScore(float score) {
		currentScore = score;
	}

	public float getScore() {
		return currentScore;
	}

    public void setElapsedTime(float time)
    {
        currentElapsedTime = time;
    }

    public float getElapsedTime()
    {
        return currentElapsedTime;
    }

    public void SaveCurrentRecord()
    {
        if (!string.IsNullOrEmpty(currentName))
        {
            statsData.playerNames.Add(currentName);
            statsData.playerScores.Add(currentScore);
            statsData.times.Add(currentElapsedTime);
            SaveStats();
        }
    }

    public List<string> GetPlayerNames()
    {
        return statsData.playerNames;
    }

    public List<float> GetScores()
    {
        return statsData.playerScores;
    }

    public List<float> GetTimes()
    {
        return statsData.times;
    }

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

            if (statsData.playerNames.Count > 0)
            {
                currentName = statsData.playerNames[statsData.playerNames.Count - 1];
                currentScore = statsData.playerScores[statsData.playerScores.Count - 1];
                currentElapsedTime = statsData.times[statsData.times.Count - 1];
            }
        }
    }

    // New scoring algorithm methods
    
    public void InitializeScore()
    {
        currentScore = maxScore;
        disqualified = false;
    }

    public void Disqualify()
    {
        disqualified = true;
        Debug.Log("[StatsManager] Player disqualified - too much pressure!");
    }

    public bool IsDisqualified() => disqualified;

    //public float CalculateFinalScore(float percentWaxRemoved, float elapsedTime)
    //{
    //    if (disqualified)
    //        return 0f;

    //    float waxScore = (percentWaxRemoved / 100f) * 50f;
    //    float timeScore = Mathf.Clamp((1f - (elapsedTime / 20f)) * 50f, 0f, 50f);

    //    return Mathf.Clamp(waxScore + timeScore, 0f, maxScore);
    //}

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


}
