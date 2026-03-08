using UnityEngine;

public class SortCanvasCaller : MonoBehaviour
{
    [SerializeField] private string canvasId;

    public void OpenCanvas()
    {
        SortEventManager.Publish(canvasId);
    }

    public void OpenCanvasDirect()
    {
        if (SortCanvasManager.Instance != null)
            SortCanvasManager.Instance.Open(canvasId);
    }

    public void CloseAllCanvases()
    {
        if (SortCanvasManager.Instance != null)
            SortCanvasManager.Instance.CloseAll();
    }

    public void SetCanvasId(string id) => canvasId = id;
    public string GetCanvasId() => canvasId;
}
