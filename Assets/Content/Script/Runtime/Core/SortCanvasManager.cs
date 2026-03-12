using System;
using System.Collections.Generic;
using UnityEngine;

public class SortCanvasManager : MonoBehaviour
{
    public static SortCanvasManager Instance { get; private set; }

    [Serializable]
    public class CanvasEntry
    {
        public string id;
        public GameObject canvas;
    }

    [SerializeField] private List<CanvasEntry> entries = new List<CanvasEntry>();

    private Dictionary<string, GameObject> _map;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        BuildMap();
    }

    private void BuildMap()
    {
        _map = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);
        if (entries == null) return;
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (string.IsNullOrEmpty(e.id) || e.canvas == null) continue;
            _map[e.id] = e.canvas;
        }
    }

    private void OnEnable()
    {
        SortEventManager.SubscribeAction("OpenCanvas", HandleOpenCanvas);
        SortEventManager.SubscribeAction("CloseAllCanvases", CloseAll);
        SortEventManager.SubscribeAction("SwitchCanvas", HandleSwitchCanvas);
    }

    private void OnDisable()
    {
        SortEventManager.UnsubscribeAction("OpenCanvas", HandleOpenCanvas);
        SortEventManager.UnsubscribeAction("CloseAllCanvases", CloseAll);
        SortEventManager.UnsubscribeAction("SwitchCanvas", HandleSwitchCanvas);
    }

    private void HandleSwitchCanvas(string canvasId)
    {
        if (string.IsNullOrEmpty(canvasId)) return;
        CloseAll();
        Open(canvasId);
    }

    private void HandleOpenCanvas(string canvasId)
    {
        if (string.IsNullOrEmpty(canvasId)) return;
        Open(canvasId);
    }

    public void Open(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        if (_map == null) BuildMap();
        foreach (var kv in _map)
            kv.Value.SetActive(kv.Key.Equals(id, StringComparison.OrdinalIgnoreCase));
    }

    public void CloseAll()
    {
        if (_map == null) BuildMap();
        foreach (var kv in _map)
            if (kv.Value != null) kv.Value.SetActive(false);
    }

    public void Show(string id)
    {
        if (string.IsNullOrEmpty(id) || _map == null) return;
        if (_map.TryGetValue(id, out var go) && go != null)
            go.SetActive(true);
    }

    public void Hide(string id)
    {
        if (string.IsNullOrEmpty(id) || _map == null) return;
        if (_map.TryGetValue(id, out var go) && go != null)
            go.SetActive(false);
    }

    public GameObject GetCanvas(string id)
    {
        if (_map == null) BuildMap();
        return _map != null && _map.TryGetValue(id, out var go) ? go : null;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
