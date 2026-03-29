using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SortGameplayController : MonoBehaviour
{
    private struct MoveRecord
    {
        public SortDahan Source;
        public SortDahan Dest;
        public int Count;

        public MoveRecord(SortDahan source, SortDahan dest, int count)
        {
            Source = source;
            Dest = dest;
            Count = count;
        }
    }

    public static SortGameplayController Instance { get; private set; }

    [Header("Level")]
    [SerializeField] private SortLevelLoader levelLoader;

    [Header("Canvas (by id)")]
    [SerializeField] private string gameplayCanvasId = "gameplay";
    [SerializeField] private string resultCanvasId = "result";
    [SerializeField] private string pauseCanvasId = "pause";

    [Header("Background")]
    [SerializeField] private Image backgroundImage;

    [Header("Timer & stars")]
    [SerializeField] private SortStarDisplay starDisplay;
    [SerializeField] private bool pauseStopsTimer = true;
    [SerializeField] private TextMeshProUGUI undoUsedCountText;
    [SerializeField] private bool allowMultipleUndo = true;

    [Header("Audio IDs")]
    [SerializeField] private float bgmFadeOutOnGameplayStart = 0.3f;
    [SerializeField] private string gameplayBgmId = "Gameplay";
    [SerializeField] private float gameplayBgmFadeInSeconds = 0.35f;
    [SerializeField] private float gameplayBgmFadeOutSeconds = 0.4f;
    [SerializeField] private string sfxWinId = "Win";
    [SerializeField] private string sfxLoseId = "Lose";
    [SerializeField] private string sfxKindMoveId = "KindMove";

    private float levelDuration;
    private float timeRemaining;
    private bool running;
    private bool ended;
    private bool paused;
    private bool _winAnimating;
    private bool _moveInProgress;
    private bool _uiTransitionInProgress;
    private int _undoRemaining;
    private int _undoStartCount;
    private readonly List<MoveRecord> _undoHistory = new List<MoveRecord>(16);
    private static readonly List<int> _tempSlots = new List<int>(8);
    private static readonly List<int> _tempDestSlots = new List<int>(8);

    public float TimeRemaining => timeRemaining;
    public bool IsRunning => running && !ended;
    public bool IsPaused => paused;
    public bool IsInteractionBlocked => paused || ended || _moveInProgress || _uiTransitionInProgress;
    public int UndoRemaining => _undoRemaining;
    public int UndoUsedCount => Mathf.Max(0, _undoStartCount - _undoRemaining);
    public bool CanUndo => _undoRemaining > 0 && _undoHistory.Count > 0 && running && !ended && !_moveInProgress;

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
        SortEventManager.SubscribeAction("LevelLoaded", OnLevelLoaded);
        SortEventManager.SubscribeAction("PauseGameplay", OnPauseRequested);
        SortEventManager.SubscribeAction("ResumeGameplay", OnResumeRequested);
        SortEventManager.SubscribeAction("BackToMainMenu", OnBackToMainMenuRequested);
    }

    private void OnDisable()
    {
        SortEventManager.UnsubscribeAction("LevelLoaded", OnLevelLoaded);
        SortEventManager.UnsubscribeAction("PauseGameplay", OnPauseRequested);
        SortEventManager.UnsubscribeAction("ResumeGameplay", OnResumeRequested);
        SortEventManager.UnsubscribeAction("BackToMainMenu", OnBackToMainMenuRequested);
    }

    private void OnPauseRequested()
    {
        Pause();
    }

    private void OnResumeRequested()
    {
        Resume();
    }

    private void OnBackToMainMenuRequested()
    {
        BackToMainMenu();
    }

    private void OnLevelLoaded(string _)
    {
        if (levelLoader == null || levelLoader.GetCurrentLevel() == null) return;
        if (!string.IsNullOrEmpty(gameplayCanvasId))
            SortEventManager.Publish(new UIActionEvent("SwitchCanvas", gameplayCanvasId));
        HideResultPopups();
        if (!string.IsNullOrEmpty(pauseCanvasId))
            SortEventManager.Publish(new UIActionEvent("HidePopupCanvas", pauseCanvasId));
        ApplyLevelTheme();
        StartLevel();
    }

    private void Update()
    {
        if (!running || ended) return;
        if (paused && pauseStopsTimer)
        {
            RefreshTimerAndStars();
            return;
        }

        float dt = paused && !pauseStopsTimer ? Time.unscaledDeltaTime : Time.deltaTime;
        timeRemaining -= dt;
        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            EndLevel(false);
        }
        RefreshTimerAndStars();
    }

    private void RefreshTimerAndStars()
    {
        float normalized = levelDuration > 0f ? Mathf.Clamp01(timeRemaining / levelDuration) : 0f;
        if (starDisplay != null)
            starDisplay.SetNormalizedTime(normalized);
    }

    public void StartLevel()
    {
        StopResultAndUiSfx();
        ended = false;
        _winAnimating = false;
        _moveInProgress = false;
        _uiTransitionInProgress = false;
        running = true;
        paused = false;
        levelDuration = GetLevelDuration();
        timeRemaining = levelDuration;
        _undoStartCount = GetLevelUndoCount();
        _undoRemaining = _undoStartCount;
        ClearLastMove();
        if (starDisplay != null)
        {
            starDisplay.ResetToFull();
            starDisplay.SetLevelNumber(levelLoader != null ? levelLoader.GetDisplayLevelNumber() : 1);
        }
        RefreshTimerAndStars();
        RefreshUndoUsedCountUi();
        var resolved = levelLoader != null ? levelLoader.GetResolvedLevelSettings() : default;
        string bgmId = !string.IsNullOrEmpty(gameplayBgmId) ? gameplayBgmId : resolved.audioId;
        if (SortEffectPoolManager.Instance != null)
        {
            SortEffectPoolManager.Instance.StopAudioChannelWithFade(SortAudioChannel.Bgm, bgmFadeOutOnGameplayStart);
            if (!string.IsNullOrEmpty(bgmId))
            {
                SortEffectPoolManager.Instance.StopAudioGroup(bgmId);
                SortEffectPoolManager.Instance.PlayAudioWithFadeIn(bgmId, SortAudioChannel.Bgm, gameplayBgmFadeInSeconds);
            }
        }
        if (SortGameManager.Instance != null)
            SortGameManager.Instance.Resume();
    }

    private int GetLevelUndoCount()
    {
        if (levelLoader == null) return 3;
        var resolved = levelLoader.GetResolvedLevelSettings();
        return resolved.undoCount >= 0 ? resolved.undoCount : 3;
    }

    private float GetLevelDuration()
    {
        if (levelLoader == null) return 60f;
        var resolved = levelLoader.GetResolvedLevelSettings();
        return resolved.levelDurationSeconds > 0f ? resolved.levelDurationSeconds : 60f;
    }

    private SortLevelData GetLevelData()
    {
        if (levelLoader == null) return null;
        var asset = levelLoader.GetCurrentLevel();
        return asset?.GetData();
    }

    public int GetCurrentLevelKindCount()
    {
        var data = GetLevelData();
        if (data == null) return 1;
        return Mathf.Max(1, CountBitsInMask(data.kindMask));
    }

    private static int CountBitsInMask(int mask)
    {
        int n = 0;
        for (int i = 0; i < 32; i++)
        {
            if ((mask & (1 << i)) != 0)
                n++;
        }
        return n;
    }

    public void OnDahanComplete(SortDahan dahan)
    {
        if (dahan == null) return;
        RemoveInvalidUndoRecords(dahan);
        var resolved = levelLoader != null ? levelLoader.GetResolvedLevelSettings() : default;
        SortLevelRules.ProcessCompleteDahan(dahan, resolved.destroyBranchWhenComplete);
    }

    public void NotifyLevelWinBeforeFeedback()
    {
        if (ended || _winAnimating) return;
        _winAnimating = true;
        running = false;
        RefreshTimerAndStars();
    }

    public bool CanMove(SortDahan source, SortDahan dest, int kind, int count)
    {
        if (_moveInProgress) return false;
        if (source == null || dest == null || source == dest || count <= 0) return false;
        if (dest.GetEmptySlotCount() < count) return false;
        int? topDest = dest.GetTopKind();
        if (topDest.HasValue && topDest.Value != kind) return false;
        return true;
    }

    public void DoMove(SortDahan source, SortDahan dest, int count, Action onComplete = null, bool recordAsLastMove = true)
    {
        if (_moveInProgress) { onComplete?.Invoke(); return; }
        if (source == null || dest == null || count <= 0) { onComplete?.Invoke(); return; }

        source.GetTopGroup(out int? topKind, out int actualCount, _tempSlots);
        if (!topKind.HasValue || actualCount == 0 || _tempSlots.Count == 0) { onComplete?.Invoke(); return; }

        int moveCount = Mathf.Min(count, actualCount, _tempSlots.Count);
        dest.GetNextEmptySlotIndicesForAdd(moveCount, _tempDestSlots);
        if (_tempDestSlots.Count < moveCount) { onComplete?.Invoke(); return; }

        _moveInProgress = true;
        source.OnTransferOut();

        var moving = new List<SortKarakter>(moveCount);
        for (int i = 0; i < moveCount; i++)
        {
            var c = source.RemoveCharacterAtSlot(_tempSlots[i]);
            if (c != null)
            {
                c.transform.SetParent(dest.transform, true);
                moving.Add(c);
            }
        }

        if (moving.Count == 0)
        {
            _moveInProgress = false;
            onComplete?.Invoke();
            return;
        }

        if (!string.IsNullOrEmpty(sfxKindMoveId) && SortEffectPoolManager.Instance != null)
            SortEffectPoolManager.Instance.PlayAudio(sfxKindMoveId, SortAudioChannel.Sfx);

        int moveCountFinal = moving.Count;
        int arrived = 0;
        for (int i = 0; i < moving.Count && i < _tempDestSlots.Count; i++)
        {
            int slotIndex = _tempDestSlots[i];
            Vector3 targetPos = dest.GetSlotPosition(slotIndex);
            SortKarakter c = moving[i];
            c.MoveTo(targetPos, () =>
            {
                dest.AddCharacterAtSlot(c, slotIndex);
                dest.OnTransferIn();
                arrived++;
                if (arrived >= moveCountFinal)
                {
                    dest.CompactSlots();
                    if (recordAsLastMove)
                        AddUndoRecord(source, dest, moveCountFinal);
                    _moveInProgress = false;
                    onComplete?.Invoke();
                    CheckLevelComplete();
                }
            });
        }
    }

    private void AddUndoRecord(SortDahan source, SortDahan dest, int count)
    {
        if (source == null || dest == null || count <= 0) return;
        if (_undoStartCount <= 0) return;

        if (!allowMultipleUndo)
            _undoHistory.Clear();

        _undoHistory.Add(new MoveRecord(source, dest, count));

        if (allowMultipleUndo && _undoStartCount > 0 && _undoHistory.Count > _undoStartCount)
            _undoHistory.RemoveAt(0);
    }

    private void ClearLastMove()
    {
        _undoHistory.Clear();
    }

    private void RemoveInvalidUndoRecords(SortDahan dahan)
    {
        if (dahan == null || _undoHistory.Count == 0) return;
        for (int i = _undoHistory.Count - 1; i >= 0; i--)
        {
            var record = _undoHistory[i];
            if (record.Source == dahan || record.Dest == dahan)
                _undoHistory.RemoveAt(i);
        }
    }

    public void Undo()
    {
        if (!CanUndo) return;
        _undoRemaining--;
        RefreshUndoUsedCountUi();
        int lastIndex = _undoHistory.Count - 1;
        MoveRecord record = _undoHistory[lastIndex];
        _undoHistory.RemoveAt(lastIndex);
        DoMove(record.Dest, record.Source, record.Count, onComplete: null, recordAsLastMove: false);
    }

    public void AddUndo(int amount)
    {
        if (amount > 0)
        {
            _undoRemaining = Mathf.Min(_undoStartCount, _undoRemaining + amount);
            RefreshUndoUsedCountUi();
        }
    }

    private void RefreshUndoUsedCountUi()
    {
        if (undoUsedCountText == null) return;
        undoUsedCountText.text = UndoRemaining.ToString();
    }

    public void CheckLevelComplete()
    {
        if (ended) return;
        if (levelLoader == null) return;

        int expected = levelLoader.GetSpawnedCharacterCountForCurrentLevel();
        if (expected <= 0)
            return;

        int alive = FindObjectsOfType<SortKarakter>().Length;
        if (alive > 0)
            return;

        EndLevel(true);
    }

    private void ApplyLevelTheme()
    {
        if (backgroundImage == null) return;
        Sprite bg = GetLevelBackground();
        if (bg != null)
            backgroundImage.sprite = bg;
    }

    private Sprite GetLevelBackground()
    {
        if (levelLoader == null) return null;
        var resolved = levelLoader.GetResolvedLevelSettings();
        return resolved.backgroundTheme;
    }

    private void EndLevel(bool won)
    {
        if (ended) return;
        ended = true;
        _winAnimating = false;
        _moveInProgress = false;
        running = false;
        var resolved = levelLoader != null ? levelLoader.GetResolvedLevelSettings() : default;
        string bgmId = !string.IsNullOrEmpty(gameplayBgmId) ? gameplayBgmId : resolved.audioId;
        if (!string.IsNullOrEmpty(bgmId) && SortEffectPoolManager.Instance != null)
            SortEffectPoolManager.Instance.StopAudioGroupWithFade(bgmId, gameplayBgmFadeOutSeconds);

        int stars = 0;
        if (won && levelLoader != null && SortLevelSelectManager.Instance != null)
        {
            int globalIndex = levelLoader.GetLevelIndexInDatabase();
            float norm = levelDuration > 0f ? Mathf.Clamp01(timeRemaining / levelDuration) : 0f;
            stars = norm > 0.6f ? 3 : (norm > 0.3f ? 2 : 1);
            SortLevelSelectManager.Instance.ReportLevelCompleted(globalIndex, stars);
        }
        if (SortGameManager.Instance != null)
            SortGameManager.Instance.Pause();
        if (SortEffectPoolManager.Instance != null)
        {
            SortEffectPoolManager.Instance.StopAudioChannelWithFade(SortAudioChannel.Sfx, 0f);
            string sfxId = won ? sfxWinId : sfxLoseId;
            if (!string.IsNullOrEmpty(sfxId))
                SortEffectPoolManager.Instance.PlayAudio(sfxId, SortAudioChannel.Sfx);
        }
        if (!string.IsNullOrEmpty(resultCanvasId))
            SortEventManager.Publish(new UIActionEvent("ShowPopupCanvas", resultCanvasId));
        SortEventManager.Publish(new UIActionEvent(won ? "Win" : "Lose", won ? stars.ToString() : null));
    }

    private void HideResultPopups()
    {
        if (!string.IsNullOrEmpty(resultCanvasId))
            SortEventManager.Publish(new UIActionEvent("HidePopupCanvas", resultCanvasId));
    }

    private void StopResultAndUiSfx()
    {
        if (SortEffectPoolManager.Instance != null)
            SortEffectPoolManager.Instance.StopAudioChannelWithFade(SortAudioChannel.Sfx, 0f);
    }

    public void ContinueToNextLevel()
    {
        if (_uiTransitionInProgress) return;
        StopResultAndUiSfx();
        if (levelLoader == null) return;
        int idx = levelLoader.GetLevelIndexInDatabase();
        if (idx < 0) return;
        _uiTransitionInProgress = true;
        int total = levelLoader.GetTotalLevelCount();
        int next = idx + 1;
        if (next >= total)
        {
            BackToMainMenu();
            return;
        }

        SortEventManager.Publish(new UIActionEvent("Level", next.ToString()));
    }

    public void Pause()
    {
        paused = true;
        if (SortGameManager.Instance != null)
            SortGameManager.Instance.Pause();
        SortEventManager.Publish(new UIActionEvent("ShowPopupCanvas", pauseCanvasId));
    }

    public void Resume()
    {
        paused = false;
        if (SortGameManager.Instance != null)
            SortGameManager.Instance.Resume();
        SortEventManager.Publish(new UIActionEvent("HidePopupCanvas", pauseCanvasId));
    }

    public void RestartLevel()
    {
        if (_uiTransitionInProgress) return;
        if (levelLoader == null) return;
        _uiTransitionInProgress = true;
        StopResultAndUiSfx();
        HideResultPopups();
        levelLoader.LoadLevel();
        ApplyLevelTheme();
        SortEventManager.Publish(new UIActionEvent("OpenCanvas", gameplayCanvasId));
        StartLevel();
    }

    public void BackToMainMenu()
    {
        if (_uiTransitionInProgress) return;
        _uiTransitionInProgress = true;
        StopResultAndUiSfx();
        ended = true;
        _winAnimating = false;
        running = false;
        paused = false;
        _moveInProgress = false;
        ClearLastMove();

        HideResultPopups();
        if (!string.IsNullOrEmpty(pauseCanvasId))
            SortEventManager.Publish(new UIActionEvent("HidePopupCanvas", pauseCanvasId));

        // Unload current level objects
        if (levelLoader != null)
            levelLoader.UnloadLevel();

        // Ensure timescale restored
        if (SortGameManager.Instance != null)
            SortGameManager.Instance.Resume();

        // Go to main menu (map selector canvas)
        SortEventManager.Publish(new UIActionEvent("Map", null));
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
