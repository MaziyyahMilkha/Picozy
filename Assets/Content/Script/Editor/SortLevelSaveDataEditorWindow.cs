using UnityEditor;
using UnityEngine;

public class SortLevelSaveDataEditorWindow : EditorWindow
{
    private const string DefaultDatabasePath = "Assets/Content/Data/SortLevelDatabase.asset";
    private const float LevelNumWidth = 56f;
    private const float StatusWidth = 88f;
    private const float BtnResetWidth = 56f;
    private const float BtnSetTamatWidth = 72f;
    private const float BtnStarWidth = 22f;

    [SerializeField] private SortLevelDatabase database;

    private bool _hasSave;
    private SortLevelSaveData _cachedData;
    private Vector2 _scroll;
    private int _totalLevels;

    [MenuItem("Sort/Debug - Save Data (ES3)")]
    public static void Open()
    {
        var w = GetWindow<SortLevelSaveDataEditorWindow>("Save Data");
        w.minSize = new Vector2(440, 360);
    }

    private void OnEnable()
    {
        if (database == null)
            database = AssetDatabase.LoadAssetAtPath<SortLevelDatabase>(DefaultDatabasePath);
        RefreshStatus();
    }

    private void OnFocus()
    {
        RefreshStatus();
    }

    private void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        EditorGUILayout.BeginVertical();

        DrawHeader();
        DrawSetup();
        DrawStatusAndReset();
        DrawMapLevels();
        DrawSetTamatSemua();

        EditorGUILayout.Space(8);
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndScrollView();
    }

    private void DrawHeader()
    {
        EditorGUILayout.Space(4);
        var titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 };
        EditorGUILayout.LabelField("Save Data (ES3)", titleStyle);
        EditorGUILayout.LabelField("Level progress — debug only", EditorStyles.miniLabel);
        EditorGUILayout.Space(4);
    }

    private void DrawSetup()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Setup", EditorStyles.boldLabel);
        _totalLevels = GetTotalLevelCount();
        EditorGUI.BeginChangeCheck();
        database = (SortLevelDatabase)EditorGUILayout.ObjectField("Level Database", database, typeof(SortLevelDatabase), false);
        if (EditorGUI.EndChangeCheck())
            RefreshStatus();
        if (database != null)
            EditorGUILayout.LabelField("Total level", _totalLevels.ToString(), EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(4);
    }

    private void DrawStatusAndReset()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Status", GUILayout.Width(50));
        var statusStyle = new GUIStyle(EditorStyles.boldLabel);
        if (_hasSave)
            statusStyle.normal.textColor = new Color(0.2f, 0.6f, 0.2f);
        else
            statusStyle.normal.textColor = EditorStyles.label.normal.textColor;
        EditorGUILayout.LabelField(_hasSave ? "Save ada" : "Tidak ada save", statusStyle);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(4);

        var oldColor = GUI.backgroundColor;
        GUI.backgroundColor = new Color(1f, 0.85f, 0.85f);
        if (GUILayout.Button("Hapus semua save", GUILayout.Height(26)))
        {
            if (ES3.KeyExists(SortLevelSelectManager.SaveKey))
            {
                ES3.DeleteKey(SortLevelSelectManager.SaveKey);
                _cachedData = null;
                RefreshStatus();
                Debug.Log("[Sort Save Data Editor] Save dihapus.");
            }
            else
                Debug.Log("[Sort Save Data Editor] Tidak ada save.");
        }
        GUI.backgroundColor = oldColor;
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(6);
    }

    private void DrawMapLevels()
    {
        EditorGUILayout.LabelField("Per map & level", EditorStyles.boldLabel);
        EditorGUILayout.Space(2);

        if (database == null || database.maps == null || database.maps.Count == 0)
        {
            EditorGUILayout.HelpBox("Assign Level Database dan isi maps.", MessageType.None);
            return;
        }

        int globalIndex = 0;
        for (int m = 0; m < database.maps.Count; m++)
        {
            var map = database.maps[m];
            var list = map.levels;
            int levelCount = list != null ? list.Count : 0;
            string mapLabel = string.IsNullOrEmpty(map.id) ? $"Map {m + 1}" : map.id;
            int mapStartGlobalIndex = globalIndex;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField($"{mapLabel}", EditorStyles.boldLabel, GUILayout.Width(100));
            EditorGUILayout.LabelField($"{levelCount} level", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(2);

            bool numberContinues = database.levelNumberContinuesAcrossMaps;
            for (int i = 0; i < levelCount; i++, globalIndex++)
            {
                int displayLevelNum = numberContinues ? (globalIndex + 1) : (i + 1);
                bool tamat = IsLevelCompleted(globalIndex);
                int stars = GetStarsForLevel(globalIndex);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField($"Level {displayLevelNum}", GUILayout.Width(LevelNumWidth));
                EditorGUILayout.LabelField(tamat ? "Tamat" : "Belum", GUILayout.Width(StatusWidth));
                if (tamat)
                    EditorGUILayout.LabelField(stars + "★", GUILayout.Width(24));
                GUILayout.Space(4);
                if (GUILayout.Button("Reset", GUILayout.Width(BtnResetWidth)))
                {
                    SetStarsForLevel(globalIndex, 0);
                    Debug.Log($"[Sort Save Data Editor] {mapLabel} Level {displayLevelNum} di-reset.");
                }
                if (GUILayout.Button("Tamat", GUILayout.Width(48)))
                {
                    SetStarsUpToInMap(mapStartGlobalIndex, i, levelCount, 1, fillPreviousAsOneStar: true);
                    Debug.Log($"[Sort Save Data Editor] {mapLabel} Level {displayLevelNum} set tamat (1★).");
                }
                if (GUILayout.Button("1", GUILayout.Width(BtnStarWidth)))
                {
                    SetStarsUpToInMap(mapStartGlobalIndex, i, levelCount, 1, fillPreviousAsOneStar: true);
                    Debug.Log($"[Sort Save Data Editor] {mapLabel} Level {displayLevelNum} → 1★.");
                }
                if (GUILayout.Button("2", GUILayout.Width(BtnStarWidth)))
                {
                    SetStarsUpToInMap(mapStartGlobalIndex, i, levelCount, 2, fillPreviousAsOneStar: true);
                    Debug.Log($"[Sort Save Data Editor] {mapLabel} Level {displayLevelNum} → 2★.");
                }
                if (GUILayout.Button("3", GUILayout.Width(BtnStarWidth)))
                {
                    SetStarsUpToInMap(mapStartGlobalIndex, i, levelCount, 3, fillPreviousAsOneStar: true);
                    Debug.Log($"[Sort Save Data Editor] {mapLabel} Level {displayLevelNum} → 3★.");
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4);
        }
    }

    private void DrawSetTamatSemua()
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        var oldColor = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.85f, 1f, 0.9f);
        if (GUILayout.Button("Set Tamat Semua", GUILayout.Height(28)))
        {
            SetAllStars(1);
            Debug.Log($"[Sort Save Data Editor] Semua {_totalLevels} level set tamat.");
        }
        GUI.backgroundColor = oldColor;
        EditorGUILayout.EndVertical();
    }

    private int GetTotalLevelCount()
    {
        return database != null ? database.GetTotalLevelCount() : 0;
    }

    private bool IsLevelCompleted(int globalIndex)
    {
        if (_cachedData == null) return false;
        if (_cachedData.starsPerLevel == null) return false;
        if (globalIndex < 0 || globalIndex >= _cachedData.starsPerLevel.Count) return false;
        return _cachedData.starsPerLevel[globalIndex] > 0;
    }

    private int GetStarsForLevel(int globalIndex)
    {
        if (_cachedData == null || _cachedData.starsPerLevel == null) return 0;
        if (globalIndex < 0 || globalIndex >= _cachedData.starsPerLevel.Count) return 0;
        return Mathf.Clamp(_cachedData.starsPerLevel[globalIndex], 0, 3);
    }

    private void SetHighestCompleted(int newHighest)
    {
        if (newHighest < 0)
        {
            SetAllStars(0);
            return;
        }
        SetStarsForLevel(newHighest, 1);
    }

    private void SetStarsForLevel(int globalIndex, int starCount)
    {
        var data = LoadOrCreateSaveData();
        if (data.starsPerLevel == null) data.starsPerLevel = new System.Collections.Generic.List<int>();
        while (data.starsPerLevel.Count <= globalIndex)
            data.starsPerLevel.Add(0);
        data.starsPerLevel[globalIndex] = Mathf.Clamp(starCount, 0, 3);
        data.highestCompletedGlobalIndex = ComputeHighestCompletedIndex(data);
        data.lastSavedTimestampUtc = System.DateTime.UtcNow.Ticks;
        ES3.Save(SortLevelSelectManager.SaveKey, data);
        _cachedData = data;
        RefreshStatus();
    }

    private void SetStarsUpToInMap(int mapStartGlobalIndex, int localIndexInclusive, int mapLevelCount, int starCountForTarget, bool fillPreviousAsOneStar)
    {
        if (localIndexInclusive < 0) return;
        if (mapLevelCount <= 0) return;
        if (database == null) return;

        int clampedLocal = Mathf.Clamp(localIndexInclusive, 0, mapLevelCount - 1);
        int targetGlobal = mapStartGlobalIndex + clampedLocal;

        var data = LoadOrCreateSaveData();
        if (data.starsPerLevel == null) data.starsPerLevel = new System.Collections.Generic.List<int>();
        while (data.starsPerLevel.Count < mapStartGlobalIndex + mapLevelCount)
            data.starsPerLevel.Add(0);

        if (fillPreviousAsOneStar)
        {
            for (int g = mapStartGlobalIndex; g < targetGlobal; g++)
            {
                if (g < 0 || g >= data.starsPerLevel.Count) continue;
                if (data.starsPerLevel[g] <= 0)
                    data.starsPerLevel[g] = 1;
            }
        }

        data.starsPerLevel[targetGlobal] = Mathf.Clamp(starCountForTarget, 0, 3);
        data.highestCompletedGlobalIndex = ComputeHighestCompletedIndex(data);
        data.lastSavedTimestampUtc = System.DateTime.UtcNow.Ticks;
        ES3.Save(SortLevelSelectManager.SaveKey, data);
        _cachedData = data;
        RefreshStatus();
    }

    private void SetAllStars(int starCount)
    {
        var data = LoadOrCreateSaveData();
        int total = GetTotalLevelCount();
        int s = Mathf.Clamp(starCount, 0, 3);
        if (data.starsPerLevel == null) data.starsPerLevel = new System.Collections.Generic.List<int>(total);
        data.starsPerLevel.Clear();
        for (int i = 0; i < total; i++)
            data.starsPerLevel.Add(s);
        data.highestCompletedGlobalIndex = ComputeHighestCompletedIndex(data);
        data.lastSavedTimestampUtc = System.DateTime.UtcNow.Ticks;
        ES3.Save(SortLevelSelectManager.SaveKey, data);
        _cachedData = data;
        RefreshStatus();
    }

    private static int ComputeHighestCompletedIndex(SortLevelSaveData data)
    {
        if (data == null || data.starsPerLevel == null) return -1;
        for (int i = data.starsPerLevel.Count - 1; i >= 0; i--)
            if (data.starsPerLevel[i] > 0) return i;
        return -1;
    }

    private SortLevelSaveData LoadOrCreateSaveData()
    {
        if (ES3.KeyExists(SortLevelSelectManager.SaveKey))
        {
            try { return ES3.Load<SortLevelSaveData>(SortLevelSelectManager.SaveKey); }
            catch { }
        }
        return new SortLevelSaveData { highestCompletedGlobalIndex = -1, lastSavedTimestampUtc = System.DateTime.UtcNow.Ticks, starsPerLevel = new System.Collections.Generic.List<int>() };
    }

    private void RefreshStatus()
    {
        _hasSave = ES3.KeyExists(SortLevelSelectManager.SaveKey);
        if (_hasSave)
        {
            try { _cachedData = ES3.Load<SortLevelSaveData>(SortLevelSelectManager.SaveKey); }
            catch { _cachedData = null; }
        }
        else
            _cachedData = null;
    }
}
