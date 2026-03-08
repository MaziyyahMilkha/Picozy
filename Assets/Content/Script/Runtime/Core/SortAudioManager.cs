using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SortAudioManager : MonoBehaviour
{
    public static SortAudioManager Instance { get; private set; }

    [SerializeField] private SortAudioData data;
    [SerializeField] private SortAudioPlayMode defaultMode = SortAudioPlayMode.Random;
    [SerializeField] private int poolSize = 8;
    [SerializeField] private int poolMax = 16;

    private List<AudioSource> _pool = new List<AudioSource>();
    private readonly Dictionary<string, List<AudioSource>> _loopedByGroup = new Dictionary<string, List<AudioSource>>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        GrowPool(poolSize);
    }

    private void GrowPool(int count)
    {
        for (int i = 0; i < count && _pool.Count < poolMax; i++)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            _pool.Add(src);
        }
    }

    private AudioSource GetPooledSource()
    {
        foreach (var s in _pool)
            if (s != null && !s.isPlaying) return s;
        if (_pool.Count < poolMax)
        {
            GrowPool(1);
            foreach (var s in _pool)
                if (s != null && !s.isPlaying) return s;
        }
        return null;
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
        SortEventManager.Subscribe<PlayAudioEvent>(HandlePlayAudio);
    }

    private void OnDisable()
    {
        SortEventManager.Unsubscribe<PlayAudioEvent>(HandlePlayAudio);
    }

    private void HandlePlayAudio(PlayAudioEvent e)
    {
        Play(e.id, e.mode);
    }

    public void Play(string id)
    {
        Play(id, defaultMode);
    }

    public void Play(string id, SortAudioPlayMode mode)
    {
        if (data == null || string.IsNullOrEmpty(id)) return;
        var group = data.GetGroup(id);
        if (group == null || group.clips == null || group.clips.Count == 0) return;

        bool loop = group.looping;
        var clips = group.clips;

        if (loop)
        {
            PlayLooped(id, group, mode);
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

    private void PlayLooped(string groupId, SortAudioGroupEntry group, SortAudioPlayMode mode)
    {
        var clips = group.clips;
        switch (mode)
        {
            case SortAudioPlayMode.Random:
            case SortAudioPlayMode.Single:
                var clip = mode == SortAudioPlayMode.Single ? clips[0] : clips[Random.Range(0, clips.Count)];
                if (clip != null)
                {
                    var src = GetPooledSource();
                    if (src != null)
                    {
                        src.clip = clip;
                        src.loop = true;
                        src.Play();
                        TrackLooped(groupId, src);
                    }
                }
                break;
            case SortAudioPlayMode.AllSimultaneous:
                foreach (var c in clips)
                {
                    if (c == null) continue;
                    var s = GetPooledSource();
                    if (s == null) break;
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
        var src = GetPooledSource();
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
        var src = GetPooledSource();
        if (src == null) return;
        src.loop = false;
        src.PlayOneShot(clip);
        StartCoroutine(ReturnSourceWhenDone(src, clip.length));
    }

    private IEnumerator ReturnSourceWhenDone(AudioSource src, float duration)
    {
        yield return new WaitForSecondsRealtime(duration);
    }

    private IEnumerator PlaySequentialRoutine(List<AudioClip> clips)
    {
        var src = GetPooledSource();
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

    public void StopGroup(string groupId)
    {
        if (!_loopedByGroup.TryGetValue(groupId, out var list)) return;
        for (int i = list.Count - 1; i >= 0; i--)
        {
            var s = list[i];
            if (s != null) s.Stop();
        }
        _loopedByGroup.Remove(groupId);
    }

    public void Stop()
    {
        foreach (var s in _pool)
            if (s != null) s.Stop();
        StopAllCoroutines();
        _loopedByGroup.Clear();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
