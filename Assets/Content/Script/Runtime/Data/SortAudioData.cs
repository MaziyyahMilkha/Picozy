using UnityEngine;
using System.Collections.Generic;

public enum SortAudioPlayMode
{
    Random,
    AllSimultaneous,
    Sequential,
    Single
}

[System.Serializable]
public class SortAudioGroupEntry
{
    public string id;
    public List<AudioClip> clips = new List<AudioClip>();
    public bool looping;
    public SortAudioPlayMode playMode;
}

[CreateAssetMenu(fileName = "SortAudioData", menuName = "Sort/Audio Data", order = 1)]
public class SortAudioData : ScriptableObject
{
    public List<SortAudioGroupEntry> groups = new List<SortAudioGroupEntry>();

    public SortAudioGroupEntry GetGroup(string id)
    {
        if (groups == null || string.IsNullOrEmpty(id)) return null;
        foreach (var g in groups)
            if (g != null && g.id == id) return g;
        return null;
    }
}
