using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class NewTestScript
{

    [UnityTest]
    public IEnumerator CheckEarTypeSetting()
    {

        string ear = "3";
        PlayerPrefs.SetString("earType", "3");

        // Use the Assert class to test conditions.
        // Use yield to skip a frame.
        Assert.AreEqual(PlayerPrefs.GetString("earType"), ear);


        yield return null;
    }

    [UnityTest]
    public IEnumerator CheckBlockTypeSetting()
    {

        string block = "2";
        PlayerPrefs.SetString("blockType", "2");

        // Use the Assert class to test conditions.
        // Use yield to skip a frame.
        Assert.AreEqual(PlayerPrefs.GetString("blockType"), block);


        yield return null;
    }

    [UnityTest]
    public IEnumerator CheckWaxTypeSetting()
    {

        string wax = "1";
        PlayerPrefs.SetString("waxType", "1");

        // Use the Assert class to test conditions.
        // Use yield to skip a frame.
        Assert.AreEqual(PlayerPrefs.GetString("waxType"), wax);


        yield return null;
    }

    // [UnityTest]
    // public IEnumerator CheckCerumenAmountSetting()
    // {

    //     int cerumen = 15;

    //     PlayerPrefs.SetInt("cerumenAmount", 15);

    //     // Use the Assert class to test conditions.
    //     // Use yield to skip a frame.
    //     Assert.AreEqual(PlayerPrefs.GetInt("cerumenAmount"), cerumen);


    //     yield return null;
    // }

    //[UnityTest]
    //public IEnumerator CheckSceneLoading()
    //{

    //    string endScene = "EndScene";

    //    EditorSceneManager.OpenScene(endScene);

        
       

    //    // Use the Assert class to test conditions.
    //    // Use yield to skip a frame.
    //    Assert.AreEqual(EditorSceneManager.GetSceneByName("endScene").isLoaded, true);


    //    yield return null;
    //}
}
