using UnityEngine;
using UnityEngine.SceneManagement;

public class SortGameManager : MonoBehaviour
{
    public static SortGameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Time.timeScale = 1f;
    }

    public float GetTimeScale()
    {
        return Time.timeScale;
    }

    public void SetTimeScale(float scale)
    {
        Time.timeScale = Mathf.Clamp01(scale);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void LoadScene(string sceneName)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
