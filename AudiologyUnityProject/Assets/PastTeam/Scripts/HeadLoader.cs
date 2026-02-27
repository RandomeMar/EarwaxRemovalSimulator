using UnityEngine;

public class HeadLoader : MonoBehaviour
{
    [SerializeField] GameObject leftHead;
    [SerializeField] GameObject rightHead;
    [SerializeField] GameObject head;
    void Start()
    {
        string headType = PlayerPrefs.GetString("headType");
        if (headType == "Right")
        {
            head.SetActive(false);
            rightHead.SetActive(true);
        }
        else if (headType == "Left")
        {
            head.SetActive(false);
            leftHead.SetActive(true);
        }
        else
        {
            head.SetActive(true);
            leftHead.SetActive(false);
            rightHead.SetActive(false);
        }
    }
}
