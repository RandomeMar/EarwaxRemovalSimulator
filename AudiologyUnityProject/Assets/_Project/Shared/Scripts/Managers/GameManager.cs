using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Windows;

public enum GameState
{
    StartMenu,
    StatsMenuScene,
    SimSetup,
    ResultsMenu
}

/// <summary>
/// Manager of scene transitions and game state.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState State { get; private set; }

    [Header("Scene Names")]
    public string startScene = "StartSceneVR";
    public string statsScene = "StatsSceneVR";
    public string simulationScene = "SimulationSceneVR";
    public string endScene = "EndSceneVR";
    
    /// <summary>
    /// Initializes singleton.
    /// </summary>
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

    /// <summary>
    /// Loads the start menu.
    /// </summary>
    /// <remarks>Also deletes any StatsManagers.</remarks>
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
    /// Loads the simulation scene.
    /// </summary>
    public void LoadSimulation()
    {
        State = GameState.SimSetup;
        SceneManager.LoadScene(simulationScene);
    }

    /// <summary>
    /// Loads the results menu.
    /// </summary>
    public void LoadResultsMenu()
    {
        State = GameState.ResultsMenu;
        SceneManager.LoadScene(endScene);
    }

    /// <summary>
    /// Loads the stats menu.
    /// </summary>
    public void LoadStatsMenu()
    {
        State = GameState.StatsMenuScene;
        SceneManager.LoadScene(statsScene);
    }

    /// <summary>
    /// Ends the game.
    /// </summary>
    public void QuitGame()
    {
        Application.Quit();

        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

}
