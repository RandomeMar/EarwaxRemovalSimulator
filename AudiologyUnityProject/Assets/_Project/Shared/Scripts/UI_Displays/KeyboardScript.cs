using UnityEngine;
using UnityEngine.UI;

public class KeyboardScript : MonoBehaviour
{
    public InputField inputField;
    public Text textField;

    public void OnButtonPress()
    {
        if (inputField.text.Length < 10)
        {
            inputField.text += textField.text;
        }
    }

    public void OnBackspacePress()
    {
        if (inputField.text.Length > 0)
        {
            inputField.text = inputField.text.Substring(0, inputField.text.Length - 1);
        }
    }
}
