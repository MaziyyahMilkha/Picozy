using UnityEngine;
using System;

public class SortLevelSelectManager : MonoBehaviour
{
    public const string SaveKey = "SortLevelProgress";

    public static SortLevelSelectManager Instance { get; private set; }

    [SerializeField] private SortLevelDatabase database;
    [SerializeField] private SortLevelSelectorUI levelSelectorUI;
    [SerializeField] private string levelSelectorCanvasId;

    private int _currentMapIndex;
    private int _currentPageIndex;
    private int _highestCompletedGlobalIndex = -1;

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
        int mapIndex = database.GetMapIndexById(mapId);
        if (mapIndex >= 0)
            SetMap(mapIndex);
        if (!string.IsNullOrEmpty(levelSelectorCanvasId))
            SortEventManager.Publish(new UIActionEvent("SwitchCanvas", levelSelectorCanvasId));
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
        _currentPageIndex = 0;
        OnPageOrMapChanged?.Invoke();
    }

    public void PlayLevelAtSlot(int slotIndex)
    {
        int globalIndex = GetGlobalLevelIndexForSlot(slotIndex);
        if (globalIndex < 0) return;
        SortEventManager.Publish(new UIActionEvent("Level", globalIndex.ToString()));
    }

    public int GetLevelNumberForSlot(int slotIndex)
    {
        int global = GetGlobalLevelIndexForSlot(slotIndex);
        if (global < 0) return 0;
        return global + 1;
    }

    public string GetMapId(int mapIndex)
    {
        if (database == null || database.maps == null || mapIndex < 0 || mapIndex >= database.maps.Count)
            return "";
        return database.maps[mapIndex].id ?? "";
    }

    /// <summary>Panggil saat level selesai (menang). Dipakai untuk tampilan completed/available/locked di selector.</summary>
    public void ReportLevelCompleted(int globalLevelIndex)
    {
        if (globalLevelIndex < 0) return;
        if (globalLevelIndex > _highestCompletedGlobalIndex)
        {
            _highestCompletedGlobalIndex = globalLevelIndex;
            SaveProgress();
        }
    }

    private void LoadProgress()
    {
        try
        {
            if (ES3.KeyExists(SaveKey))
            {
                var data = ES3.Load<SortLevelSaveData>(SaveKey);
                _highestCompletedGlobalIndex = data.highestCompletedGlobalIndex;
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
                lastSavedTimestampUtc = DateTime.UtcNow.Ticks
            };
            ES3.Save(SaveKey, data);
        }
        catch (Exception e) { Debug.LogWarning("[SortLevelSelectManager] SaveProgress: " + e.Message); }
    }

    public LevelSlotState GetSlotState(int globalLevelIndex)
    {
        if (globalLevelIndex <= _highestCompletedGlobalIndex) return LevelSlotState.Completed;
        if (globalLevelIndex == _highestCompletedGlobalIndex + 1) return LevelSlotState.Available;
        return LevelSlotState.Locked;
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
