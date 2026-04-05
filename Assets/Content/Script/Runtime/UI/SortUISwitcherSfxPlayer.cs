using UnityEngine;
using UnityEngine.EventSystems;
using UISwitcher;

[DisallowMultipleComponent]
public class SortUISwitcherSfxPlayer : MonoBehaviour, IPointerClickHandler
{
    [Header("Target")]
    [SerializeField] private UINullableToggle targetSwitcher;

    [Header("Play On Click")]
    [SerializeField] private bool playOnClick = true;
    [SerializeField] private string clickSfxId = "ButtonClick";

    [Header("Play On Value Changed")]
    [SerializeField] private bool playOnValueChanged = false;
    [SerializeField] private string onValueTrueSfxId = "SwitchOn";
    [SerializeField] private string onValueFalseSfxId = "SwitchOff";
    [SerializeField] private string onValueNullSfxId = "SwitchNull";

    private void Reset()
    {
        TryAutoBindTarget();
    }

    private void Awake()
    {
        TryAutoBindTarget();
    }

    private void OnEnable()
    {
        if (targetSwitcher != null)
            targetSwitcher.onValueChangedNullable.AddListener(OnSwitcherValueChanged);
    }

    private void OnDisable()
    {
        if (targetSwitcher != null)
            targetSwitcher.onValueChangedNullable.RemoveListener(OnSwitcherValueChanged);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!playOnClick) return;
        PlaySfx(clickSfxId);
    }

    private void OnSwitcherValueChanged(bool? value)
    {
        if (!playOnValueChanged) return;

        if (!value.HasValue)
        {
            PlaySfx(onValueNullSfxId);
            return;
        }

        PlaySfx(value.Value ? onValueTrueSfxId : onValueFalseSfxId);
    }

    private void TryAutoBindTarget()
    {
        if (targetSwitcher != null) return;
        targetSwitcher = GetComponent<UINullableToggle>();
    }

    private static void PlaySfx(string audioId)
    {
        if (string.IsNullOrEmpty(audioId)) return;
        if (SortEffectPoolManager.Instance == null) return;
        SortEffectPoolManager.Instance.PlayAudio(audioId, SortAudioChannel.Sfx);
    }
}
