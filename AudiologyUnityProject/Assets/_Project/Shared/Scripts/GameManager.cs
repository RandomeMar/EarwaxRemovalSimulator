using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Windows;

public enum GameState
{
    StartMenu,
    StatsMenuScene,
    SimSetup,
    SimRunning,
    ResultsMenu
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState State { get; private set; }

    [Header("Scene Names")]
    public string startScene = "StartSceneVR";
    public string statsScene = "StatsSceneVR";
    public string simulationScene = "SimulationSceneVR";
    public string endScene = "EndSceneVR";
    

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        GameManager.Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Loads the start menu
    public void LoadStartMenu()
    {
        if (StatsManager.Instance != null)
        {
            Destroy(StatsManager.Instance.gameObject);
        }

        State = GameState.StartMenu;
        SceneManager.LoadScene(startScene);
    }

    /// <summary>
    /// Loads the simulation scene
    /// </summary>
    public void LoadSimulation()
    {
        State = GameState.SimSetup;
        SceneManager.LoadScene(simulationScene);
    }

    /// <summary>
    /// Begins the simulation
    /// </summary>
    public void StartSimulationRun(string playerName)
    {
        State = GameState.SimRunning;
        StatsManager.Instance.PlayerName = playerName;
        StatsManager.Instance.Score = 0;

        Time.timeScale = 1;
    }

    /// <summary>
    /// Ends the simulation and loads the results screen
    /// </summary>
    public void EndSimulationRun(float score)
    {
        StatsManager.Instance.Score = score;
        StatsManager.Instance.SaveCurrentRecord();
        Debug.Log("EAR SIM OFF\n");

        State = GameState.ResultsMenu;
        SceneManager.LoadScene(endScene);
    }

    /// <summary>
    /// Loads the stats menu
    /// </summary>
    public void LoadStatsMenu()
    {
        State = GameState.StatsMenuScene;
        SceneManager.LoadScene(statsScene);
    }

    /// <summary>
    /// Ends the game
    /// </summary>
    public void QuitGame()
    {
        Application.Quit();

        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

}
