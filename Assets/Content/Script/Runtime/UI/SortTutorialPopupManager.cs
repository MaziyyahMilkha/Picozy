using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SortTutorialPopupManager : MonoBehaviour
{
    [Header("Popup")]
    [SerializeField] private string tutorialCanvasId = "Tutorial";

    [Header("Book Page Curl")]
    [SerializeField] private AutoFlip autoFlip;

    [Header("Navigation Button")]
    [SerializeField] private Button navButton;
    [SerializeField] private Image navIconImage;
    [SerializeField] private Sprite navNextSprite;
    [SerializeField] private Sprite navPrevSprite;
    [SerializeField] private float navReenableDelaySeconds = 0.1f;

    private bool _nextClickFlipRight = true;
    private bool _navLocked;
    private Coroutine _unlockRoutine;
    private Book _book;
    private bool _bookListenerRegistered;

    private void Awake()
    {
        WireButtons();
        ApplyNavIcon();
    }

    private void OnDestroy()
    {
        UnwireButtons();
    }

    private void OnEnable()
    {
        SortEventManager.SubscribeAction("OpenTutorial", OnOpenTutorialEvent);
        SortEventManager.SubscribeAction("CloseTutorial", OnCloseTutorialEvent);
        RegisterBookFlipListener();
        SetNavLocked(false);
    }

    private void OnDisable()
    {
        SortEventManager.UnsubscribeAction("OpenTutorial", OnOpenTutorialEvent);
        SortEventManager.UnsubscribeAction("CloseTutorial", OnCloseTutorialEvent);
        UnregisterBookFlipListener();
        StopUnlockRoutine();
        SetNavLocked(false);
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
        _nextClickFlipRight = true;
        SetNavLocked(false);
        ApplyNavIcon();
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
        if (_navLocked || autoFlip == null) return;
        if (!CanFlipCurrentDirection()) return;

        SetNavLocked(true);

        if (_nextClickFlipRight)
            autoFlip.FlipRightPage();
        else
            autoFlip.FlipLeftPage();

        _nextClickFlipRight = !_nextClickFlipRight;
        ApplyNavIcon();
    }

    private void ApplyNavIcon()
    {
        if (navIconImage != null)
        {
            Sprite icon = _nextClickFlipRight ? navNextSprite : navPrevSprite;
            if (icon != null)
                navIconImage.sprite = icon;
        }
    }

    private bool CanFlipCurrentDirection()
    {
        Book book = GetBook();
        if (book == null) return false;
        if (_nextClickFlipRight)
            return book.currentPage < book.TotalPageCount;
        return book.currentPage > 0;
    }

    private Book GetBook()
    {
        if (_book != null) return _book;
        if (autoFlip == null) return null;
        _book = autoFlip.ControledBook != null ? autoFlip.ControledBook : autoFlip.GetComponent<Book>();
        return _book;
    }

    private void RegisterBookFlipListener()
    {
        if (_bookListenerRegistered) return;
        Book book = GetBook();
        if (book == null || book.OnFlip == null) return;
        book.OnFlip.AddListener(OnBookFlipped);
        _bookListenerRegistered = true;
    }

    private void UnregisterBookFlipListener()
    {
        if (!_bookListenerRegistered) return;
        if (_book != null && _book.OnFlip != null)
            _book.OnFlip.RemoveListener(OnBookFlipped);
        _bookListenerRegistered = false;
    }

    private void OnBookFlipped()
    {
        StopUnlockRoutine();
        _unlockRoutine = StartCoroutine(UnlockAfterDelayRoutine());
    }

    private IEnumerator UnlockAfterDelayRoutine()
    {
        float delay = Mathf.Max(0f, navReenableDelaySeconds);
        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);
        SetNavLocked(false);
        _unlockRoutine = null;
    }

    private void StopUnlockRoutine()
    {
        if (_unlockRoutine == null) return;
        StopCoroutine(_unlockRoutine);
        _unlockRoutine = null;
    }

    private void SetNavLocked(bool locked)
    {
        _navLocked = locked;
    }
}
