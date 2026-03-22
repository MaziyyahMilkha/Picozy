using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SortLevelResultPanel : MonoBehaviour
{
    [Header("Stars")]
    [SerializeField] private Image[] starImages = new Image[3];
    [SerializeField] private Sprite starEarnedSprite;
    [SerializeField] private Sprite starNotEarnedSprite;

    [Header("Title")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private string winTitle = "You win!";
    [SerializeField] private string loseTitle = "Time's up!";

    [Header("Buttons")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button exitButton;

    private void Awake()
    {
        WireButtons();
    }

    private void OnDestroy()
    {
        UnwireButtons();
    }

    private void OnEnable()
    {
        SortEventManager.SubscribeAction("Win", OnWin);
        SortEventManager.SubscribeAction("Lose", OnLoseNoData);
    }

    private void OnDisable()
    {
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
        int earned = 1;
        if (!string.IsNullOrEmpty(starsData))
            int.TryParse(starsData, out earned);
        earned = Mathf.Clamp(earned, 0, 3);

        ApplyStarSprites(earned, forWin: true);

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
        ApplyStarSprites(0, forWin: false);

        if (titleText != null)
            titleText.text = loseTitle;

        if (continueButton != null)
            continueButton.gameObject.SetActive(false);
        if (retryButton != null)
            retryButton.gameObject.SetActive(true);
        if (exitButton != null)
            exitButton.gameObject.SetActive(true);
    }

    private void ApplyStarSprites(int earnedCount, bool forWin)
    {
        if (starImages == null || starImages.Length == 0) return;

        for (int i = 0; i < starImages.Length; i++)
        {
            Image img = starImages[i];
            if (img == null) continue;

            bool lit = forWin && earnedCount > i;
            if (lit && starEarnedSprite != null)
                img.sprite = starEarnedSprite;
            else if (starNotEarnedSprite != null)
                img.sprite = starNotEarnedSprite;
        }
    }

    private void OnContinueClicked()
    {
        if (SortGameplayController.Instance != null)
            SortGameplayController.Instance.ContinueToNextLevel();
    }

    private void OnRetryClicked()
    {
        if (SortGameplayController.Instance != null)
            SortGameplayController.Instance.RestartLevel();
    }

    private void OnExitClicked()
    {
        if (SortGameplayController.Instance != null)
            SortGameplayController.Instance.BackToMainMenu();
    }
}
