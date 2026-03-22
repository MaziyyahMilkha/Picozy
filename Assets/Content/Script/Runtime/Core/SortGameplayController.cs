using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SortGameplayController : MonoBehaviour
{
    public static SortGameplayController Instance { get; private set; }

    [Header("Level")]
    [SerializeField] private SortLevelLoader levelLoader;

    [Header("Canvas (by id)")]
    [SerializeField] private string gameplayCanvasId = "gameplay";
    [SerializeField] private string winCanvasId = "win";
    [SerializeField] private string loseCanvasId = "lose";
    [SerializeField] private string pauseCanvasId = "pause";

    [Header("Background")]
    [SerializeField] private Image backgroundImage;

    [Header("Timer & stars")]
    [SerializeField] private SortStarDisplay starDisplay;
    [SerializeField] private bool pauseStopsTimer = true;

    private float levelDuration;
    private float timeRemaining;
    private bool running;
    private bool ended;
    private bool paused;
    private int _undoRemaining;
    private bool _hasLastMove;
    private SortDahan _lastSource, _lastDest;
    private int _lastCount;
    private int _lastKind;
    private static readonly List<int> _tempSlots = new List<int>(8);
    private static readonly List<int> _tempDestSlots = new List<int>(8);

    public float TimeRemaining => timeRemaining;
    public bool IsRunning => running && !ended;
    public bool IsPaused => paused;
    public bool IsInteractionBlocked => paused || ended;
    public int UndoRemaining => _undoRemaining;
    public bool CanUndo => _undoRemaining > 0 && _hasLastMove && running && !ended;

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
        if (!string.IsNullOrEmpty(winCanvasId))
            SortEventManager.Publish(new UIActionEvent("HidePopupCanvas", winCanvasId));
        if (!string.IsNullOrEmpty(loseCanvasId))
            SortEventManager.Publish(new UIActionEvent("HidePopupCanvas", loseCanvasId));
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
        ended = false;
        running = true;
        paused = false;
        levelDuration = GetLevelDuration();
        timeRemaining = levelDuration;
        _undoRemaining = GetLevelUndoCount();
        ClearLastMove();
        if (starDisplay != null)
        {
            starDisplay.ResetToFull();
            starDisplay.SetLevelNumber(levelLoader != null ? levelLoader.GetDisplayLevelNumber() : 1);
        }
        RefreshTimerAndStars();
        var resolved = levelLoader != null ? levelLoader.GetResolvedLevelSettings() : default;
        if (!string.IsNullOrEmpty(resolved.audioId) && SortEffectPoolManager.Instance != null)
        {
            SortEffectPoolManager.Instance.StopAudioGroup(resolved.audioId);
            SortEffectPoolManager.Instance.PlayAudio(resolved.audioId);
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
        if (_hasLastMove && (_lastSource == dahan || _lastDest == dahan))
            ClearLastMove();
        var resolved = levelLoader != null ? levelLoader.GetResolvedLevelSettings() : default;
        SortLevelRules.ProcessCompleteDahan(dahan, resolved.destroyBranchWhenComplete);
        CheckLevelComplete();
    }

    public bool CanMove(SortDahan source, SortDahan dest, int kind, int count)
    {
        if (source == null || dest == null || source == dest || count <= 0) return false;
        if (dest.GetEmptySlotCount() < count) return false;
        int? topDest = dest.GetTopKind();
        if (topDest.HasValue && topDest.Value != kind) return false;
        return true;
    }

    public void DoMove(SortDahan source, SortDahan dest, int count, Action onComplete = null, bool recordAsLastMove = true)
    {
        if (source == null || dest == null || count <= 0) { onComplete?.Invoke(); return; }

        source.GetTopGroup(out int? topKind, out int actualCount, _tempSlots);
        if (!topKind.HasValue || actualCount == 0 || _tempSlots.Count == 0) { onComplete?.Invoke(); return; }

        int moveKind = topKind.Value;
        int moveCount = Mathf.Min(count, actualCount, _tempSlots.Count);
        dest.GetNextEmptySlotIndicesForAdd(moveCount, _tempDestSlots);
        if (_tempDestSlots.Count < moveCount) { onComplete?.Invoke(); return; }

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

        if (moving.Count == 0) { onComplete?.Invoke(); return; }

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
                        SetLastMove(source, dest, moveCountFinal, moveKind);
                    onComplete?.Invoke();
                    CheckLevelComplete();
                }
            });
        }
    }

    private void SetLastMove(SortDahan source, SortDahan dest, int count, int kind)
    {
        _lastSource = source;
        _lastDest = dest;
        _lastCount = count;
        _lastKind = kind;
        _hasLastMove = true;
    }

    private void ClearLastMove()
    {
        _hasLastMove = false;
        _lastSource = null;
        _lastDest = null;
    }

    public void Undo()
    {
        if (!CanUndo) return;
        _undoRemaining--;
        SortDahan from = _lastDest, to = _lastSource;
        int count = _lastCount;
        ClearLastMove();
        DoMove(from, to, count, onComplete: null, recordAsLastMove: false);
    }

    public void AddUndo(int amount)
    {
        if (amount > 0)
            _undoRemaining += amount;
    }

    public void CheckLevelComplete()
    {
        if (FindObjectsOfType<SortKarakter>().Length == 0)
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
        running = false;
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
        SortEventManager.Publish(new UIActionEvent("ShowPopupCanvas", won ? winCanvasId : loseCanvasId));
        SortEventManager.Publish(new UIActionEvent(won ? "Win" : "Lose", won ? stars.ToString() : null));
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
        if (levelLoader == null) return;
        levelLoader.LoadLevel();
        ApplyLevelTheme();
        SortEventManager.Publish(new UIActionEvent("OpenCanvas", gameplayCanvasId));
        StartLevel();
    }

    public void BackToMainMenu()
    {
        ended = true;
        running = false;
        paused = false;
        ClearLastMove();

        // Close gameplay popups
        if (!string.IsNullOrEmpty(winCanvasId))
            SortEventManager.Publish(new UIActionEvent("HidePopupCanvas", winCanvasId));
        if (!string.IsNullOrEmpty(loseCanvasId))
            SortEventManager.Publish(new UIActionEvent("HidePopupCanvas", loseCanvasId));
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
