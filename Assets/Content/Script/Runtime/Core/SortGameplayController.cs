using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SortGameplayController : MonoBehaviour
{
    private const bool UseDebugLog = true;
    private struct MoveRecord
    {
        public SortDahan Source;
        public SortDahan Dest;
        public SortKarakter[] Movers;
        public int[] SourceSlotIndices;

        public int Count => Movers != null ? Movers.Length : 0;

        public MoveRecord(SortDahan source, SortDahan dest, SortKarakter[] movers, int[] sourceSlotIndices)
        {
            Source = source;
            Dest = dest;
            Movers = movers;
            SourceSlotIndices = sourceSlotIndices;
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
    [SerializeField] private bool keepTimerWhenRestartingFromGameplay = false;
    [SerializeField] private TextMeshProUGUI undoUsedCountText;
    [SerializeField] private bool allowMultipleUndo = true;

    [Header("Audio IDs")]
    [SerializeField] private float bgmFadeOutOnGameplayStart = 0.3f;
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
    private bool _undoQueued;
    private Coroutine _startBgmRoutine;
    private int _undoRemaining;
    private int _undoStartCount;
    private readonly List<MoveRecord> _undoHistory = new List<MoveRecord>(16);
    private readonly HashSet<SortDahan> _completedDahansThisMove = new HashSet<SortDahan>();
    private static readonly List<int> _tempSlots = new List<int>(8);
    private static readonly List<int> _tempDestSlots = new List<int>(8);

    public float TimeRemaining => timeRemaining;
    public bool IsRunning => running && !ended;
    public bool IsPaused => paused;
    public bool IsInteractionBlocked => paused || ended || _moveInProgress || _uiTransitionInProgress;
    public int UndoRemaining => _undoRemaining;
    public int UndoUsedCount => Mathf.Max(0, _undoStartCount - _undoRemaining);
    public bool CanUndo => _undoRemaining > 0 && _undoHistory.Count > 0 && running && !ended && !_moveInProgress;
    public bool HasNextLevel
    {
        get
        {
            if (levelLoader == null) return false;
            int idx = levelLoader.GetLevelIndexInDatabase();
            int total = levelLoader.GetTotalLevelCount();
            return idx >= 0 && idx + 1 < total;
        }
    }

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
        float t0 = Time.realtimeSinceStartup;
        if (levelLoader == null || levelLoader.GetCurrentLevel() == null) return;
        float tCheck = Time.realtimeSinceStartup;
        if (!string.IsNullOrEmpty(gameplayCanvasId))
            SortEventManager.Publish(new UIActionEvent("SwitchCanvas", gameplayCanvasId));
        float tCanvas = Time.realtimeSinceStartup;
        HideResultPopups();
        if (!string.IsNullOrEmpty(pauseCanvasId))
            SortEventManager.Publish(new UIActionEvent("HidePopupCanvas", pauseCanvasId));
        ApplyLevelTheme();
        float tTheme = Time.realtimeSinceStartup;
        StartLevel();
        float tStart = Time.realtimeSinceStartup;
        if (UseDebugLog)
        {
            Debug.LogWarning(
                $"[Perf][Gameplay] OnLevelLoaded check={(tCheck - t0) * 1000f:0.0}ms " +
                $"canvas={(tCanvas - tCheck) * 1000f:0.0}ms theme+ui={(tTheme - tCanvas) * 1000f:0.0}ms " +
                $"startLevel={(tStart - tTheme) * 1000f:0.0}ms total={(tStart - t0) * 1000f:0.0}ms");
        }
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

    public void StartLevel(bool resetTimer = true, float preservedTime = -1f, bool restartBgm = true)
    {
        float t0 = Time.realtimeSinceStartup;
        StopResultAndUiSfx();
        ended = false;
        _winAnimating = false;
        _moveInProgress = false;
        _uiTransitionInProgress = false;
        running = true;
        paused = false;
        _completedDahansThisMove.Clear();
        levelDuration = GetLevelDuration();
        if (resetTimer)
            timeRemaining = levelDuration;
        else
        {
            float sourceTime = preservedTime >= 0f ? preservedTime : timeRemaining;
            timeRemaining = Mathf.Clamp(sourceTime, 0f, levelDuration);
        }
        _undoStartCount = GetLevelUndoCount();
        _undoRemaining = _undoStartCount;
        ClearLastMove();
        if (starDisplay != null)
        {
            if (resetTimer)
                starDisplay.ResetToFull();
            starDisplay.SetLevelNumber(levelLoader != null ? levelLoader.GetDisplayLevelNumber() : 1);
        }
        RefreshTimerAndStars();
        RefreshUndoUsedCountUi();
        var resolved = levelLoader != null ? levelLoader.GetResolvedLevelSettings() : default;
        string bgmId = resolved.audioId;
        if (_startBgmRoutine != null)
        {
            StopCoroutine(_startBgmRoutine);
            _startBgmRoutine = null;
        }
        if (restartBgm)
            _startBgmRoutine = StartCoroutine(StartGameplayBgmNextFrame(bgmId));
        if (SortGameManager.Instance != null)
            SortGameManager.Instance.Resume();
        if (UseDebugLog)
        {
            float tEnd = Time.realtimeSinceStartup;
            Debug.LogWarning(
                $"[Perf][Gameplay] StartLevel resetTimer={resetTimer} restartBgm={restartBgm} " +
                $"duration={(tEnd - t0) * 1000f:0.0}ms bgmId={(string.IsNullOrEmpty(bgmId) ? "<none>" : bgmId)}");
        }
    }

    private IEnumerator StartGameplayBgmNextFrame(string bgmId)
    {
        yield return null;
        _startBgmRoutine = null;
        if (SortEffectPoolManager.Instance == null) yield break;
        var fx = SortEffectPoolManager.Instance;
        fx.StopAudioChannelWithFade(SortAudioChannel.Bgm, bgmFadeOutOnGameplayStart);
        if (string.IsNullOrEmpty(bgmId)) yield break;
        fx.WarmupAudioGroup(bgmId);
        float warmupTimeout = 1.25f;
        float elapsed = 0f;
        float t0 = Time.realtimeSinceStartup;
        while (elapsed < warmupTimeout && !fx.IsAudioGroupLoaded(bgmId))
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        if (UseDebugLog)
            Debug.LogWarning($"[Perf][AudioWarmup] gameplayBgmId={bgmId} waited={(Time.realtimeSinceStartup - t0) * 1000f:0.0}ms loaded={fx.IsAudioGroupLoaded(bgmId)}");
        fx.StopAudioGroup(bgmId);
        fx.PlayAudioWithFadeIn(bgmId, SortAudioChannel.Bgm, gameplayBgmFadeInSeconds);
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
        _completedDahansThisMove.Add(dahan);
        var resolved = levelLoader != null ? levelLoader.GetResolvedLevelSettings() : default;
        SortLevelRules.ProcessCompleteDahan(dahan, resolved.destroyBranchWhenComplete);
    }

    public void OnDahanCollectionStarted(SortDahan dahan)
    {
        if (dahan == null) return;
        ClearLastMove();
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
        _completedDahansThisMove.Clear();

        source.GetTopGroup(out int? topKind, out int actualCount, _tempSlots);
        if (!topKind.HasValue || actualCount == 0 || _tempSlots.Count == 0) { onComplete?.Invoke(); return; }

        int moveCount = Mathf.Min(count, actualCount, _tempSlots.Count);
        dest.GetNextEmptySlotIndicesForAdd(moveCount, _tempDestSlots);
        if (_tempDestSlots.Count < moveCount) { onComplete?.Invoke(); return; }

        _moveInProgress = true;
        source.OnTransferOut();

        var moversTemp = new SortKarakter[moveCount];
        var sourceSlotsTemp = new int[moveCount];
        int moverCount = 0;
        for (int i = 0; i < moveCount; i++)
        {
            var c = source.RemoveCharacterAtSlot(_tempSlots[i]);
            if (c != null)
            {
                c.transform.SetParent(dest.transform, true);
                moversTemp[moverCount] = c;
                sourceSlotsTemp[moverCount] = _tempSlots[i];
                moverCount++;
            }
        }

        if (moverCount == 0)
        {
            _moveInProgress = false;
            onComplete?.Invoke();
            return;
        }

        if (!string.IsNullOrEmpty(sfxKindMoveId) && SortEffectPoolManager.Instance != null)
            SortEffectPoolManager.Instance.PlayAudio(sfxKindMoveId, SortAudioChannel.Sfx);

        var movers = new SortKarakter[moverCount];
        var sourceSlots = new int[moverCount];
        Array.Copy(moversTemp, movers, moverCount);
        Array.Copy(sourceSlotsTemp, sourceSlots, moverCount);

        int moveCountFinal = moverCount;
        int arrived = 0;
        for (int i = 0; i < moverCount && i < _tempDestSlots.Count; i++)
        {
            int slotIndex = _tempDestSlots[i];
            Vector3 targetPos = dest.GetSlotPosition(slotIndex);
            SortKarakter c = movers[i];
            c.MoveTo(targetPos, () =>
            {
                dest.AddCharacterAtSlot(c, slotIndex);
                dest.OnTransferIn();
                arrived++;
                if (arrived >= moveCountFinal)
                {
                    dest.CompactSlots();
                    if (recordAsLastMove)
                    {
                        if (!_completedDahansThisMove.Contains(dest))
                            AddUndoRecord(source, dest, movers, sourceSlots);
                    }
                    _moveInProgress = false;
                    if (_undoQueued)
                    {
                        _undoQueued = false;
                        if (UseDebugLog)
                            Debug.LogWarning("[Undo] dequeued after move complete");
                        Undo();
                        return;
                    }
                    onComplete?.Invoke();
                    CheckLevelComplete();
                }
            });
        }
    }

    private void AddUndoRecord(SortDahan source, SortDahan dest, SortKarakter[] movers, int[] sourceSlots)
    {
        if (source == null || dest == null) return;
        if (movers == null || movers.Length == 0) return;
        if (sourceSlots == null || sourceSlots.Length != movers.Length) return;
        if (_undoStartCount <= 0) return;

        if (!allowMultipleUndo)
            _undoHistory.Clear();

        _undoHistory.Add(new MoveRecord(source, dest, movers, sourceSlots));
        if (UseDebugLog)
            Debug.LogWarning($"[Undo] record add count={movers.Length} source={source.name} dest={dest.name} history={_undoHistory.Count}");

        if (allowMultipleUndo && _undoStartCount > 0 && _undoHistory.Count > _undoStartCount)
            _undoHistory.RemoveAt(0);
    }

    private void ClearLastMove()
    {
        _undoHistory.Clear();
        _undoQueued = false;
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
        if (UseDebugLog)
            Debug.LogWarning($"[Undo] request remaining={_undoRemaining} running={running} ended={ended} paused={paused} moveInProgress={_moveInProgress} uiTransition={_uiTransitionInProgress} history={_undoHistory.Count} queued={_undoQueued}");
        if (_undoRemaining <= 0) { if (UseDebugLog) Debug.LogWarning("[Undo] blocked: undoRemaining<=0"); return; }
        if (!running || ended) { if (UseDebugLog) Debug.LogWarning("[Undo] blocked: not running or ended"); return; }
        if (_moveInProgress)
        {
            _undoQueued = true;
            if (UseDebugLog) Debug.LogWarning("[Undo] queued: move in progress");
            return;
        }

        while (_undoHistory.Count > 0)
        {
            int lastIndex = _undoHistory.Count - 1;
            MoveRecord record = _undoHistory[lastIndex];
            _undoHistory.RemoveAt(lastIndex);
            if (!IsUndoRecordValid(record, out string invalidReason))
            {
                if (UseDebugLog)
                    Debug.LogWarning($"[Undo] skip invalid record count={record.Count} reason={invalidReason} source={(record.Source != null ? record.Source.name : "<null>")} dest={(record.Dest != null ? record.Dest.name : "<null>")}");
                continue;
            }
            _undoRemaining--;
            RefreshUndoUsedCountUi();
            if (UseDebugLog)
                Debug.LogWarning($"[Undo] execute count={record.Count} from={record.Dest.name} to={record.Source.name} remaining={_undoRemaining} historyLeft={_undoHistory.Count}");
            DoUndoMove(record);
            return;
        }
        if (UseDebugLog)
            Debug.LogWarning("[Undo] no history record to undo");
    }

    private bool IsUndoRecordValid(MoveRecord record, out string invalidReason)
    {
        invalidReason = null;
        if (record.Source == null || record.Dest == null) { invalidReason = "source/dest null"; return false; }
        if (record.Movers == null || record.Movers.Length == 0) { invalidReason = "movers empty"; return false; }
        if (record.SourceSlotIndices == null || record.SourceSlotIndices.Length != record.Movers.Length) { invalidReason = "slot indices mismatch"; return false; }
        for (int i = 0; i < record.Movers.Length; i++)
        {
            var mover = record.Movers[i];
            if (mover == null) { invalidReason = $"mover null at {i}"; return false; }
            if (mover.GetDahan() != record.Dest) { invalidReason = $"mover not in dest at {i}"; return false; }
            int slotIndex = record.SourceSlotIndices[i];
            if (!record.Source.IsSlotEmpty(slotIndex)) { invalidReason = $"source slot not empty at {slotIndex}"; return false; }
        }
        return true;
    }

    private void DoUndoMove(MoveRecord record)
    {
        if (_moveInProgress) return;
        if (record.Source == null || record.Dest == null) return;
        if (record.Movers == null || record.Movers.Length == 0) return;
        if (record.SourceSlotIndices == null || record.SourceSlotIndices.Length != record.Movers.Length) return;

        _moveInProgress = true;
        record.Dest.OnTransferOut();

        int moveCountFinal = record.Movers.Length;
        int arrived = 0;
        for (int i = 0; i < record.Movers.Length; i++)
        {
            var c = record.Movers[i];
            int slotIndex = record.SourceSlotIndices[i];
            if (c == null)
            {
                arrived++;
                continue;
            }
            record.Dest.RemoveCharacter(c);
            c.transform.SetParent(record.Source.transform, true);
            Vector3 targetPos = record.Source.GetSlotPosition(slotIndex);
            c.MoveTo(targetPos, () =>
            {
                record.Source.AddCharacterAtSlot(c, slotIndex);
                record.Source.OnTransferIn();
                arrived++;
                if (arrived >= moveCountFinal)
                {
                    record.Dest.CompactSlots();
                    _moveInProgress = false;
                    if (_undoQueued)
                    {
                        _undoQueued = false;
                        if (UseDebugLog)
                            Debug.LogWarning("[Undo] dequeued after undo complete");
                        Undo();
                        return;
                    }
                    CheckLevelComplete();
                }
            });
        }
        if (arrived >= moveCountFinal)
        {
            record.Dest.CompactSlots();
            _moveInProgress = false;
            if (_undoQueued)
            {
                _undoQueued = false;
                if (UseDebugLog)
                    Debug.LogWarning("[Undo] dequeued after undo complete");
                Undo();
                return;
            }
            CheckLevelComplete();
        }
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
        string bgmId = resolved.audioId;
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
        int total = levelLoader.GetTotalLevelCount();
        int next = idx + 1;
        if (next >= total)
        {
            DoBackToMainMenu(ignoreTransitionGuard: true);
            return;
        }
        _uiTransitionInProgress = true;
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
        // The user requested that restarting the level should reset everything.
        // So we do not preserve the timer, and we restart the BGM.
        bool preserveTimer = false;
        float preservedTime = timeRemaining;
        _uiTransitionInProgress = true;
        StopResultAndUiSfx();
        HideResultPopups();
        levelLoader.LoadLevel();
        ApplyLevelTheme();
        SortEventManager.Publish(new UIActionEvent("OpenCanvas", gameplayCanvasId));
        StartLevel(!preserveTimer, preservedTime, restartBgm: true);
    }

    public void BackToMainMenu()
    {
        DoBackToMainMenu(ignoreTransitionGuard: false);
    }

    private void DoBackToMainMenu(bool ignoreTransitionGuard)
    {
        if (!ignoreTransitionGuard && _uiTransitionInProgress) return;
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
        if (_startBgmRoutine != null)
            StopCoroutine(_startBgmRoutine);
        if (Instance == this)
            Instance = null;
    }
}
