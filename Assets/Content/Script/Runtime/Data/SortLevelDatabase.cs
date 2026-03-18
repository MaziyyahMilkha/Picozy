using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class SortLevelMapEntry
{
    public string id;

    public Sprite levelSelectorBackground;
    public Sprite slotLockBackground;

    [Header("Global settings")]
    public float globalLevelDurationSeconds = 60f;
    public int globalUndoCount = 3;
    public bool globalDestroyBranchWhenComplete = true;
    public Sprite globalBackgroundTheme;
    public string globalAudioId;

    [Header("Levels")]
    public List<SortLevelAsset> levels = new List<SortLevelAsset>();
}

public struct ResolvedLevelSettings
{
    public float levelDurationSeconds;
    public int undoCount;
    public bool destroyBranchWhenComplete;
    public Sprite backgroundTheme;
    public string audioId;
}

[CreateAssetMenu(fileName = "SortLevelDatabase", menuName = "Sort/Level Database")]
public class SortLevelDatabase : ScriptableObject
{
    [Header("Map settings")]
    public bool levelNumberContinuesAcrossMaps = true;
    public bool unlockFirstLevelPerMap = false;

    public List<SortLevelMapEntry> maps = new List<SortLevelMapEntry>();

    public int MapCount => maps != null ? maps.Count : 0;

    public bool IsFirstLevelOfAnyMap(int globalIndex)
    {
        if (maps == null || globalIndex < 0) return false;
        int sum = 0;
        for (int m = 0; m < maps.Count; m++)
        {
            if (globalIndex == sum) return true;
            sum += GetLevelCountInMap(m);
        }
        return false;
    }

    public int GetMapIndexById(string mapId)
    {
        if (maps == null || string.IsNullOrEmpty(mapId)) return -1;
        for (int i = 0; i < maps.Count; i++)
            if (string.Equals(maps[i].id, mapId, System.StringComparison.OrdinalIgnoreCase))
                return i;
        return -1;
    }

    public int GetLevelCountInMap(int mapIndex)
    {
        if (maps == null || mapIndex < 0 || mapIndex >= maps.Count) return 0;
        var list = maps[mapIndex].levels;
        return list != null ? list.Count : 0;
    }

    public int GetTotalLevelCount()
    {
        if (maps == null) return 0;
        int n = 0;
        for (int i = 0; i < maps.Count; i++)
        {
            var list = maps[i].levels;
            if (list != null) n += list.Count;
        }
        return n;
    }

    public SortLevelAsset GetLevelAssetByGlobalIndex(int globalIndex)
    {
        if (maps == null || globalIndex < 0) return null;
        int remaining = globalIndex;
        for (int m = 0; m < maps.Count; m++)
        {
            var list = maps[m].levels;
            if (list == null) continue;
            if (remaining < list.Count)
                return list[remaining];
            remaining -= list.Count;
        }
        return null;
    }

    public SortLevelData GetLevel(int globalIndex)
    {
        var asset = GetLevelAssetByGlobalIndex(globalIndex);
        return asset != null ? asset.GetData() : null;
    }

    public int GetGlobalIndexForAsset(SortLevelAsset asset)
    {
        if (asset == null || maps == null) return -1;
        int global = 0;
        for (int m = 0; m < maps.Count; m++)
        {
            var list = maps[m].levels;
            if (list == null) continue;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == asset) return global + i;
            }
            global += list.Count;
        }
        return -1;
    }

    public int GetMapIndexForGlobalIndex(int globalIndex)
    {
        if (maps == null || globalIndex < 0) return -1;
        int remaining = globalIndex;
        for (int m = 0; m < maps.Count; m++)
        {
            var list = maps[m].levels;
            int count = list != null ? list.Count : 0;
            if (remaining < count) return m;
            remaining -= count;
        }
        return -1;
    }

    public int GetDisplayLevelNumber(int globalIndex)
    {
        if (maps == null || globalIndex < 0) return 1;
        if (levelNumberContinuesAcrossMaps)
            return globalIndex + 1;
        int mapIndex = GetMapIndexForGlobalIndex(globalIndex);
        if (mapIndex < 0) return 1;
        int sum = 0;
        for (int m = 0; m < mapIndex; m++)
            sum += GetLevelCountInMap(m);
        return (globalIndex - sum) + 1;
    }

    public ResolvedLevelSettings GetResolvedSettings(int globalIndex)
    {
        var def = new ResolvedLevelSettings
        {
            levelDurationSeconds = 60f,
            undoCount = 3,
            destroyBranchWhenComplete = true,
            backgroundTheme = null,
            audioId = null
        };
        var asset = GetLevelAssetByGlobalIndex(globalIndex);
        int mapIndex = GetMapIndexForGlobalIndex(globalIndex);
        if (asset == null || mapIndex < 0 || maps == null || mapIndex >= maps.Count)
            return def;
        var data = asset.GetData();
        var map = maps[mapIndex];
        if (data == null) return def;
        if (data.useGlobalSettings)
        {
            return new ResolvedLevelSettings
            {
                levelDurationSeconds = map.globalLevelDurationSeconds > 0f ? map.globalLevelDurationSeconds : 60f,
                undoCount = map.globalUndoCount >= 0 ? map.globalUndoCount : 3,
                destroyBranchWhenComplete = map.globalDestroyBranchWhenComplete,
                backgroundTheme = map.globalBackgroundTheme,
                audioId = map.globalAudioId
            };
        }
        return new ResolvedLevelSettings
        {
            levelDurationSeconds = data.levelDurationSeconds > 0f ? data.levelDurationSeconds : 60f,
            undoCount = data.undoCount >= 0 ? data.undoCount : 3,
            destroyBranchWhenComplete = data.destroyBranchWhenComplete,
            backgroundTheme = data.backgroundTheme,
            audioId = data.audioId
        };
    }
}
