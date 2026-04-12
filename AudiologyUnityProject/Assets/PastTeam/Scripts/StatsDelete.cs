using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
public class StatsDelete : MonoBehaviour
{
    [SerializeField] Canvas statsDeleteCanvas;

    private void Start()
    {
        statsDeleteCanvas.gameObject.SetActive(false);
    }

    public void  OpenConfirm()
    {
        statsDeleteCanvas.gameObject.SetActive(true);
    }

    public void CloseConfirm()
    {
        statsDeleteCanvas.gameObject.SetActive(false);
    }
}
