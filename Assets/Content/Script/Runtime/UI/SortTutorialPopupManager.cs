using UnityEngine;
using UnityEngine.UI;

public class SortTutorialPopupManager : MonoBehaviour
{
    [Header("Popup")]
    [SerializeField] private string tutorialCanvasId = "Tutorial";

    [Header("Tutorial Image")]
    [SerializeField] private Image tutorialImage;
    [SerializeField] private Sprite tutorialPage1Sprite;
    [SerializeField] private Sprite tutorialPage2Sprite;

    [Header("Navigation Button")]
    [SerializeField] private Button navButton;
    [SerializeField] private Image navIconImage;
    [SerializeField] private Sprite navNextSprite;
    [SerializeField] private Sprite navPrevSprite;

    private int _pageIndex; // 0 = page1, 1 = page2

    private void Awake()
    {
        WireButtons();
        ApplyPage(0);
    }

    private void OnDestroy()
    {
        UnwireButtons();
    }

    private void OnEnable()
    {
        SortEventManager.SubscribeAction("OpenTutorial", OnOpenTutorialEvent);
        SortEventManager.SubscribeAction("CloseTutorial", OnCloseTutorialEvent);
    }

    private void OnDisable()
    {
        SortEventManager.UnsubscribeAction("OpenTutorial", OnOpenTutorialEvent);
        SortEventManager.UnsubscribeAction("CloseTutorial", OnCloseTutorialEvent);
    }

    private void WireButtons()
    {
        if (navButton != null)
            navButton.onClick.AddListener(OnNavClicked);
    }

    private void UnwireButtons()
    {
        if (navButton != null)
            navButton.onClick.RemoveListener(OnNavClicked);
    }

    private void OnOpenTutorialEvent()
    {
        OpenTutorial();
    }

    private void OnCloseTutorialEvent()
    {
        CloseTutorial();
    }

    public void OpenTutorial()
    {
        ApplyPage(0);
        if (!string.IsNullOrEmpty(tutorialCanvasId))
            SortEventManager.Publish(new UIActionEvent("ShowPopupCanvas", tutorialCanvasId));
    }

    public void CloseTutorial()
    {
        if (!string.IsNullOrEmpty(tutorialCanvasId))
            SortEventManager.Publish(new UIActionEvent("HidePopupCanvas", tutorialCanvasId));
    }

    public void OnNavClicked()
    {
        ApplyPage(_pageIndex == 0 ? 1 : 0);
    }

    private void ApplyPage(int pageIndex)
    {
        _pageIndex = Mathf.Clamp(pageIndex, 0, 1);

        if (tutorialImage != null)
        {
            Sprite sprite = _pageIndex == 0 ? tutorialPage1Sprite : tutorialPage2Sprite;
            if (sprite != null)
                tutorialImage.sprite = sprite;
        }

        bool onFirstPage = _pageIndex == 0;
        if (navIconImage != null)
        {
            Sprite icon = onFirstPage ? navNextSprite : navPrevSprite;
            if (icon != null)
                navIconImage.sprite = icon;
        }
    }
}
