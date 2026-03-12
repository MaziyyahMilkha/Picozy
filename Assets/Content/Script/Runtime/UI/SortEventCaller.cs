using UnityEngine;

public class SortEventCaller : MonoBehaviour
{

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

}
