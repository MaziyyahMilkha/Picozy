using UnityEngine;

public class ResultUIManager : MonoBehaviour
{
    public GameObject darkOverlay;
    public GameObject winPanel;
    public GameObject losePanel;
    public GameObject[] stars;

    void Start()
    {
        bool isWin = PlayerPrefs.GetInt("RESULT_WIN", 0) == 1;
        int starCount = PlayerPrefs.GetInt("RESULT_STARS", 0);

        if (darkOverlay != null)
            darkOverlay.SetActive(true);

        if (winPanel != null)
            winPanel.SetActive(isWin);

        if (losePanel != null)
            losePanel.SetActive(!isWin);

        for (int i = 0; i < stars.Length; i++)
            stars[i].SetActive(isWin && i < starCount);
    }
}
