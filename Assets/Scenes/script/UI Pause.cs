using UnityEngine;
using UnityEngine.SceneManagement;

public class UIPauseManager : MonoBehaviour

{
    public string uiSceneName = "UI";

    public void LoadUIPause()
    {
        if (!SceneManager.GetSceneByName(uiSceneName).isLoaded)
        {
            SceneManager.LoadScene(uiSceneName, LoadSceneMode.Additive);
        }

        Time.timeScale = 0f;
    }

    public void UnloadUI()
    {
        Time.timeScale = 1f;
        SceneManager.UnloadSceneAsync(uiSceneName);
    }
}
