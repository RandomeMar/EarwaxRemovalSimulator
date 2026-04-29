using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class UnityTestScript
{

    [UnityTest]
    public IEnumerator CheckHeadTypeSetting()
    {

        string head = "Left";
        PlayerPrefs.SetString("headType", "Left");

        // Use the Assert class to test conditions.
        // Use yield to skip a frame.
        Assert.AreEqual(PlayerPrefs.GetString("headType"), head);


        yield return null;
    }

    [UnityTest]
    public IEnumerator CheckCerumenAmountSetting()
    {

        int cerumen = 10;

        PlayerPrefs.SetInt("cerumenAmount", 10);

        // Use the Assert class to test conditions.
        // Use yield to skip a frame.
        Assert.AreEqual(PlayerPrefs.GetInt("cerumenAmount"), cerumen);


        yield return null;
    }

}
