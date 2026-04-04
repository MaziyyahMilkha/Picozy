using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum LevelSlotState
{
    Locked,
    Available,
    Completed
}

public class SortLevelButtonSlot : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject lockBackground;
    [SerializeField] private GameObject unlockBackground;
    [SerializeField] private GameObject unlockIcon;
    [SerializeField] private TMP_Text levelNumberText;

    [Header("Stars")]
    [SerializeField] private GameObject star1;
    [SerializeField] private GameObject star2;
    [SerializeField] private GameObject star3;

    [Header("Completed Pulse")]
    [SerializeField] private bool pulseWhenCompleted = true;

    private const float CompletedPulseScaleAmount = 0.03f;
    private const float CompletedPulseSpeed = 1.8f;
    private const float AvailablePulseScaleAmount = 0.055f;
    private const float AvailablePulseSpeed = 3.1f;

    private Button _button;
    private Vector3 _baseScale;
    private bool _isPulsingCompleted;
    private float _pulsePhase;
    private float _pulseScaleAmountCurrent;
    private float _pulseSpeedCurrent;

    public Button Button => _button != null ? _button : _button = GetComponent<Button>();

    private void Awake()
    {
        _baseScale = transform.localScale;
        _pulsePhase = Random.Range(0f, Mathf.PI * 2f);
        HideStars();
    }

    private void OnEnable()
    {
        transform.localScale = _baseScale;
    }

    private void Update()
    {
        if (!_isPulsingCompleted)
            return;

        _pulsePhase += Time.unscaledDeltaTime * Mathf.Max(0f, _pulseSpeedCurrent);
        float wave = (Mathf.Sin(_pulsePhase) + 1f) * 0.5f;
        float scale = 1f + Mathf.Max(0f, _pulseScaleAmountCurrent) * wave;
        transform.localScale = _baseScale * scale;
    }

    private void HideStars()
    {
        if (star1 != null) star1.SetActive(false);
        if (star2 != null) star2.SetActive(false);
        if (star3 != null) star3.SetActive(false);
    }

    public void SetLevelNumber(int levelNumber)
    {
        if (levelNumberText != null)
            levelNumberText.text = levelNumber > 0 ? levelNumber.ToString() : "";
    }

    public void SetState(LevelSlotState state, int levelNumber, int stars = 0)
    {
        int s = (state == LevelSlotState.Completed) ? (stars <= 0 ? 1 : Mathf.Clamp(stars, 1, 3)) : 0;
        bool pulseCompleted = pulseWhenCompleted && state == LevelSlotState.Completed;
        bool pulseAvailable = state == LevelSlotState.Available;
        _isPulsingCompleted = pulseCompleted || pulseAvailable;
        if (pulseAvailable)
        {
            _pulseScaleAmountCurrent = AvailablePulseScaleAmount;
            _pulseSpeedCurrent = AvailablePulseSpeed;
        }
        else if (pulseCompleted)
        {
            _pulseScaleAmountCurrent = CompletedPulseScaleAmount;
            _pulseSpeedCurrent = CompletedPulseSpeed;
        }
        if (!_isPulsingCompleted)
            transform.localScale = _baseScale;

        SetLevelNumber(levelNumber);
        if (lockBackground != null)
            lockBackground.SetActive(state == LevelSlotState.Completed);
        if (unlockBackground != null)
            unlockBackground.SetActive(state == LevelSlotState.Locked || state == LevelSlotState.Available);
        if (unlockIcon != null)
            unlockIcon.SetActive(state == LevelSlotState.Locked);
        if (levelNumberText != null)
            levelNumberText.gameObject.SetActive(state == LevelSlotState.Available || state == LevelSlotState.Completed);

        bool show1 = s >= 1, show2 = s >= 2, show3 = s >= 3;
        if (star1 != null) star1.SetActive(show1);
        if (star2 != null) star2.SetActive(show2);
        if (star3 != null) star3.SetActive(show3);

        var btn = Button;
        if (btn != null)
            btn.interactable = state != LevelSlotState.Locked;
    }

    public void SetLockBackground(Sprite sprite)
    {
        if (lockBackground == null || sprite == null) return;
        var img = lockBackground.GetComponent<Image>();
        if (img != null)
            img.sprite = sprite;
    }
}
