using UnityEngine;

public class SortGameFlowManager : MonoBehaviour
{
    public static SortGameFlowManager Instance { get; private set; }

    [Header("Start")]
    [SerializeField] private bool startWithSplash = true;

    [Header("Canvas IDs")]
    [SerializeField] private string splashCanvasId;
    [SerializeField] private string mapCanvasId;
    [SerializeField] private string mainMenuBgmId = "Mainmenu";

    [Header("Debug")]
    [SerializeField] private int debugLevel;

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
        SortEventManager.Publish(new UIActionEvent("SwitchCanvas", mapCanvasId));
        if (SortEffectPoolManager.Instance != null && !string.IsNullOrEmpty(mainMenuBgmId))
        {
            SortEffectPoolManager.Instance.StopAllAudio();
            SortEffectPoolManager.Instance.PlayAudio(mainMenuBgmId, SortAudioChannel.Bgm);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
