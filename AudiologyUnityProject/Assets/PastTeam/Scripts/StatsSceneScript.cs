using UnityEngine;
using UnityEngine.UI;
using System.IO;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;

public class StatsSceneScript : MonoBehaviour
{
    public Transform contentParent;
    public GameObject statEntryRowPrefab;
    public InputField searchInput;
    public InputField searchVRInput;
    public Dropdown sortDropdown;

    private string filePath;

    private enum SortMode { HighestScore, Fastest, LastCompleted}
    private SortMode currentSortMode = SortMode.HighestScore;

    void Start()
    {
        // Stat data is stored at path:
        // "C:/Users/ <<your PC username>> /AppData/LocalLow/DefaultCompany/AudiologyUnityProject/stats.json"
        filePath = Application.persistentDataPath + "/stats.json";

        // Starts the drop down menu listener
        if (sortDropdown != null)
        {
            sortDropdown.onValueChanged.RemoveAllListeners();
            sortDropdown.onValueChanged.AddListener(OnSortOptionChanged);
        }
        LoadStats();
    }

    // Used for keyboard only, not VR keyboard (only one input field allowed per search box)
    public void OnSearchButtonPressed()
    {
        string input = searchInput.text;
        LoadStats(input);
    }

    // Used for VR keyboard only, not keyboard (only one input field allowed per search box)
    public void OnSearchButtonVRPressed()
    {
        string inputVR = searchVRInput.text;
        LoadStats(inputVR);
    }

    public void LoadStats(string nameFilter = "")
    {
        // Destroy all previous instances of stat data loaded
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        // Reads from stored stat data to build a list of Playername, Playerscore, and times
        if (!File.Exists(filePath)) return;

        string json = File.ReadAllText(filePath);
        StatsData statsData = JsonUtility.FromJson<StatsData>(json);

        List<(string name, float score, float time)> combined = new List<(string, float, float)>();

        // Builds full list of stat data
        for (int i = 0; i < statsData.playerStatRecords.Count; i++)
        {
            PlayerStatRecord playerRecord = statsData.playerStatRecords[i];

            string playerName = playerRecord.name;
            float score = playerRecord.score;
            float time = playerRecord.time;
            // Debug.Log("Player Stats: " + player + score + time);
            if (!string.IsNullOrEmpty(nameFilter) &&
                !playerName.ToLower().Contains(nameFilter.ToLower()))
            {
                continue;
            }

            combined.Add((playerName, score, time));
        }

        // Switch statement for the drop down menu
        switch (currentSortMode)
        {
            case SortMode.HighestScore:
                combined.Sort((a, b) => b.score.CompareTo(a.score));
                break;
            case SortMode.Fastest :
                combined.Sort((a, b) => a.time.CompareTo(b.time));
                break;
            case SortMode.LastCompleted:
                combined.Reverse();
                break;
        }

        // Reduce combine list to 5 and fill in user data in list
        int count = 0;
        foreach (var (player, score, time) in combined)
        {
            if (count >= 5)
                break;
            GameObject row = Instantiate(statEntryRowPrefab, contentParent);
            row.transform.Find("PlayerNameText").GetComponent<Text>().text = player;
            row.transform.Find("PlayerScoreText").GetComponent<Text>().text = FormatScore(score);
            row.transform.Find("TimeText").GetComponent<Text>().text = FormatTime(time);
            row.SetActive(true);
            count++;
        }
    }

    // The drop down menu
    public void OnSortOptionChanged(int index)
    {
        Debug.Log("Dropdown changed: " + index);
        currentSortMode = (SortMode)index;
        LoadStats(searchInput.text);
    }

    // Formats score to 1 decimal place
    private string FormatScore(float score)
    {
        return score.ToString("F1");
    }

    // Formats time 
    private string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    // Back to main menu button ends all StatManger game objects from running and loads the main scene
    public void BackToMainMenu()
    {
        //// Destroy all old StatManagers as a new Statmanager is made to track new player stats
        //StatsManager[] all = FindObjectsByType<StatsManager>(FindObjectsSortMode.None);
        //if (all.Length > 0)
        //{
        //    for (int i = 0; i < all.Length; i++)
        //    {
        //        Destroy(all[i].gameObject);
        //    }
        //}
        //SceneManager.LoadScene(mainMenuScene);

        GameManager.Instance.LoadStartMenu();
    }
}