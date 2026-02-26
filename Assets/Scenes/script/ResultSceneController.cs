using UnityEngine;
using System.Collections;

public class ResultSceneController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject loadingPanel;
    public GameObject berandaPanel;
    public GameObject winPanel;

    void Start()
    {
        loadingPanel.SetActive(true);
        berandaPanel.SetActive(false);
        winPanel.SetActive(false);

        StartCoroutine(ShowResult());
    }

    IEnumerator ShowResult()
    {
        yield return new WaitForSeconds(2f); // durasi loading

        loadingPanel.SetActive(false);

        int isWin = PlayerPrefs.GetInt("RESULT_WIN", 0);

        if (isWin == 1)
        {
            winPanel.SetActive(true);
        }
        else
        {
            berandaPanel.SetActive(true);
        }
    }
}
