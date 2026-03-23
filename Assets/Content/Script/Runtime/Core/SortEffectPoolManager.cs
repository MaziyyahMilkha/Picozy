using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SortEffectPoolManager : MonoBehaviour
{
    public static SortEffectPoolManager Instance { get; private set; }

    [SerializeField] private SortAudioData audioData;
    [SerializeField] private int prewarmAudioSources = 4;
    private string poolRootName = "_AudioPool";
    private readonly List<AudioSource> _audioPool = new List<AudioSource>();
    private readonly Dictionary<string, List<AudioSource>> _loopedByGroup = new Dictionary<string, List<AudioSource>>();
    private readonly Dictionary<AudioSource, SortAudioChannel> _sourceChannels = new Dictionary<AudioSource, SortAudioChannel>();
    private readonly HashSet<AudioSource> _pausedBySettings = new HashSet<AudioSource>();
    private Transform _poolRoot;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        PrewarmPool();
    }

    private void PrewarmPool()
    {
        EnsurePoolRoot();
        int count = Mathf.Max(0, prewarmAudioSources);
        for (int i = _audioPool.Count; i < count; i++)
            GetOrCreateAudio();
    }

    #region Audio pool

    private void EnsurePoolRoot()
    {
        if (_poolRoot != null) return;

        string rootName = string.IsNullOrEmpty(poolRootName) ? "_AudioPool" : poolRootName;
        Transform existing = transform.Find(rootName);
        if (existing != null)
        {
            _poolRoot = existing;
            return;
        }

        GameObject go = new GameObject(rootName);
        go.transform.SetParent(transform, false);
        _poolRoot = go.transform;
    }

    private AudioSource GetOrCreateAudio()
    {
        EnsurePoolRoot();

        foreach (var s in _audioPool)
            if (s != null && !s.isPlaying) return s;

        GameObject child = new GameObject($"AudioSource_{_audioPool.Count:00}");
        child.transform.SetParent(_poolRoot, false);
        var src = child.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f;
        src.loop = false;
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
        PlayAudio(id, SortAudioChannel.Sfx);
    }

    public void PlayAudio(string id)
    {
        PlayAudio(id, SortAudioChannel.Sfx);
    }

    public void PlayAudio(string id, SortAudioChannel channel)
    {
        if (!CanPlayChannel(channel)) return;
        if (audioData == null)
            return;
        if (string.IsNullOrEmpty(id))
            return;
        var group = audioData.GetGroup(id);
        if (group == null)
            return;
        if (group.clips == null || group.clips.Count == 0)
            return;

        var mode = group.playMode;
        bool loop = group.looping;
        var clips = group.clips;

        if (loop)
        {
            PlayAudioLooped(id, group, mode, channel);
            return;
        }

        switch (mode)
        {
            case SortAudioPlayMode.Random:
                var r = clips[Random.Range(0, clips.Count)];
                if (r != null) PlayOneShotPooled(r, channel);
                break;
            case SortAudioPlayMode.Single:
                if (clips[0] != null) PlayOneShotPooled(clips[0], channel);
                break;
            case SortAudioPlayMode.AllSimultaneous:
                foreach (var c in clips)
                    if (c != null) PlayOneShotPooled(c, channel);
                break;
            case SortAudioPlayMode.Sequential:
                StartCoroutine(PlaySequentialRoutine(clips, channel));
                break;
        }
    }

    public void PlayAudioWithFadeIn(string id, SortAudioChannel channel, float fadeInDuration)
    {
        PlayAudio(id, channel);
        StartCoroutine(FadeInGroupRoutine(id, channel, Mathf.Max(0f, fadeInDuration)));
    }

    private IEnumerator FadeInGroupRoutine(string groupId, SortAudioChannel channel, float fadeInDuration)
    {
        if (fadeInDuration <= 0f) yield break;
        if (!_loopedByGroup.TryGetValue(groupId, out var list) || list == null || list.Count == 0) yield break;

        var sources = new List<AudioSource>(list.Count);
        var targetVolumes = new List<float>(list.Count);
        float target = GetChannelVolume(channel);
        for (int i = 0; i < list.Count; i++)
        {
            var src = list[i];
            if (src == null) continue;
            sources.Add(src);
            targetVolumes.Add(target);
            src.volume = 0f;
        }
        if (sources.Count == 0) yield break;

        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeInDuration);
            for (int i = 0; i < sources.Count; i++)
            {
                if (sources[i] != null)
                    sources[i].volume = targetVolumes[i] * t;
            }
            yield return null;
        }

        for (int i = 0; i < sources.Count; i++)
        {
            if (sources[i] != null)
                sources[i].volume = targetVolumes[i];
        }
    }

    private void PlayAudioLooped(string groupId, SortAudioGroupEntry group, SortAudioPlayMode mode, SortAudioChannel channel)
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
                    _sourceChannels[src] = channel;
                    src.clip = clip;
                    src.loop = true;
                    src.volume = GetChannelVolume(channel);
                    src.Play();
                    TrackLooped(groupId, src);
                }
                break;
            case SortAudioPlayMode.AllSimultaneous:
                foreach (var c in clips)
                {
                    if (c == null) continue;
                    var s = GetOrCreateAudio();
                    _sourceChannels[s] = channel;
                    s.clip = c;
                    s.loop = true;
                    s.volume = GetChannelVolume(channel);
                    s.Play();
                    TrackLooped(groupId, s);
                }
                break;
            case SortAudioPlayMode.Sequential:
                StartCoroutine(PlaySequentialLoopedRoutine(groupId, clips, channel));
                break;
        }
    }

    private IEnumerator PlaySequentialLoopedRoutine(string groupId, List<AudioClip> clips, SortAudioChannel channel)
    {
        var src = GetOrCreateAudio();
        if (src == null) yield break;
        _sourceChannels[src] = channel;
        TrackLooped(groupId, src);
        int index = 0;
        try
        {
            while (src != null && clips != null && clips.Count > 0)
            {
                src.clip = clips[index];
                src.loop = false;
                src.volume = GetChannelVolume(channel);
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

    private void PlayOneShotPooled(AudioClip clip, SortAudioChannel channel)
    {
        var src = GetOrCreateAudio();
        if (src == null) return;
        _sourceChannels[src] = channel;
        src.loop = false;
        src.volume = GetChannelVolume(channel);
        src.PlayOneShot(clip);
    }

    private IEnumerator PlaySequentialRoutine(List<AudioClip> clips, SortAudioChannel channel)
    {
        var src = GetOrCreateAudio();
        if (src == null) yield break;
        _sourceChannels[src] = channel;
        src.loop = false;
        for (int i = 0; i < clips.Count; i++)
        {
            var clip = clips[i];
            if (clip == null) continue;
            src.clip = clip;
            src.volume = GetChannelVolume(channel);
            src.Play();
            while (src != null && src.isPlaying)
                yield return null;
        }
    }

    private bool CanPlayChannel(SortAudioChannel channel)
    {
        var settings = SortSettingsManager.Instance;
        return settings == null || settings.IsAudioChannelEnabled(channel);
    }

    private float GetChannelVolume(SortAudioChannel channel)
    {
        var settings = SortSettingsManager.Instance;
        if (settings == null) return 1f;
        return channel == SortAudioChannel.Bgm ? settings.BgmVolume : settings.SfxVolume;
    }

    public void ApplySettingsAudioState()
    {
        for (int i = _audioPool.Count - 1; i >= 0; i--)
        {
            var src = _audioPool[i];
            if (src == null) continue;
            SortAudioChannel channel = _sourceChannels.TryGetValue(src, out var mapped) ? mapped : SortAudioChannel.Sfx;
            bool enabled = CanPlayChannel(channel);
            src.volume = GetChannelVolume(channel);

            if (enabled)
            {
                if (_pausedBySettings.Contains(src))
                {
                    src.UnPause();
                    _pausedBySettings.Remove(src);
                }
            }
            else
            {
                if (src.isPlaying)
                {
                    src.Pause();
                    _pausedBySettings.Add(src);
                }
            }
        }
    }

    public void StopAudioGroup(string groupId)
    {
        if (!_loopedByGroup.TryGetValue(groupId, out var list)) return;
        for (int i = list.Count - 1; i >= 0; i--)
        {
            var s = list[i];
            if (s != null)
            {
                s.Stop();
                _pausedBySettings.Remove(s);
            }
        }
        _loopedByGroup.Remove(groupId);
    }

    public void StopAudioGroupWithFade(string groupId, float fadeDuration)
    {
        if (!_loopedByGroup.TryGetValue(groupId, out var list) || list == null || list.Count == 0) return;
        StartCoroutine(FadeOutAndStopGroupRoutine(groupId, list, Mathf.Max(0f, fadeDuration)));
    }

    public void StopAudioChannelWithFade(SortAudioChannel channel, float fadeDuration)
    {
        var sources = new List<AudioSource>();
        for (int i = 0; i < _audioPool.Count; i++)
        {
            var src = _audioPool[i];
            if (src == null || !src.isPlaying) continue;
            SortAudioChannel mapped = _sourceChannels.TryGetValue(src, out var c) ? c : SortAudioChannel.Sfx;
            if (mapped == channel)
                sources.Add(src);
        }
        if (sources.Count == 0) return;
        StartCoroutine(FadeOutAndStopSourcesRoutine(sources, Mathf.Max(0f, fadeDuration)));
    }

    private IEnumerator FadeOutAndStopGroupRoutine(string groupId, List<AudioSource> list, float fadeDuration)
    {
        if (fadeDuration <= 0f)
        {
            StopAudioGroup(groupId);
            yield break;
        }

        var sources = new List<AudioSource>(list.Count);
        var startVolumes = new List<float>(list.Count);
        for (int i = 0; i < list.Count; i++)
        {
            var src = list[i];
            if (src == null) continue;
            sources.Add(src);
            startVolumes.Add(src.volume);
        }
        if (sources.Count == 0)
        {
            _loopedByGroup.Remove(groupId);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            float k = 1f - t;
            for (int i = 0; i < sources.Count; i++)
            {
                if (sources[i] != null)
                    sources[i].volume = startVolumes[i] * k;
            }
            yield return null;
        }

        for (int i = 0; i < sources.Count; i++)
        {
            var src = sources[i];
            if (src != null)
            {
                src.Stop();
                _pausedBySettings.Remove(src);
            }
        }
        _loopedByGroup.Remove(groupId);
    }

    private IEnumerator FadeOutAndStopSourcesRoutine(List<AudioSource> sources, float fadeDuration)
    {
        if (sources == null || sources.Count == 0) yield break;
        if (fadeDuration <= 0f)
        {
            for (int i = 0; i < sources.Count; i++)
            {
                var src = sources[i];
                if (src != null)
                {
                    src.Stop();
                    _pausedBySettings.Remove(src);
                }
            }
            yield break;
        }

        var startVolumes = new List<float>(sources.Count);
        for (int i = 0; i < sources.Count; i++)
            startVolumes.Add(sources[i] != null ? sources[i].volume : 0f);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            float k = 1f - t;
            for (int i = 0; i < sources.Count; i++)
            {
                if (sources[i] != null)
                    sources[i].volume = startVolumes[i] * k;
            }
            yield return null;
        }

        for (int i = 0; i < sources.Count; i++)
        {
            var src = sources[i];
            if (src != null)
            {
                src.Stop();
                _pausedBySettings.Remove(src);
            }
        }
    }

    public void StopAllAudio()
    {
        foreach (var s in _audioPool)
            if (s != null)
            {
                s.Stop();
                _pausedBySettings.Remove(s);
            }
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
