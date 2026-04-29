using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class EndSceneScript : MonoBehaviour
{
    public string mainMenuScene;
    public string statsScene;
    public Text introText;
    public Text timeText;
    public Text scoreText;
    
    private void Start()
    {
        GameObject statsManager = GameObject.Find("StatsManager");

        if (statsManager != null )
        {
            string name = statsManager.GetComponent<StatsManager>().currentName;
            float score = statsManager.GetComponent<StatsManager>().getScore();
            float elapsedTime = statsManager.GetComponent<StatsManager>().getElapsedTime();

            int minutes = Mathf.FloorToInt(elapsedTime / 60);
            int seconds = Mathf.FloorToInt(elapsedTime % 60);
            string formattedTime = string.Format("{0:00}:{1:00}", minutes, seconds);

            introText.text = $"Wax On, Wax Gone!\r\nHere's how you did, {name}:";
            timeText.text = $"Time Taken: {formattedTime}";
            scoreText.text = $"Score: {score:F1} pts";
        }


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

    public void GoToStatsScene()
    {
        SceneManager.LoadScene(statsScene); 
    }
}
