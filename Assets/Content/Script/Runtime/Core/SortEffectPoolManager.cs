using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SortEffectPoolManager : MonoBehaviour
{
    public static SortEffectPoolManager Instance { get; private set; }

    [SerializeField] private SortAudioData audioData;
    private readonly List<AudioSource> _audioPool = new List<AudioSource>();
    private readonly Dictionary<string, List<AudioSource>> _loopedByGroup = new Dictionary<string, List<AudioSource>>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    #region Audio pool

    private AudioSource GetOrCreateAudio()
    {
        foreach (var s in _audioPool)
            if (s != null && !s.isPlaying) return s;
        var src = gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
        _audioPool.Add(src);
        return src;
    }

    private void TrackLooped(string groupId, AudioSource src)
    {
        if (string.IsNullOrEmpty(groupId) || src == null) return;
        if (!_loopedByGroup.TryGetValue(groupId, out var list))
        {
            list = new List<AudioSource>();
            _loopedByGroup[groupId] = list;
        }
        if (!list.Contains(src)) list.Add(src);
    }

    private void UntrackLooped(string groupId, AudioSource src)
    {
        if (_loopedByGroup.TryGetValue(groupId, out var list))
        {
            list.Remove(src);
            if (list.Count == 0) _loopedByGroup.Remove(groupId);
        }
    }

    private void OnEnable()
    {
        SortEventManager.SubscribeAction("PlayAudio", HandlePlayAudio);
    }

    private void OnDisable()
    {
        SortEventManager.UnsubscribeAction("PlayAudio", HandlePlayAudio);
    }

    private void HandlePlayAudio(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        PlayAudio(id);
    }

    public void PlayAudio(string id)
    {
        if (audioData == null || string.IsNullOrEmpty(id)) return;
        var group = audioData.GetGroup(id);
        if (group == null || group.clips == null || group.clips.Count == 0) return;

        var mode = group.playMode;
        bool loop = group.looping;
        var clips = group.clips;

        if (loop)
        {
            PlayAudioLooped(id, group, mode);
            return;
        }

        switch (mode)
        {
            case SortAudioPlayMode.Random:
                var r = clips[Random.Range(0, clips.Count)];
                if (r != null) PlayOneShotPooled(r);
                break;
            case SortAudioPlayMode.Single:
                if (clips[0] != null) PlayOneShotPooled(clips[0]);
                break;
            case SortAudioPlayMode.AllSimultaneous:
                foreach (var c in clips)
                    if (c != null) PlayOneShotPooled(c);
                break;
            case SortAudioPlayMode.Sequential:
                StartCoroutine(PlaySequentialRoutine(clips));
                break;
        }
    }

    private void PlayAudioLooped(string groupId, SortAudioGroupEntry group, SortAudioPlayMode mode)
    {
        var clips = group.clips;
        switch (mode)
        {
            case SortAudioPlayMode.Random:
            case SortAudioPlayMode.Single:
                var clip = mode == SortAudioPlayMode.Single ? clips[0] : clips[Random.Range(0, clips.Count)];
                if (clip != null)
                {
                    var src = GetOrCreateAudio();
                    src.clip = clip;
                    src.loop = true;
                    src.Play();
                    TrackLooped(groupId, src);
                }
                break;
            case SortAudioPlayMode.AllSimultaneous:
                foreach (var c in clips)
                {
                    if (c == null) continue;
                    var s = GetOrCreateAudio();
                    s.clip = c;
                    s.loop = true;
                    s.Play();
                    TrackLooped(groupId, s);
                }
                break;
            case SortAudioPlayMode.Sequential:
                StartCoroutine(PlaySequentialLoopedRoutine(groupId, clips));
                break;
        }
    }

    private IEnumerator PlaySequentialLoopedRoutine(string groupId, List<AudioClip> clips)
    {
        var src = GetOrCreateAudio();
        if (src == null) yield break;
        TrackLooped(groupId, src);
        int index = 0;
        try
        {
            while (src != null && clips != null && clips.Count > 0)
            {
                src.clip = clips[index];
                src.loop = false;
                src.Play();
                while (src != null && src.isPlaying)
                    yield return null;
                index = (index + 1) % clips.Count;
            }
        }
        finally
        {
            UntrackLooped(groupId, src);
        }
    }

    private void PlayOneShotPooled(AudioClip clip)
    {
        var src = GetOrCreateAudio();
        if (src == null) return;
        src.loop = false;
        src.PlayOneShot(clip);
    }

    private IEnumerator PlaySequentialRoutine(List<AudioClip> clips)
    {
        var src = GetOrCreateAudio();
        if (src == null) yield break;
        src.loop = false;
        for (int i = 0; i < clips.Count; i++)
        {
            var clip = clips[i];
            if (clip == null) continue;
            src.clip = clip;
            src.Play();
            while (src != null && src.isPlaying)
                yield return null;
        }
    }

    public void StopAudioGroup(string groupId)
    {
        if (!_loopedByGroup.TryGetValue(groupId, out var list)) return;
        for (int i = list.Count - 1; i >= 0; i--)
        {
            var s = list[i];
            if (s != null) s.Stop();
        }
        _loopedByGroup.Remove(groupId);
    }

    public void StopAllAudio()
    {
        foreach (var s in _audioPool)
            if (s != null) s.Stop();
        StopAllCoroutines();
        _loopedByGroup.Clear();
    }

    #endregion

    #region SFX (update kedepannya)
    #endregion

    #region Particle (update kedepannya)
    #endregion

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
