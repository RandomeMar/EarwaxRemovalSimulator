using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
public class MainMenu : MonoBehaviour
{
    [SerializeField] Canvas settingsCanvas;
    public string startGameScene;
    public string statsScene;

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
    public void StartGame()
    {
        SceneManager.LoadScene(startGameScene);
    }

    public void GoToStatsScene()
    {
        SceneManager.LoadScene(statsScene);
    }

    public void  OpenSettings()
    {
        settingsCanvas.gameObject.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsCanvas.gameObject.SetActive(false);
    }
}
