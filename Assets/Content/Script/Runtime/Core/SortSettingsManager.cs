using System;
using UnityEngine;
using UISwitcher;

public enum SortAudioChannel
{
    Sfx = 0,
    Bgm = 1
}

public class SortSettingsManager : MonoBehaviour
{
    public static SortSettingsManager Instance { get; private set; }

    public event Action OnSettingsChanged;

    [Header("Settings Canvas")]
    [SerializeField] private string settingsCanvasId = "Settings";
    [SerializeField] private bool closeRestoresPreviousFocus = false;
    private string _returnToCanvasId;

    [Header("UI Switches")]
    [SerializeField] private UISwitcher.UISwitcher bgmUiSwitcher;
    [SerializeField] private UISwitcher.UISwitcher sfxUiSwitcher;

    private const string KeyBgmEnabled = "sort_settings_bgm_enabled";
    private const string KeySfxEnabled = "sort_settings_sfx_enabled";

    [Header("Defaults")]
    [SerializeField] private bool defaultBgmEnabled = true;
    [SerializeField] private bool defaultSfxEnabled = true;

    public bool BgmEnabled { get; private set; } = true;
    public bool SfxEnabled { get; private set; } = true;
    public float BgmVolume => BgmEnabled ? 1f : 0f;
    public float SfxVolume => SfxEnabled ? 1f : 0f;
    public bool IsAudioChannelEnabled(SortAudioChannel channel) => channel == SortAudioChannel.Bgm ? BgmEnabled : SfxEnabled;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Load();
        ApplyToAudio();
        SyncSwitchesFromSettings();
    }

    private void OnEnable()
    {
        SortEventManager.SubscribeAction("OpenSettings", HandleOpenSettings);
        SortEventManager.SubscribeAction("CloseSettings", HandleCloseSettings);

        if (bgmUiSwitcher != null) bgmUiSwitcher.onValueChanged.AddListener(OnBgmSwitchChanged);
        if (sfxUiSwitcher != null) sfxUiSwitcher.onValueChanged.AddListener(OnSfxSwitchChanged);
    }

    private void OnDisable()
    {
        SortEventManager.UnsubscribeAction("OpenSettings", HandleOpenSettings);
        SortEventManager.UnsubscribeAction("CloseSettings", HandleCloseSettings);

        if (bgmUiSwitcher != null) bgmUiSwitcher.onValueChanged.RemoveListener(OnBgmSwitchChanged);
        if (sfxUiSwitcher != null) sfxUiSwitcher.onValueChanged.RemoveListener(OnSfxSwitchChanged);
    }

    public void Load()
    {
        BgmEnabled = PlayerPrefs.GetInt(KeyBgmEnabled, defaultBgmEnabled ? 1 : 0) != 0;
        SfxEnabled = PlayerPrefs.GetInt(KeySfxEnabled, defaultSfxEnabled ? 1 : 0) != 0;
    }

    public void Save()
    {
        PlayerPrefs.SetInt(KeyBgmEnabled, BgmEnabled ? 1 : 0);
        PlayerPrefs.SetInt(KeySfxEnabled, SfxEnabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetBgmEnabled(bool enabled)
    {
        if (BgmEnabled == enabled) return;
        BgmEnabled = enabled;
        Save();
        ApplyToAudio();
        SyncSwitchesFromSettings();
        OnSettingsChanged?.Invoke();
    }

    public void SetSfxEnabled(bool enabled)
    {
        if (SfxEnabled == enabled) return;
        SfxEnabled = enabled;
        Save();
        ApplyToAudio();
        SyncSwitchesFromSettings();
        OnSettingsChanged?.Invoke();
    }

    public void ResetToDefaults()
    {
        BgmEnabled = defaultBgmEnabled;
        SfxEnabled = defaultSfxEnabled;
        Save();
        ApplyToAudio();
        SyncSwitchesFromSettings();
        OnSettingsChanged?.Invoke();
    }

    private void ApplyToAudio()
    {
        if (SortEffectPoolManager.Instance != null)
            SortEffectPoolManager.Instance.ApplySettingsAudioState();
    }

    private void HandleOpenSettings(string returnToCanvasId)
    {
        _returnToCanvasId = string.IsNullOrEmpty(returnToCanvasId) ? null : returnToCanvasId;
        SortEventManager.Publish(new UIActionEvent("ShowPopupCanvas", settingsCanvasId));
    }

    private void HandleCloseSettings()
    {
        SortEventManager.Publish(new UIActionEvent("HidePopupCanvas", settingsCanvasId));

        if (closeRestoresPreviousFocus && !string.IsNullOrEmpty(_returnToCanvasId))
            SortEventManager.Publish(new UIActionEvent("OpenCanvas", _returnToCanvasId));
    }

    private void SyncSwitchesFromSettings()
    {
        if (bgmUiSwitcher != null) bgmUiSwitcher.SetWithoutNotify(BgmEnabled);
        if (sfxUiSwitcher != null) sfxUiSwitcher.SetWithoutNotify(SfxEnabled);
    }

    private void OnBgmSwitchChanged(bool enabled)
    {
        SetBgmEnabled(enabled);
    }

    private void OnSfxSwitchChanged(bool enabled)
    {
        SetSfxEnabled(enabled);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}

