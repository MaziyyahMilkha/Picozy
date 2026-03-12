using UnityEngine;

public class SortGameFlowManager : MonoBehaviour
{
    public static SortGameFlowManager Instance { get; private set; }

    [SerializeField] private string splashCanvasId;
    [SerializeField] private string mapCanvasId;
    [SerializeField] private string levelCanvasId;
    [SerializeField] private float splashDuration = 2f;

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
        SortEventManager.Publish(new UIActionEvent("Start"));
    }

    public float GetSplashDuration() => Mathf.Max(0.1f, splashDuration);

    private void OnStart()
    {
        if (SortCanvasManager.Instance != null)
            SortCanvasManager.Instance.Open(splashCanvasId);
    }

    private void OnMap()
    {
        if (SortCanvasManager.Instance != null)
            SortCanvasManager.Instance.Open(mapCanvasId);
    }

    private void OnLevel()
    {
        if (SortCanvasManager.Instance != null)
            SortCanvasManager.Instance.Open(levelCanvasId);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
