using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Responsible for main menu functionality.
/// </summary>
public class MainMenu : MonoBehaviour
{
    [SerializeField] Canvas settingsCanvas;

    private void Start()
    {
        // Old main menu settings
        // PlayerPrefs.SetString("headType", "Center");
        // PlayerPrefs.SetInt("cerumenAmount", 15);

        // New Main Menu settings
        PlayerPrefs.SetString("earType", "1");
        PlayerPrefs.SetString("blockType", "1");
        PlayerPrefs.SetString("waxType", "1");
        settingsCanvas.gameObject.SetActive(false);
    }

    /// <summary>
    /// Start game button.
    /// </summary>
    public void StartGame()
    {
        GameManager.Instance.LoadSimulation();
    }

    /// <summary>
    /// Stats button.
    /// </summary>
    public void GoToStatsScene()
    {
        GameManager.Instance.LoadStatsMenu();
    }

    /// <summary>
    /// Open settings button.
    /// </summary>
    public void  OpenSettings()
    {
        settingsCanvas.gameObject.SetActive(true);
    }

    /// <summary>
    /// Close settings button.
    /// </summary>
    public void CloseSettings()
    {
        settingsCanvas.gameObject.SetActive(false);
    }
}
