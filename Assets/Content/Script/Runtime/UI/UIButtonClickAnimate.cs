using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class UIButtonClickAnimate : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerExitHandler
{
    [Header("Target")]
    [SerializeField] private RectTransform animateTarget;

    [Header("Squish (pointer down)")]
    [SerializeField] [Range(0.75f, 1f)] private float pressedScale = 0.88f;
    [SerializeField] private float pressDuration = 0.07f;

    [Header("Release")]
    [SerializeField] private float releaseDuration = 0.48f;
    [SerializeField] [Range(0.5f, 2.5f)] private float elasticAmplitude = 1.15f;
    [SerializeField] [Range(0.15f, 0.9f)] private float elasticPeriod = 0.38f;

    [Header("General")]
    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField] private bool disableAnimation;
    [SerializeField] private bool playClickAudio = true;
    [SerializeField] private string clickAudioId = "Click";
    [SerializeField] private SortAudioChannel clickAudioChannel = SortAudioChannel.Sfx;

    private Button _button;
    private RectTransform _rect;
    private Vector3 _restLocalScale;
    private Tweener _activeTween;
    private bool _pointerDown;
    private bool _wasInteractableLastFrame = true;
    private bool _clickAudioListenerBound;
    private float _lastClickAudioTime = -999f;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _rect = animateTarget != null ? animateTarget : GetComponent<RectTransform>();
        if (_rect != null)
            _restLocalScale = _rect.localScale;
        _wasInteractableLastFrame = _button != null && _button.interactable;

        if (_button != null)
        {
            _button.onClick.AddListener(OnButtonClickAudio);
            _clickAudioListenerBound = true;
        }
    }

    private void OnDestroy()
    {
        if (_button != null && _clickAudioListenerBound)
            _button.onClick.RemoveListener(OnButtonClickAudio);
        KillTween();
    }

    private void OnDisable()
    {
        KillTween();
        _pointerDown = false;
        if (_rect != null)
            _rect.localScale = _restLocalScale;
    }

    private void Update()
    {
        if (_button == null || _rect == null) return;

        if (_button.interactable != _wasInteractableLastFrame)
        {
            _wasInteractableLastFrame = _button.interactable;
            if (!_button.interactable)
            {
                KillTween();
                _pointerDown = false;
                _rect.localScale = _restLocalScale;
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (disableAnimation) return;
        if (_button == null || !_button.interactable || _rect == null) return;
        if (eventData.button != PointerEventData.InputButton.Left) return;

        _pointerDown = true;
        KillTween();
        _activeTween = _rect
            .DOScale(_restLocalScale * pressedScale, pressDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(useUnscaledTime);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (disableAnimation) return;
        if (eventData.button != PointerEventData.InputButton.Left) return;

        _pointerDown = false;
        if (_button == null || !_button.interactable || _rect == null) return;

        TryPlayClickAudio();
        PlayReleaseTween(1f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (disableAnimation) return;
        if (!_pointerDown) return;

        _pointerDown = false;
        if (_button == null || !_button.interactable || _rect == null) return;

        PlayReleaseTween(0.88f);
    }

    private void PlayReleaseTween(float durationMultiplier)
    {
        KillTween();
        _activeTween = _rect
            .DOScale(_restLocalScale, releaseDuration * durationMultiplier)
            .SetEase(Ease.OutElastic, elasticAmplitude, elasticPeriod)
            .SetUpdate(useUnscaledTime);
    }

    private void KillTween()
    {
        if (_activeTween != null && _activeTween.IsActive())
            _activeTween.Kill();
        _activeTween = null;
        if (_rect != null)
            _rect.DOKill();
    }

    private void TryPlayClickAudio()
    {
        if (!playClickAudio || string.IsNullOrEmpty(clickAudioId)) return;
        if (Time.unscaledTime - _lastClickAudioTime < 0.025f) return;
        _lastClickAudioTime = Time.unscaledTime;

        var settings = SortSettingsManager.Instance;
        if (settings != null && !settings.IsAudioChannelEnabled(clickAudioChannel))
            return;

        if (SortEffectPoolManager.Instance != null)
            SortEffectPoolManager.Instance.PlayAudio(clickAudioId, clickAudioChannel);
        else
            SortEventManager.Publish(new UIActionEvent("PlayAudio", clickAudioId));
    }

    private void OnButtonClickAudio()
    {
        TryPlayClickAudio();
    }
}
