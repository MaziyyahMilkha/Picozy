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

    [Header("Debug")]
    [SerializeField] private int debugLevel;
    private Coroutine _pendingMainMenuBgmRoutine;

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
    }

    private void OnDisable()
    {
        SortEventManager.UnsubscribeAction("Start", OnStart);
        SortEventManager.UnsubscribeAction("Map", OnMap);
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
    }

    private void OnMap()
    {
        if (SortEffectPoolManager.Instance != null)
            SortEffectPoolManager.Instance.StopAudioChannelWithFade(SortAudioChannel.Sfx, 0f);
        SortEventManager.Publish(new UIActionEvent("SwitchCanvas", mapCanvasId));
        TryPlayMainMenuBgm();
    }

    private void TryPlayMainMenuBgm()
    {
        if (string.IsNullOrEmpty(mainMenuBgmId)) return;

        var fx = SortEffectPoolManager.Instance;
        if (fx != null)
        {
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

    private void OnDestroy()
    {
        if (_pendingMainMenuBgmRoutine != null)
            StopCoroutine(_pendingMainMenuBgmRoutine);
        if (Instance == this)
            Instance = null;
    }
}
