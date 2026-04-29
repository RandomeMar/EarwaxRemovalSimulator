using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class EarSettingsScript : MonoBehaviour
{
    [SerializeField] Text EarSelectedText;
    [SerializeField] Text BlockSeletedText;
    [SerializeField] Text WaxSelectedText;

    [SerializeField]  GameObject LeftEarButton;
    [SerializeField]  GameObject CenterEarButton;
    [SerializeField]  GameObject RightEarButton;

    [SerializeField]  GameObject LeftBlockButton;
    [SerializeField]  GameObject CenterBlockButton;
    [SerializeField]  GameObject RightBlockButton;

    [SerializeField]  GameObject LeftWaxButton;
    [SerializeField]  GameObject CenterWaxButton;
    [SerializeField]  GameObject RightWaxButton;

    private GameObject lastSelectedButton;

    private Color defaultColor = new Color(0.451f, 0.451f, 0.451f);  // #737373
    private Color selectedColor = new Color(0.51f, 0.196f, 0.196f);   // #823232

    void Start()
    {
        LeftEarButton.GetComponent<Image>().color = selectedColor;
        CenterEarButton.GetComponent<Image>().color = defaultColor;
        RightEarButton.GetComponent<Image>().color = defaultColor;

        LeftBlockButton.GetComponent<Image>().color = selectedColor;
        CenterBlockButton.GetComponent<Image>().color = defaultColor;
        RightBlockButton.GetComponent<Image>().color = defaultColor;

        LeftWaxButton.GetComponent<Image>().color = selectedColor;
        CenterWaxButton.GetComponent<Image>().color = defaultColor;
        RightWaxButton.GetComponent<Image>().color = defaultColor;
    }

    // Ear Selection Settings
    public void updateEarLeft()
    {
        PlayerPrefs.SetString("earType", "1");
        EarSelectedText.text = $"Ear Type: 1";
        ResetEarVisuals();
        LeftEarButton.GetComponent<Image>().color = selectedColor;
    }

    public void updateEarCenter()
    {
        PlayerPrefs.SetString("earType", "2");
        EarSelectedText.text = $"Ear Type: 2";
        ResetEarVisuals();
        CenterEarButton.GetComponent<Image>().color = selectedColor;
    }

    public void updateEarRight()
    {
        PlayerPrefs.SetString("earType", "3");
        EarSelectedText.text = $"Ear Type: 3";
        ResetEarVisuals();
        RightEarButton.GetComponent<Image>().color = selectedColor;
    }


    // Ear Blockage Selection Settings
    public void updateBlockLeft()
    {
        PlayerPrefs.SetString("blockType", "1");
        BlockSeletedText.text = $"Block Type: 1";
        ResetBlockVisuals();
        LeftBlockButton.GetComponent<Image>().color = selectedColor;
    }

    public void updateBlockCenter()
    {
        PlayerPrefs.SetString("blockType", "2");
        BlockSeletedText.text = $"Block Type: 2";
        ResetBlockVisuals();
        CenterBlockButton.GetComponent<Image>().color = selectedColor;
    }

    public void updateBlockRight()
    {
        PlayerPrefs.SetString("blockType", "3");
        BlockSeletedText.text = $"Block Type: 3";
        ResetBlockVisuals();
        RightBlockButton.GetComponent<Image>().color = selectedColor;
    }


    // Wax Selection Settings
    public void updateWaxLeft()
    {
        PlayerPrefs.SetString("waxType", "1");
        WaxSelectedText.text = $"Wax Type: 1";
        ResetWaxVisuals();
        LeftWaxButton.GetComponent<Image>().color = selectedColor;
    }

    public void updateWaxCenter()
    {
        PlayerPrefs.SetString("waxType", "2");
        WaxSelectedText.text = $"Wax Type: 2";
        ResetWaxVisuals();
        CenterWaxButton.GetComponent<Image>().color = selectedColor;
    }

    public void updateWaxRight()
    {
        PlayerPrefs.SetString("waxType", "3");
        WaxSelectedText.text = $"Wax Type: 3";
        ResetWaxVisuals();
        RightWaxButton.GetComponent<Image>().color = selectedColor;
    }
    

    private void WaxSelectButton(GameObject button)
    {
        button.GetComponent<Image>().color = selectedColor;
    }

    private void BlockSelectButton(GameObject button)
    {
        button.GetComponent<Image>().color = selectedColor;
    }

    private void EarSelectButton(GameObject button)
    {
        button.GetComponent<Image>().color = selectedColor;
    }

    private void ResetEarVisuals()
    {
        LeftEarButton.GetComponent<Image>().color = defaultColor;
        CenterEarButton.GetComponent<Image>().color = defaultColor;
        RightEarButton.GetComponent<Image>().color = defaultColor;
    }

    private void ResetBlockVisuals()
    {
        LeftBlockButton.GetComponent<Image>().color = defaultColor;
        CenterBlockButton.GetComponent<Image>().color = defaultColor;
        RightBlockButton.GetComponent<Image>().color = defaultColor;
    }

    private void ResetWaxVisuals()
    {
        LeftWaxButton.GetComponent<Image>().color = defaultColor;
        CenterWaxButton.GetComponent<Image>().color = defaultColor;
        RightWaxButton.GetComponent<Image>().color = defaultColor;
    }

}
