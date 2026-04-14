using UnityEngine;
using System;
using System.Collections.Generic;

public class SortLevelSelectManager : MonoBehaviour
{
    public const string SaveKey = "SortLevelProgress";
    private const bool UseDebugLog = true;

    public static SortLevelSelectManager Instance { get; private set; }

    [SerializeField] private SortLevelDatabase database;
    [SerializeField] private SortLevelSelectorUI levelSelectorUI;
    [SerializeField] private string levelSelectorCanvasId;

    private int _currentMapIndex;
    private int _currentPageIndex;
    private int _highestCompletedGlobalIndex = -1;
    private List<int> _starsPerLevel = new List<int>();

    public event Action OnPageOrMapChanged;

    public int CurrentMapIndex => _currentMapIndex;
    public int CurrentPageIndex => _currentPageIndex;

    private int LevelsPerPage => levelSelectorUI != null && levelSelectorUI.GetLevelButtonCount() > 0
        ? levelSelectorUI.GetLevelButtonCount()
        : 1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        LoadProgress();
    }

    private void OnEnable()
    {
        SortEventManager.SubscribeAction("OpenLevelSelector", OnOpenLevelSelector);
    }

    private void OnDisable()
    {
        SortEventManager.UnsubscribeAction("OpenLevelSelector", OnOpenLevelSelector);
    }

    private void OnOpenLevelSelector(string mapId)
    {
        if (database == null || string.IsNullOrEmpty(mapId)) return;
        float t0 = Time.realtimeSinceStartup;
        int mapIndex = database.GetMapIndexById(mapId);
        float tLookup = Time.realtimeSinceStartup;
        if (mapIndex >= 0)
            SetMap(mapIndex);
        float tSetMap = Time.realtimeSinceStartup;
        if (!string.IsNullOrEmpty(levelSelectorCanvasId))
            SortEventManager.Publish(new UIActionEvent("SwitchCanvas", levelSelectorCanvasId));
        float tEnd = Time.realtimeSinceStartup;

        float total = tEnd - t0;
        if (UseDebugLog)
        {
            Debug.LogWarning(
                $"[Perf][LevelSelector] OpenLevelSelector mapId={mapId} mapIndex={mapIndex} " +
                $"lookup={(tLookup - t0) * 1000f:0.0}ms setMap={(tSetMap - tLookup) * 1000f:0.0}ms " +
                $"switchCanvas={(tEnd - tSetMap) * 1000f:0.0}ms total={total * 1000f:0.0}ms");
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public int GetTotalMaps()
    {
        return database != null ? database.MapCount : 0;
    }

    public int GetLevelCountInMap(int mapIndex)
    {
        return database != null ? database.GetLevelCountInMap(mapIndex) : 0;
    }

    public int GetTotalPagesInMap(int mapIndex)
    {
        int count = GetLevelCountInMap(mapIndex);
        return count <= 0 ? 0 : (count + LevelsPerPage - 1) / LevelsPerPage;
    }

    public int GetGlobalLevelIndexForSlot(int slotIndex)
    {
        if (database == null) return -1;
        int levelInMap = _currentPageIndex * LevelsPerPage + slotIndex;
        int countInMap = GetLevelCountInMap(_currentMapIndex);
        if (levelInMap < 0 || levelInMap >= countInMap) return -1;
        int global = 0;
        for (int m = 0; m < _currentMapIndex; m++)
            global += GetLevelCountInMap(m);
        global += levelInMap;
        return global;
    }

    public int GetLevelCountOnCurrentPage()
    {
        int totalInMap = GetLevelCountInMap(_currentMapIndex);
        int startOnPage = _currentPageIndex * LevelsPerPage;
        if (startOnPage >= totalInMap) return 0;
        return Mathf.Min(LevelsPerPage, totalInMap - startOnPage);
    }

    public bool HasNextPage()
    {
        return _currentPageIndex + 1 < GetTotalPagesInMap(_currentMapIndex);
    }

    public bool HasPrevPage()
    {
        return _currentPageIndex > 0;
    }

    public bool NextPage()
    {
        if (!HasNextPage()) return false;
        _currentPageIndex++;
        OnPageOrMapChanged?.Invoke();
        return true;
    }

    public bool PrevPage()
    {
        if (!HasPrevPage()) return false;
        _currentPageIndex--;
        OnPageOrMapChanged?.Invoke();
        return true;
    }

    public void SetMap(int mapIndex)
    {
        int total = GetTotalMaps();
        if (total == 0) return;
        _currentMapIndex = Mathf.Clamp(mapIndex, 0, total - 1);
        _currentPageIndex = GetAutoPageIndexForCurrentMap();
        OnPageOrMapChanged?.Invoke();
    }

    private int GetAutoPageIndexForCurrentMap()
    {
        int levelCountInMap = GetLevelCountInMap(_currentMapIndex);
        if (levelCountInMap <= 0) return 0;

        int mapStartGlobal = GetGlobalStartIndexForMap(_currentMapIndex);
        int localLevelIndex = 0;
        for (int i = 0; i < levelCountInMap; i++)
        {
            int global = mapStartGlobal + i;
            if (GetSlotState(global) == LevelSlotState.Available)
            {
                localLevelIndex = i;
                break;
            }
        }

        int page = localLevelIndex / Mathf.Max(1, LevelsPerPage);
        int maxPage = Mathf.Max(0, GetTotalPagesInMap(_currentMapIndex) - 1);
        return Mathf.Clamp(page, 0, maxPage);
    }

    private int GetGlobalStartIndexForMap(int mapIndex)
    {
        if (mapIndex <= 0) return 0;
        int globalStart = 0;
        for (int m = 0; m < mapIndex; m++)
            globalStart += GetLevelCountInMap(m);
        return globalStart;
    }

    public void PlayLevelAtSlot(int slotIndex)
    {
        int globalIndex = GetGlobalLevelIndexForSlot(slotIndex);
        if (globalIndex < 0) return;
        if (UseDebugLog)
            Debug.LogWarning($"[Perf][LevelSelector] PlayLevelAtSlot slot={slotIndex} globalIndex={globalIndex} map={_currentMapIndex} page={_currentPageIndex}");
        SortEventManager.Publish(new UIActionEvent("Level", globalIndex.ToString()));
    }

    public int GetLevelNumberForSlot(int slotIndex)
    {
        int global = GetGlobalLevelIndexForSlot(slotIndex);
        if (global < 0) return 0;
        if (database != null && !database.levelNumberContinuesAcrossMaps)
        {
            int levelInMap = _currentPageIndex * LevelsPerPage + slotIndex;
            return levelInMap + 1;
        }
        return global + 1;
    }

    public string GetMapId(int mapIndex)
    {
        if (database == null || database.maps == null || mapIndex < 0 || mapIndex >= database.maps.Count)
            return "";
        return database.maps[mapIndex].id ?? "";
    }

    public void ReportLevelCompleted(int globalLevelIndex, int starCount = 1)
    {
        if (globalLevelIndex < 0) return;
        starCount = Mathf.Clamp(starCount, 1, 3);
        while (_starsPerLevel.Count <= globalLevelIndex)
            _starsPerLevel.Add(0);
        _starsPerLevel[globalLevelIndex] = starCount;
        if (globalLevelIndex > _highestCompletedGlobalIndex)
            _highestCompletedGlobalIndex = globalLevelIndex;
        SaveProgress();
    }

    public int GetStarsForLevel(int globalLevelIndex)
    {
        if (globalLevelIndex < 0 || globalLevelIndex >= _starsPerLevel.Count) return 0;
        return Mathf.Clamp(_starsPerLevel[globalLevelIndex], 0, 3);
    }

    private void LoadProgress()
    {
        try
        {
            if (ES3.KeyExists(SaveKey))
            {
                var data = ES3.Load<SortLevelSaveData>(SaveKey);
                _highestCompletedGlobalIndex = data.highestCompletedGlobalIndex;
                _starsPerLevel = data.starsPerLevel != null ? new List<int>(data.starsPerLevel) : new List<int>();
            }
        }
        catch (Exception e) { Debug.LogWarning("[SortLevelSelectManager] LoadProgress: " + e.Message); }
    }

    private void SaveProgress()
    {
        try
        {
            var data = new SortLevelSaveData
            {
                highestCompletedGlobalIndex = _highestCompletedGlobalIndex,
                lastSavedTimestampUtc = DateTime.UtcNow.Ticks,
                starsPerLevel = new List<int>(_starsPerLevel)
            };
            ES3.Save(SaveKey, data);
        }
        catch (Exception e) { Debug.LogWarning("[SortLevelSelectManager] SaveProgress: " + e.Message); }
    }

    public LevelSlotState GetSlotState(int globalLevelIndex)
    {
        if (IsLevelCompleted(globalLevelIndex))
            return LevelSlotState.Completed;

        if (database != null && database.IsFirstLevelOfAnyMap(globalLevelIndex))
            return LevelSlotState.Available;

        int prev = GetPreviousLevelInSameMap(globalLevelIndex);
        if (prev >= 0 && IsLevelCompleted(prev))
            return LevelSlotState.Available;

        return LevelSlotState.Locked;
    }

    private bool IsLevelCompleted(int globalLevelIndex)
    {
        if (globalLevelIndex < 0 || globalLevelIndex >= _starsPerLevel.Count) return false;
        return _starsPerLevel[globalLevelIndex] > 0;
    }

    private int GetPreviousLevelInSameMap(int globalLevelIndex)
    {
        if (database == null || globalLevelIndex < 0) return -1;
        int mapIndex = database.GetMapIndexForGlobalIndex(globalLevelIndex);
        if (mapIndex < 0) return -1;
        int mapStart = GetGlobalStartIndexForMap(mapIndex);
        int local = globalLevelIndex - mapStart;
        if (local <= 0) return -1;
        return globalLevelIndex - 1;
    }

    public Sprite GetCurrentMapLevelSelectorBackground()
    {
        if (database == null || database.maps == null || _currentMapIndex < 0 || _currentMapIndex >= database.maps.Count)
            return null;
        return database.maps[_currentMapIndex].levelSelectorBackground;
    }

    public Sprite GetCurrentMapSlotLockBackground()
    {
        if (database == null || database.maps == null || _currentMapIndex < 0 || _currentMapIndex >= database.maps.Count)
            return null;
        return database.maps[_currentMapIndex].slotLockBackground;
    }
}
