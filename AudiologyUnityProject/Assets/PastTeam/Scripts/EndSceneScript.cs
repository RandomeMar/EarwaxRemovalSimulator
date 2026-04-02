using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class EndSceneScript : MonoBehaviour
{
    public string mainMenuScene;
    public string statsScene;
    public Text introText;
    public Text timeText;
    
    private void Start()
    {
        GameObject statsManager = GameObject.Find("StatsManager");

        if (statsManager != null )
        {
            string name = statsManager.GetComponent<StatsManager>().getName();
            float elapsedTime = statsManager.GetComponent<StatsManager>().getElapsedTime();

            int minutes = Mathf.FloorToInt(elapsedTime / 60);
            int seconds = Mathf.FloorToInt(elapsedTime % 60);
            string formattedTime = string.Format("{0:00}:{1:00}", minutes, seconds);

            introText.text = $"Wax On, Wax Gone!\r\nHere's how you did, {name}:";
            timeText.text = $"Time Taken: {formattedTime}";
        }


    }
    public void BackToMainMenu()
    {
        SceneManager.LoadScene(mainMenuScene);
    }

    public void GoToStatsScene()
    {
        SceneManager.LoadScene(statsScene); 
    }
}
