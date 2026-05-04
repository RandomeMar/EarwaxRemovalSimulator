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
    private string passFilePath;
    [SerializeField] Canvas statsDeleteCanvas;
    public GameObject statsDeleteButton;
    public GameObject statsDeletedButton;
    public GameObject statsChangePassButton;
    public GameObject statsWrongPassButton;
    public GameObject statsPassChangedButton;
    public GameObject statsPassCheckButton;

    void Start()
    {
        // path of saved password file
        passFilePath = Application.persistentDataPath + "/gate.json";

        // If password file is not found, create a new one with initial password "Cerumen"
        if (!File.Exists(passFilePath)) 
        {   
            PassData passwordData = new PassData();
            passwordData.gatePass.Add("Cerumen");
            string json = JsonUtility.ToJson(passwordData, true);
            File.WriteAllText(passFilePath, json);

            Debug.Log("Password file created\n" + passFilePath);
        }

        // Password file found
        else
        {
            Debug.Log("Password File Found!");
        }
    }

    // Set password enter button to display, and other popup buttons and windows to not display
    public void OnClickClearData()
    {
        statsPassCheckButton.gameObject.SetActive(true);
        statsDeleteButton.gameObject.SetActive(false);
        statsDeletedButton.gameObject.SetActive(false);
        statsWrongPassButton.gameObject.SetActive(false);
        statsChangePassButton.gameObject.SetActive(false);
        statsPassChangedButton.gameObject.SetActive(false);
    }

    // Check if input data matches password
    // Password is stored at path:
    // "C:/Users/ <<your PC username>> /AppData/LocalLow/DefaultCompany/AudiologyUnityProject/gate.json"
    public void PasswordCheck()
    {
        // load saved password file
        if (!File.Exists(passFilePath)) return;
        string jsonString = File.ReadAllText(passFilePath);
        PassData passcheckData = JsonUtility.FromJson<PassData>(jsonString);
        // Debug.Log("CHECK: " + passcheckData.gatePass[0] + " INPUT: " + passInput.text);

        // If password is correct display clear data window to delete all user stats
        if(passInput.text == passcheckData.gatePass[0])
        {
            statsWrongPassButton.gameObject.SetActive(false);
            statsDeleteButton.gameObject.SetActive(true);
            statsChangePassButton.gameObject.SetActive(true);
            statsPassCheckButton.gameObject.SetActive(false);
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

    // Reset password
    // Deletes old password json file and created a new file with a new password
    public void ResetPassword()
    {
        if (File.Exists(passFilePath)) 
        {
            File.Delete(passFilePath);
            Debug.Log("Saved Password file cleared at: " + passFilePath);
            PassData passwordData = new PassData();
            passwordData.gatePass.Add(passInput.text);
            string json = JsonUtility.ToJson(passwordData, true);
            File.WriteAllText(passFilePath, json);
            statsPassChangedButton.gameObject.SetActive(true);
            statsDeleteButton.gameObject.SetActive(false);
            Debug.Log("Password file created\n" + passFilePath);
        }
    }

    // Clear input data after wrong password used or clear data window closed.
    public void ClearPassInput()
    {
        passInput.text = "";
    }

    // Opens deleted data window and closes change password window
    public void DeletedData()
    {
        statsDeletedButton.gameObject.SetActive(true);
        statsChangePassButton.gameObject.SetActive(false);
    }

    // Open clear data window
    public void  OpenConfirm()
    {
        statsDeleteCanvas.gameObject.SetActive(true);
    }

    // Close clear data window 
    public void CloseConfirm()
    {
        // Clear text input box and set all other windows to not display
        ClearPassInput();
        statsDeleteButton.gameObject.SetActive(false);
        statsDeletedButton.gameObject.SetActive(false);
        statsWrongPassButton.gameObject.SetActive(false);
        statsDeleteCanvas.gameObject.SetActive(false);
        statsChangePassButton.gameObject.SetActive(false);
        statsPassChangedButton.gameObject.SetActive(false);
        statsPassCheckButton.gameObject.SetActive(false);
    }
}
