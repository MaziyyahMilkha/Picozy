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

    [SerializeField] private List<CanvasEntry> canvases = new List<CanvasEntry>();

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
        SortEventManager.Subscribe<string>(HandleOpenCanvas);
    }

    private void OnDisable()
    {
        SortEventManager.Unsubscribe<string>(HandleOpenCanvas);
    }

    private void HandleOpenCanvas(string canvasId)
    {
        Open(canvasId);
    }

    public void Open(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        for (int i = 0; i < canvases.Count; i++)
        {
            var e = canvases[i];
            if (e.canvas != null)
                e.canvas.SetActive(e.id == id);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
