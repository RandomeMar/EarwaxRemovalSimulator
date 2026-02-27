using UnityEngine;
using System.Collections.Generic;
using System.IO;
using UnityEngine.SceneManagement;

[System.Serializable]
public class StatsData
{
    public List<string> playerNames = new List<string>();
    public List<float> times = new List<float>();
}

public class StatsManager : MonoBehaviour
{
    private StatsData statsData = new StatsData();
    private string filePath;

    public string currentName;

	public float currentElapsedTime;

    void Awake() {
        StatsManager[] all = FindObjectsByType<StatsManager>(FindObjectsSortMode.None);
        if (all.Length > 1)
        {
            for (int i = 0; i < all.Length - 1; i++)
            {
                Destroy(all[i].gameObject);
            }
        }

        DontDestroyOnLoad(gameObject);
        Debug.Log("StatsManager has been initialized.");
        filePath = Application.persistentDataPath + "/stats.json";
        Debug.Log("Stats saved at: " + filePath);
        LoadStats();
    }

    public void AddRecord(string playerName, float elapsedTime)
    {
        statsData.playerNames.Add(playerName);
        statsData.times.Add(elapsedTime);
        SaveStats();
    }

    public void setName(string n) {
		currentName = n;
	}

	public string getName() {
		return currentName;
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
            statsData.times.Add(currentElapsedTime);
            SaveStats();
        }
    }

    public List<string> GetPlayerNames()
    {
        return statsData.playerNames;
    }

    public List<float> GetTimes()
    {
        return statsData.times;
    }

    private void SaveStats()
    {
        string json = JsonUtility.ToJson(statsData, true);
        File.WriteAllText(filePath, json);
        Debug.Log("Saved Stats: " + json);
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
                currentElapsedTime = statsData.times[statsData.times.Count - 1];
            }
        }
    }

    // For deleteing and reseting stats, saved for later
    //public void ResetStats()
    //{
    //    statsData = new StatsData();
    //    File.Delete(filePath);    
    //}


}
