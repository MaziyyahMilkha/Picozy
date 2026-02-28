using UnityEngine;
using UnityEngine.SceneManagement;

public class SortGameplayManager : MonoBehaviour
{
    public static SortGameplayManager Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;

    [Header("Level")]
    [SerializeField] private string nextSceneName = "";

    private bool levelCompleted;
    private bool levelFailed;

    public int Score { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void CheckLevelComplete()
    {
        if (levelCompleted || levelFailed) return;

        SortKarakter[] all = FindObjectsOfType<SortKarakter>();
        if (all.Length == 0)
            Win();
    }

    public void Win()
    {
        if (levelCompleted) return;
        levelCompleted = true;

        RequestPause(true);
        if (winPanel != null)
            winPanel.SetActive(true);

        Debug.Log("Level selesai!");
    }

    public void Lose()
    {
        if (levelFailed) return;
        levelFailed = true;

        RequestPause(true);
        if (losePanel != null)
            losePanel.SetActive(true);

        Debug.Log("Level gagal!");
    }

    public void RequestPause(bool pause)
    {
        if (SortGameManager.Instance != null)
            SortGameManager.Instance.SetTimeScale(pause ? 0f : 1f);
        else
            Time.timeScale = pause ? 0f : 1f;
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void NextLevel()
    {
        Time.timeScale = 1f;
        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
    }

    public void AddScore(int value)
    {
        Score += value;
    }

    public void ResetScore()
    {
        Score = 0;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
