using TMPro;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SortLevelResultPanel : MonoBehaviour
{
    [Header("Stars (filled images)")]
    [SerializeField] private Image starFill1;
    [SerializeField] private Image starFill2;
    [SerializeField] private Image starFill3;
    [SerializeField] private float starFillDuration = 0.28f;
    [SerializeField] private float starFillDelayBetween = 0.1f;
    [SerializeField] private float starPopScale = 1.25f;
    [SerializeField] private float starPopUpDuration = 0.07f;
    [SerializeField] private float starPopDownDuration = 0.12f;
    [SerializeField] private string starFillStartSfxId = "StarFill";
    [SerializeField] private string starFillCompleteSfxId = "StarFillComplete";

    [Header("Title")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private string winTitle = "You win!";
    [SerializeField] private string loseTitle = "Time's up!";

    [Header("Buttons")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button exitButton;
    private Coroutine _starFillRoutine;
    private bool _actionLocked;
    private Vector3 _star1BaseScale = Vector3.one;
    private Vector3 _star2BaseScale = Vector3.one;
    private Vector3 _star3BaseScale = Vector3.one;

    private void Awake()
    {
        WireButtons();
        CacheStarBaseScales();
    }

    private void OnDestroy()
    {
        UnwireButtons();
    }

    private void OnEnable()
    {
        _actionLocked = false;
        SetButtonsInteractable(true);
        SortEventManager.SubscribeAction("Win", OnWin);
        SortEventManager.SubscribeAction("Lose", OnLoseNoData);
    }

    private void OnDisable()
    {
        StopStarFillRoutine();
        SortEventManager.UnsubscribeAction("Win", OnWin);
        SortEventManager.UnsubscribeAction("Lose", OnLoseNoData);
    }

    private void WireButtons()
    {
        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinueClicked);
        if (retryButton != null)
            retryButton.onClick.AddListener(OnRetryClicked);
        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitClicked);
    }

    private void UnwireButtons()
    {
        if (continueButton != null)
            continueButton.onClick.RemoveListener(OnContinueClicked);
        if (retryButton != null)
            retryButton.onClick.RemoveListener(OnRetryClicked);
        if (exitButton != null)
            exitButton.onClick.RemoveListener(OnExitClicked);
    }

    private void OnWin(string starsData)
    {
        _actionLocked = false;
        SetButtonsInteractable(true);
        int earned = 1;
        if (!string.IsNullOrEmpty(starsData))
            int.TryParse(starsData, out earned);
        earned = Mathf.Clamp(earned, 0, 3);

        ResetAllStarFill();
        StopStarFillRoutine();
        _starFillRoutine = StartCoroutine(FillStarsSequentialRoutine(earned));

        if (titleText != null)
            titleText.text = winTitle;

        if (continueButton != null)
            continueButton.gameObject.SetActive(true);
        if (retryButton != null)
            retryButton.gameObject.SetActive(true);
        if (exitButton != null)
            exitButton.gameObject.SetActive(true);
    }

    private void OnLoseNoData()
    {
        _actionLocked = false;
        SetButtonsInteractable(true);
        StopStarFillRoutine();
        ResetAllStarFill();

        if (titleText != null)
            titleText.text = loseTitle;

        if (continueButton != null)
            continueButton.gameObject.SetActive(false);
        if (retryButton != null)
            retryButton.gameObject.SetActive(true);
        if (exitButton != null)
            exitButton.gameObject.SetActive(true);
    }

    private void StopStarFillRoutine()
    {
        if (_starFillRoutine == null) return;
        StopCoroutine(_starFillRoutine);
        _starFillRoutine = null;
    }

    private void ResetAllStarFill()
    {
        SetFill(starFill1, 0f);
        SetFill(starFill2, 0f);
        SetFill(starFill3, 0f);
        ResetAllStarScale();
    }

    private static void SetFill(Image img, float amount)
    {
        if (img == null) return;
        img.fillAmount = Mathf.Clamp01(amount);
    }

    private void CacheStarBaseScales()
    {
        _star1BaseScale = starFill1 != null ? starFill1.transform.localScale : Vector3.one;
        _star2BaseScale = starFill2 != null ? starFill2.transform.localScale : Vector3.one;
        _star3BaseScale = starFill3 != null ? starFill3.transform.localScale : Vector3.one;
    }

    private void ResetAllStarScale()
    {
        if (starFill1 != null) starFill1.transform.localScale = _star1BaseScale;
        if (starFill2 != null) starFill2.transform.localScale = _star2BaseScale;
        if (starFill3 != null) starFill3.transform.localScale = _star3BaseScale;
    }

    private Vector3 GetBaseScaleForStar(Image star)
    {
        if (star == starFill1) return _star1BaseScale;
        if (star == starFill2) return _star2BaseScale;
        if (star == starFill3) return _star3BaseScale;
        return star != null ? star.transform.localScale : Vector3.one;
    }

    private IEnumerator FillStarsSequentialRoutine(int earnedCount)
    {
        if (earnedCount <= 0)
        {
            _starFillRoutine = null;
            yield break;
        }

        Image[] stars = { starFill1, starFill2, starFill3 };
        int count = Mathf.Clamp(earnedCount, 0, 3);
        for (int i = 0; i < count; i++)
        {
            Image star = stars[i];
            if (star != null)
                yield return StartCoroutine(FillSingleStarRoutine(star));

            if (i < count - 1 && starFillDelayBetween > 0f)
                yield return new WaitForSecondsRealtime(starFillDelayBetween);
        }

        _starFillRoutine = null;
    }

    private IEnumerator FillSingleStarRoutine(Image star)
    {
        if (star == null) yield break;

        PlaySfx(starFillStartSfxId);
        float duration = Mathf.Max(0.01f, starFillDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            star.fillAmount = t;
            yield return null;
        }

        star.fillAmount = 1f;
        PlaySfx(starFillCompleteSfxId);
        yield return StartCoroutine(PopStarRoutine(star));
    }

    private IEnumerator PopStarRoutine(Image star)
    {
        if (star == null) yield break;

        Vector3 baseScale = GetBaseScaleForStar(star);
        Vector3 peakScale = baseScale * Mathf.Max(1f, starPopScale);

        float upDuration = Mathf.Max(0.01f, starPopUpDuration);
        float downDuration = Mathf.Max(0.01f, starPopDownDuration);

        float elapsed = 0f;
        while (elapsed < upDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / upDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            star.transform.localScale = Vector3.Lerp(baseScale, peakScale, eased);
            yield return null;
        }
        star.transform.localScale = peakScale;

        elapsed = 0f;
        while (elapsed < downDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / downDuration);
            float eased = t * t;
            star.transform.localScale = Vector3.Lerp(peakScale, baseScale, eased);
            yield return null;
        }
        star.transform.localScale = baseScale;
    }

    private static void PlaySfx(string audioId)
    {
        if (string.IsNullOrEmpty(audioId)) return;
        if (SortEffectPoolManager.Instance == null) return;
        SortEffectPoolManager.Instance.PlayAudio(audioId, SortAudioChannel.Sfx);
    }

    private void OnContinueClicked()
    {
        if (_actionLocked) return;
        _actionLocked = true;
        SetButtonsInteractable(false);
        if (SortGameplayController.Instance != null)
            SortGameplayController.Instance.ContinueToNextLevel();
    }

    private void OnRetryClicked()
    {
        if (_actionLocked) return;
        _actionLocked = true;
        SetButtonsInteractable(false);
        if (SortGameplayController.Instance != null)
            SortGameplayController.Instance.RestartLevel();
    }

    private void OnExitClicked()
    {
        if (_actionLocked) return;
        _actionLocked = true;
        SetButtonsInteractable(false);
        if (SortGameplayController.Instance != null)
            SortGameplayController.Instance.BackToMainMenu();
    }

    private void SetButtonsInteractable(bool value)
    {
        if (continueButton != null) continueButton.interactable = value;
        if (retryButton != null) retryButton.interactable = value;
        if (exitButton != null) exitButton.interactable = value;
    }
}
