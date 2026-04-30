using UnityEngine;
using UnityEngine.SceneManagement;

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


    public void LoadStartMenu()
    {
        State = GameState.StartMenu;
        SceneManager.LoadScene(startScene);
    }

    public void LoadSimulation()
    {
        State = GameState.SimSetup;
        SceneManager.LoadScene(simulationScene);
    }

    public void StartSimulationRun()
    {
        State = GameState.SimRunning;
        // DO whatever to start the simulation
    }

    public void EndSimulationRun()
    {
        State = GameState.ResultsMenu;
        SceneManager.LoadScene(endScene);
    }

    public void LoadStatsMenu()
    {
        State = GameState.StatsMenuScene;
        SceneManager.LoadScene(statsScene);
    }

    public void QuitGame()
    {
        Application.Quit();

        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

}
