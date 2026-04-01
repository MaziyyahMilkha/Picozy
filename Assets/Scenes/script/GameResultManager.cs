using UnityEngine;
using UnityEngine.SceneManagement;

public class GameResultManager : MonoBehaviour
{
    public GameTimer gameTimer;
    private bool gameEnded = false;

    void Update()
    {
        if (gameEnded) return;

        // ⏰ TIME HABIS → KALAH
        if (gameTimer != null && gameTimer.IsTimeUp())
        {
            Lose();
        }
    }

    // 🏆 DIPANGGIL DARI GameManager SAAT LEVEL COMPLETE
    public void Win()
    {
        if (gameEnded) return;
        gameEnded = true;

        int stars = 0;
        if (gameTimer != null)
        {
            stars = gameTimer.GetStarCount();
            gameTimer.StopTimer();
        }

        PlayerPrefs.SetInt("RESULT_WIN", 1);
        PlayerPrefs.SetInt("RESULT_STARS", stars);
        PlayerPrefs.Save();

        SceneManager.LoadScene("ResultScene");
    }

    public void Lose()
    {
        if (gameEnded) return;
        gameEnded = true;

        if (gameTimer != null)
            gameTimer.StopTimer();

        PlayerPrefs.SetInt("RESULT_WIN", 0);
        PlayerPrefs.SetInt("RESULT_STARS", 0);
        PlayerPrefs.Save();

        SceneManager.LoadScene("ResultScene");
    }
}
