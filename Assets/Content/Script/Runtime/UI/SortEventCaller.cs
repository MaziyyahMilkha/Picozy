using UnityEngine;

public class SortEventCaller : MonoBehaviour
{
    [Header("Raise event")]
    [SerializeField] private string eventId;
    [SerializeField] private string eventData;

    public void RaiseEvent(string actionId, string data = null)
    {
        SortEventManager.Publish(new UIActionEvent(actionId, data));
    }

    public void RaiseEvent()
    {
        SortEventManager.Publish(new UIActionEvent(eventId, string.IsNullOrEmpty(eventData) ? null : eventData));
    }

    public void PlayAudio(string id)
    {
        SortEventManager.Publish(new UIActionEvent("PlayAudio", id));
    }

    public void OpenCanvas(string id)
    {
        SortEventManager.Publish(new UIActionEvent("OpenCanvas", id));
    }

    public void CloseAllCanvases()
    {
        SortEventManager.Publish(new UIActionEvent("CloseAllCanvases", null));
    }

    public void SwitchCanvas(string id)
    {
        SortEventManager.Publish(new UIActionEvent("SwitchCanvas", id));
    }

    public void GoToStart()
    {
        SortEventManager.Publish(new UIActionEvent("Start", null));
    }

    public void GoToMap()
    {
        SortEventManager.Publish(new UIActionEvent("Map", null));
    }

    public void GoToLevel(int levelIndex)
    {
        SortEventManager.Publish(new UIActionEvent("Level", levelIndex.ToString()));
    }

    public void OpenLevelSelector(string id)
    {
        SortEventManager.Publish(new UIActionEvent("OpenLevelSelector", id));
    }

    public void OpenSettings(string returnToCanvasId = null)
    {
        SortEventManager.Publish(new UIActionEvent("OpenSettings", string.IsNullOrEmpty(returnToCanvasId) ? null : returnToCanvasId));
    }

    public void CloseSettings()
    {
        SortEventManager.Publish(new UIActionEvent("CloseSettings", null));
    }

    public void OpenPause()
    {
        SortEventManager.Publish(new UIActionEvent("PauseGameplay", null));
    }

    public void ClosePause()
    {
        SortEventManager.Publish(new UIActionEvent("ResumeGameplay", null));
    }

    public void BackToMainMenu()
    {
        SortEventManager.Publish(new UIActionEvent("BackToMainMenu", null));
    }
}
