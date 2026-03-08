using UnityEngine;

public class SortAudioCaller : MonoBehaviour
{
    [SerializeField] private string audioId;
    [SerializeField] private SortAudioPlayMode playMode = SortAudioPlayMode.Random;

    public void PlayAudio()
    {
        SortEventManager.Publish(new PlayAudioEvent { id = audioId, mode = playMode });
    }

    public void PlayAudioDirect()
    {
        if (SortAudioManager.Instance != null)
            SortAudioManager.Instance.Play(audioId, playMode);
    }

    public void PlayAudioDefault()
    {
        if (SortAudioManager.Instance != null)
            SortAudioManager.Instance.Play(audioId);
    }

    public void SetAudioId(string id) => audioId = id;
    public void SetPlayMode(SortAudioPlayMode mode) => playMode = mode;
    public string GetAudioId() => audioId;
}
