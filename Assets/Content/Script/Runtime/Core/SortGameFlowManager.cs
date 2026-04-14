using UnityEngine;
using System.Collections;

public class SortGameFlowManager : MonoBehaviour
{
    public static SortGameFlowManager Instance { get; private set; }

    [Header("Start")]
    [SerializeField] private bool startWithSplash = true;

    [Header("Canvas IDs")]
    [SerializeField] private string splashCanvasId;
    [SerializeField] private string mapCanvasId;
    [SerializeField] private string mainMenuBgmId = "Mainmenu";
    [SerializeField] private float mainMenuBgmFadeInSeconds = 0.08f;
    [SerializeField] private float mainMenuBgmFadeOutOtherBgmSeconds = 0.12f;

    [Header("Audio warmup")]
    [SerializeField] private bool warmupAudioOnSplash = true;
    [SerializeField] private bool warmupLoopingAudioOnly = true;
    [SerializeField] private float warmupFrameBudgetMs = 2.5f;
    [SerializeField] private float warmupTotalBudgetSeconds = 1.5f;

    [Header("Debug")]
    [SerializeField] private int debugLevel;
    [SerializeField] private bool debugAudioWarmupLog = true;
    private Coroutine _pendingMainMenuBgmRoutine;
    private Coroutine _settingsBindRoutine;
    private Coroutine _audioWarmupRoutine;
    private bool _settingsSubscribed;
    private bool _isMapFlowActive = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        SortEventManager.SubscribeAction("Start", OnStart);
        SortEventManager.SubscribeAction("Map", OnMap);
        SortEventManager.SubscribeAction("Level", OnLevelRequested);
        TryBindSettingsEvents();
    }

    private void OnDisable()
    {
        SortEventManager.UnsubscribeAction("Start", OnStart);
        SortEventManager.UnsubscribeAction("Map", OnMap);
        SortEventManager.UnsubscribeAction("Level", OnLevelRequested);
        UnbindSettingsEvents();
        if (_settingsBindRoutine != null)
        {
            StopCoroutine(_settingsBindRoutine);
            _settingsBindRoutine = null;
        }
    }

    private void Start()
    {
        if (startWithSplash)
        {
            SortEventManager.Publish(new UIActionEvent("Start"));
            return;
        }
        if (debugLevel <= 0)
            return;
        SortEventManager.Publish(new UIActionEvent("Level", (debugLevel - 1).ToString()));
    }

    private void OnStart()
    {
        SortEventManager.Publish(new UIActionEvent("SwitchCanvas", splashCanvasId));
        TryStartAudioWarmup();
    }

    private void OnMap()
    {
        _isMapFlowActive = true;
        if (SortEffectPoolManager.Instance != null)
            SortEffectPoolManager.Instance.StopAudioChannelWithFade(SortAudioChannel.Sfx, 0f);
        SortEventManager.Publish(new UIActionEvent("SwitchCanvas", mapCanvasId));
        TryPlayMainMenuBgm();
    }

    private void OnLevelRequested(string _)
    {
        _isMapFlowActive = false;
    }

    private void TryPlayMainMenuBgm()
    {
        if (string.IsNullOrEmpty(mainMenuBgmId)) return;

        var fx = SortEffectPoolManager.Instance;
        if (fx != null)
        {
            if (fx.IsAudioGroupPlaying(mainMenuBgmId))
            {
                fx.ApplySettingsAudioState();
                return;
            }
            fx.StopAudioChannelWithFade(SortAudioChannel.Bgm, mainMenuBgmFadeOutOtherBgmSeconds);
            fx.PlayAudioWithFadeIn(mainMenuBgmId, SortAudioChannel.Bgm, mainMenuBgmFadeInSeconds);
            return;
        }

        if (_pendingMainMenuBgmRoutine == null)
            _pendingMainMenuBgmRoutine = StartCoroutine(WaitAndPlayMainMenuBgmRoutine());
    }

    private IEnumerator WaitAndPlayMainMenuBgmRoutine()
    {
        const float timeout = 2f;
        float elapsed = 0f;
        while (elapsed < timeout)
        {
            var fx = SortEffectPoolManager.Instance;
            if (fx != null)
            {
                if (fx.IsAudioGroupPlaying(mainMenuBgmId))
                {
                    fx.ApplySettingsAudioState();
                    _pendingMainMenuBgmRoutine = null;
                    yield break;
                }
                fx.StopAudioChannelWithFade(SortAudioChannel.Bgm, mainMenuBgmFadeOutOtherBgmSeconds);
                fx.PlayAudioWithFadeIn(mainMenuBgmId, SortAudioChannel.Bgm, mainMenuBgmFadeInSeconds);
                _pendingMainMenuBgmRoutine = null;
                yield break;
            }
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        _pendingMainMenuBgmRoutine = null;
    }

    private void TryBindSettingsEvents()
    {
        var settings = SortSettingsManager.Instance;
        if (settings != null)
        {
            settings.OnSettingsChanged -= HandleSettingsChanged;
            settings.OnSettingsChanged += HandleSettingsChanged;
            _settingsSubscribed = true;
            return;
        }

        if (_settingsBindRoutine == null)
            _settingsBindRoutine = StartCoroutine(WaitAndBindSettingsRoutine());
    }

    private IEnumerator WaitAndBindSettingsRoutine()
    {
        const float timeout = 5f;
        float elapsed = 0f;
        while (elapsed < timeout)
        {
            var settings = SortSettingsManager.Instance;
            if (settings != null)
            {
                settings.OnSettingsChanged -= HandleSettingsChanged;
                settings.OnSettingsChanged += HandleSettingsChanged;
                _settingsSubscribed = true;
                _settingsBindRoutine = null;
                yield break;
            }
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        _settingsBindRoutine = null;
    }

    private void UnbindSettingsEvents()
    {
        if (!_settingsSubscribed) return;
        var settings = SortSettingsManager.Instance;
        if (settings != null)
            settings.OnSettingsChanged -= HandleSettingsChanged;
        _settingsSubscribed = false;
    }

    private void HandleSettingsChanged()
    {
        var fx = SortEffectPoolManager.Instance;
        if (fx == null) return;
        if (SortSettingsManager.Instance == null) return;

        if (!SortSettingsManager.Instance.BgmEnabled)
            fx.ApplySettingsAudioState();

        if (_isMapFlowActive)
            TryPlayMainMenuBgm();
    }

    private void TryStartAudioWarmup()
    {
        if (!warmupAudioOnSplash) return;
        if (_audioWarmupRoutine != null) return;
        _audioWarmupRoutine = StartCoroutine(AudioWarmupRoutine());
    }

    private IEnumerator AudioWarmupRoutine()
    {
        const float timeout = 2f;
        float t0 = Time.realtimeSinceStartup;
        float elapsed = 0f;
        while (elapsed < timeout)
        {
            var fx = SortEffectPoolManager.Instance;
            if (fx != null)
            {
                yield return null;
                if (debugAudioWarmupLog)
                    Debug.LogWarning($"[Perf][AudioWarmup] start loopingOnly={warmupLoopingAudioOnly} frameBudgetMs={warmupFrameBudgetMs:0.0} totalBudgetS={warmupTotalBudgetSeconds:0.00}");
                fx.WarmupAllAudioGroups(warmupLoopingAudioOnly, warmupFrameBudgetMs, warmupTotalBudgetSeconds);
                _audioWarmupRoutine = null;
                yield break;
            }
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        if (debugAudioWarmupLog)
            Debug.LogWarning($"[Perf][AudioWarmup] skipped (SortEffectPoolManager not ready) waited={(Time.realtimeSinceStartup - t0) * 1000f:0.0}ms");
        _audioWarmupRoutine = null;
    }

    private void OnDestroy()
    {
        if (_pendingMainMenuBgmRoutine != null)
            StopCoroutine(_pendingMainMenuBgmRoutine);
        if (_settingsBindRoutine != null)
            StopCoroutine(_settingsBindRoutine);
        if (_audioWarmupRoutine != null)
            StopCoroutine(_audioWarmupRoutine);
        UnbindSettingsEvents();
        if (Instance == this)
            Instance = null;
    }
}
