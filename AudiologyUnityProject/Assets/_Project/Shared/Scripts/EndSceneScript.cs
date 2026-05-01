using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class EndSceneScript : MonoBehaviour
{
    public Text introText;
    public Text timeText;
    public Text scoreText;
    
    /// <summary>
    /// Loads last player's stats to display.
    /// </summary>
    private void Start()
    {
        if (!StatsManager.Instance.LoadLastPlayer(out var lastPlayer)) return;

        int minutes = Mathf.FloorToInt(lastPlayer.time / 60);
        int seconds = Mathf.FloorToInt(lastPlayer.time % 60);
        string formattedTime = string.Format("{0:00}:{1:00}", minutes, seconds);

        introText.text = $"Wax On, Wax Gone!\r\nHere's how you did, {lastPlayer.name}:";
        timeText.text = $"Time Taken: {formattedTime}";
        scoreText.text = $"Score: {lastPlayer.score:F1} pts";
    }
    public void BackToMainMenu()
    {
        GameManager.Instance.LoadStartMenu();
    }

    public void GoToStatsScene()
    {
        GameManager.Instance.LoadStatsMenu();
    }
}
