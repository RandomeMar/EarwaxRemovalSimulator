using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class VRUserNameInputScript : MonoBehaviour 
{
    [SerializeField] InputField inputField;

    /// <summary>
    /// Ensures text input is valid and calls to start the simulation
    /// </summary>
    public void ValidateInput()
    {
        string input = inputField.text;

        if (!string.IsNullOrEmpty(input))
        {
            SimulationManager.Instance.StartSimulation(input);
        }
    }
}
