using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class NewTestScript
{

    [UnityTest]
    public IEnumerator CheckHeadTypeSetting()
    {

        string head = "Right";
        PlayerPrefs.SetString("headType", "Right");

        // Use the Assert class to test conditions.
        // Use yield to skip a frame.
        Assert.AreEqual(PlayerPrefs.GetString("headType"), head);


        yield return null;
    }

    [UnityTest]
    public IEnumerator CheckCerumenAmountSetting()
    {

        int cerumen = 15;

        PlayerPrefs.SetInt("cerumenAmount", 15);

        // Use the Assert class to test conditions.
        // Use yield to skip a frame.
        Assert.AreEqual(PlayerPrefs.GetInt("cerumenAmount"), cerumen);


        yield return null;
    }

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
