using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SortSplashScreenManager : MonoBehaviour
{
    [Header("Loading splash")]
    [SerializeField] private float durationSeconds = 2f;
    [SerializeField] private Image fillImage;

    private void OnEnable()
    {
        StartCoroutine(LoadingRoutine());
    }

    private IEnumerator LoadingRoutine()
    {
        float duration = Mathf.Max(0.1f, durationSeconds);
        float elapsed = 0f;

        if (fillImage != null)
            fillImage.fillAmount = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            if (fillImage != null)
                fillImage.fillAmount = t;
            yield return null;
        }

        if (fillImage != null)
            fillImage.fillAmount = 1f;

        SortEventManager.Publish(new UIActionEvent("Map"));
    }
}
