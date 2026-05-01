using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UserNameInputScript : MonoBehaviour 
{
    [SerializeField] InputField inputField;
    

    public void ValidateInput()
    {
        string input = inputField.text;

        if (!string.IsNullOrEmpty(input))
        {
            GameManager.Instance.StartSimulationRun(input);
        }

   
    }
    
}
