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

    void LoadStats(string nameFilter = "")
    {
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        if (!File.Exists(filePath)) return;

        string json = File.ReadAllText(filePath);
        StatsData statsData = JsonUtility.FromJson<StatsData>(json);

        List<(string name, float time)> combined = new List<(string, float)>();

        for (int i = 0; i < statsData.playerNames.Count; i++)
        {
            string player = statsData.playerNames[i];
            float time = statsData.times[i];

            if (!string.IsNullOrEmpty(nameFilter) &&
                !player.ToLower().Contains(nameFilter.ToLower()))
            {
                continue;
            }

            combined.Add((player, time));
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

        foreach (var (player, time) in combined)
        {
            GameObject row = Instantiate(statEntryRowPrefab, contentParent);
            row.transform.Find("PlayerNameText").GetComponent<Text>().text = player;
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


    private string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    public void BackToMainMenu()
    {
        SceneManager.LoadScene(mainMenuScene);
    }
}