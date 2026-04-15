using UnityEngine;
using UnityEngine.UI;
using System.IO;
using UnityEngine.Rendering;
using System;
using System.Collections.Generic;

// class of stored password file
public class PassData
{
    public List<string> gatePass = new List<string>();
}

public class StatsDelete : MonoBehaviour
{
    // password input field
    public InputField passInput;
    private string filePath;
    [SerializeField] Canvas statsDeleteCanvas;
    public GameObject statsDeleteButton;
    public GameObject statsWrongPassButton;

    void Start()
    {
        // path of saved password file
        filePath = Application.persistentDataPath + "/gate.json";

        // If password file is not found, create a new one with initial password "Abc123"
        if (!File.Exists(filePath)) 
        {   
            PassData passwordData = new PassData();
            passwordData.gatePass.Add("Abc123");
            string json = JsonUtility.ToJson(passwordData, true);
            File.WriteAllText(filePath, json);

            Debug.Log("Password file created\n" + filePath);
        }
        else
            Debug.Log("Password File Found!");

        // Set wrong and correct password windows to not display
        statsDeleteButton.gameObject.SetActive(false);
        statsWrongPassButton.gameObject.SetActive(false);
    }

    // Check if input data matches password
    // Password is stored at path:
    // "C:/Users/ <<your PC username>> /AppData/LocalLow/DefaultCompany/AudiologyUnityProject/gate.json"
    public void PasswordCheck()
    {
        // load saved password file
        if (!File.Exists(filePath)) return;
        string jsonString = File.ReadAllText(filePath);
        PassData passcheckData = JsonUtility.FromJson<PassData>(jsonString);
        // Debug.Log("CHECK: " + passcheckData.gatePass[0] + " INPUT: " + passInput.text);

        // If password is correct display clear data window to delete all user stats
        if(passInput.text == passcheckData.gatePass[0])
        {
            statsWrongPassButton.gameObject.SetActive(false);
            statsDeleteButton.gameObject.SetActive(true);
        }

        // Wrong password
        else
        {
            // Clear input data and display wrong password window
            ClearPassInput();
            statsWrongPassButton.gameObject.SetActive(true);
            statsDeleteButton.gameObject.SetActive(false);
        }
    }

    // Clear input data 
    public void ClearPassInput()
    {
        passInput.text = "";
    }

    // Open clear data window
    public void  OpenConfirm()
    {
        statsDeleteCanvas.gameObject.SetActive(true);
    }

    // Close clear data window 
    public void CloseConfirm()
    {
        // Clear input window and set all other windows to not display
        ClearPassInput();
        statsDeleteButton.gameObject.SetActive(false);
        statsWrongPassButton.gameObject.SetActive(false);
        statsDeleteCanvas.gameObject.SetActive(false);
    }
}
