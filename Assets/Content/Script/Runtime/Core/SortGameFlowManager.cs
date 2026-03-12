using UnityEngine;

public class SortGameFlowManager : MonoBehaviour
{
    public static SortGameFlowManager Instance { get; private set; }

    [Header("Start")]
    [SerializeField] private bool startWithSplash = true;

    [Header("Canvas IDs")]
    [SerializeField] private string splashCanvasId;
    [SerializeField] private string mapCanvasId;
    [SerializeField] private string levelCanvasId;

    [Header("Debug)")]
    [SerializeField] private SortLevelLoader levelLoader;
    [SerializeField] private SortLevelDatabase debugDatabase;
    [SerializeField] private int debugLevelIndex;

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
        SortEventManager.SubscribeAction("Level", OnLevel);
    }

    private void OnDisable()
    {
        SortEventManager.UnsubscribeAction("Start", OnStart);
        SortEventManager.UnsubscribeAction("Map", OnMap);
        SortEventManager.UnsubscribeAction("Level", OnLevel);
    }

    private void Start()
    {
        if (startWithSplash)
        {
            SortEventManager.Publish(new UIActionEvent("Start"));
        }
        else
        {
            if (levelLoader != null && debugDatabase != null && debugDatabase.levels != null
                && debugLevelIndex >= 0 && debugLevelIndex < debugDatabase.levels.Length)
            {
                SortEventManager.Publish(new UIActionEvent("Level", debugLevelIndex.ToString()));
            }
            else
            {
                SortEventManager.Publish(new UIActionEvent("Map"));
            }
        }
    }

    private void OnStart()
    {
        SortEventManager.Publish(new UIActionEvent("SwitchCanvas", splashCanvasId));
    }

    private void OnMap()
    {
        SortEventManager.Publish(new UIActionEvent("SwitchCanvas", mapCanvasId));
    }

    private void OnLevel(string _)
    {
        SortEventManager.Publish(new UIActionEvent("SwitchCanvas", levelCanvasId));
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
