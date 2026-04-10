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
    public Dropdown sortDropdown;

    private string filePath;
    public string mainMenuScene;

    private enum SortMode { LastCompleted, Fastest, Slowest }
    private SortMode currentSortMode = SortMode.LastCompleted;

    void Start()
    {
        
        filePath = Application.persistentDataPath + "/stats.json";

        if (sortDropdown != null)
        {
            sortDropdown.onValueChanged.RemoveAllListeners();
            sortDropdown.onValueChanged.AddListener(OnSortOptionChanged);
        }

        LoadStats();
    }

    public void OnSearchButtonPressed()
    {
        string input = searchInput.text;
        LoadStats(input);
    }

    public void LoadStats(string nameFilter = "")
    {
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        if (!File.Exists(filePath)) return;

        string json = File.ReadAllText(filePath);
        StatsData statsData = JsonUtility.FromJson<StatsData>(json);

        List<(string name, float score, float time)> combined = new List<(string, float, float)>();

        for (int i = 0; i < statsData.playerNames.Count; i++)
        {
            string player = statsData.playerNames[i];
            float score = statsData.playerScores[i];
            float time = statsData.times[i];
            Debug.Log("Player Stats: " + player + score + time);
            if (!string.IsNullOrEmpty(nameFilter) &&
                !player.ToLower().Contains(nameFilter.ToLower()))
            {
                continue;
            }

            combined.Add((player, score, time));
        }

        switch (currentSortMode)
        {
            case SortMode.Fastest:
                combined.Sort((a, b) => a.time.CompareTo(b.time));
                break;
            case SortMode.Slowest:
                combined.Sort((a, b) => b.time.CompareTo(a.time));
                break;
            case SortMode.LastCompleted:
                break;
        }

        foreach (var (player, score, time) in combined)
        {
            GameObject row = Instantiate(statEntryRowPrefab, contentParent);
            row.transform.Find("PlayerNameText").GetComponent<Text>().text = player;
            row.transform.Find("PlayerScoreText").GetComponent<Text>().text = FormatScore(score);
            row.transform.Find("TimeText").GetComponent<Text>().text = FormatTime(time);
            row.SetActive(true);
        }
    }

    public void OnSortOptionChanged(int index)
    {
        Debug.Log("Dropdown changed: " + index);
        currentSortMode = (SortMode)index;
        LoadStats(searchInput.text);
    }


    private string FormatScore(float score)
    {
        return score.ToString("F2");
    }


    private string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    public void BackToMainMenu()
    {
        // Destroy all old StatManagers as a new Statmanager is made to track new player stats
        StatsManager[] all = FindObjectsByType<StatsManager>(FindObjectsSortMode.None);
        if (all.Length > 0)
        {
            for (int i = 0; i < all.Length; i++)
            {
                Destroy(all[i].gameObject);
            }
        }
        SceneManager.LoadScene(mainMenuScene);
    }
}