using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SettingsScript : MonoBehaviour
{
    [SerializeField] Text CerumenText;
    [SerializeField] Slider cerumenSlider;
    [SerializeField] Text CanalText;

    
    [SerializeField]  GameObject LeftCanalButton;
    [SerializeField]  GameObject CenterCanalButton;
    [SerializeField]  GameObject RightCanalButton;

    private GameObject lastSelectedButton;

    private Color defaultColor = new Color(0.451f, 0.451f, 0.451f);  // #737373
    private Color selectedColor = new Color(0.51f, 0.196f, 0.196f);   // #823232

    void Start()
    {
        LeftCanalButton.GetComponent<Image>().color = defaultColor;
        CenterCanalButton.GetComponent<Image>().color = selectedColor;
        RightCanalButton.GetComponent<Image>().color = defaultColor;
    }


    public void updateCerumen()
    {
        PlayerPrefs.SetInt("cerumenAmount", (int) cerumenSlider.value);
        CerumenText.text = $"Amount of Cerumen: {PlayerPrefs.GetInt("cerumenAmount")}";
    }

    public void updateHeadLeft()
    {
        PlayerPrefs.SetString("headType", "Left");
        CanalText.text = $"Canal Type: 1";
        ResetVisuals();
        LeftCanalButton.GetComponent<Image>().color = selectedColor;
    }

    public void updateHeadCenter()
    {
        PlayerPrefs.SetString("headType", "Center");
        CanalText.text = $"Canal Type: 2";
        ResetVisuals();
        CenterCanalButton.GetComponent<Image>().color = selectedColor;
    }

    public void updateHeadRight()
    {
        PlayerPrefs.SetString("headType", "Right");
        CanalText.text = $"Canal Type: 3";
        ResetVisuals();
        RightCanalButton.GetComponent<Image>().color = selectedColor;
    }
    

    private void SelectButton(GameObject button)
    {
        button.GetComponent<Image>().color = selectedColor;
    }

    private void ResetVisuals()
    {
        LeftCanalButton.GetComponent<Image>().color = defaultColor;
        CenterCanalButton.GetComponent<Image>().color = defaultColor;
        RightCanalButton.GetComponent<Image>().color = defaultColor;
    }

}
