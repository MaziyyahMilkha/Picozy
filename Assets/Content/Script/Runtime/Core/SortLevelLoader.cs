using System.Collections.Generic;
using UnityEngine;

public class SortLevelLoader : MonoBehaviour
{
    [Header("Level")]
    private SortLevelAsset currentLevel;
    [SerializeField] private SortLevelDatabase database;

    [Header("Spawn")]
    [SerializeField] private Transform[] leftSpawnPoints = new Transform[0];
    [SerializeField] private Transform[] rightSpawnPoints = new Transform[0];

    [Header("Prefab")]
    [SerializeField] private GameObject leftDahanPrefab;
    [SerializeField] private GameObject rightDahanPrefab;
    [SerializeField] private GameObject characterPrefab;

    private List<SortDahan> spawnedDahans = new List<SortDahan>();
    private int _spawnedCharacterCount;

    private void OnEnable()
    {
        SortEventManager.SubscribeAction("Level", OnLevelEvent);
    }

    private void OnDisable()
    {
        SortEventManager.UnsubscribeAction("Level", OnLevelEvent);
    }

    private void OnLevelEvent(string levelIndexStr)
    {
        if (database == null) return;
        if (!int.TryParse(levelIndexStr, out int globalIndex) || globalIndex < 0 || globalIndex >= database.GetTotalLevelCount()) return;
        SortLevelAsset asset = database.GetLevelAssetByGlobalIndex(globalIndex);
        if (asset == null) return;
        SetLevel(asset);
        LoadLevel();
        SortEventManager.Publish(new UIActionEvent("LevelLoaded", levelIndexStr));
    }

    public void LoadLevel()
    {
        if (currentLevel == null) return;
        SortLevelData data = currentLevel.GetData();
        if (data == null) return;

        if (characterPrefab == null)
        {
            Debug.LogError("SortLevelLoader: characterPrefab is missing.");
            return;
        }
        if (leftDahanPrefab == null && rightDahanPrefab == null)
        {
            Debug.LogError("SortLevelLoader: at least one branch prefab required.");
            return;
        }
        if (leftDahanPrefab == null) leftDahanPrefab = rightDahanPrefab;
        if (rightDahanPrefab == null) rightDahanPrefab = leftDahanPrefab;

        Clear();
        SpawnLevel(data);
    }

    public void SetLevel(SortLevelAsset level)
    {
        currentLevel = level;
    }

    public SortLevelAsset GetCurrentLevel() => currentLevel;

    public void UnloadLevel()
    {
        Clear();
        currentLevel = null;
    }

    public int GetLevelIndexInDatabase()
    {
        return database != null && currentLevel != null ? database.GetGlobalIndexForAsset(currentLevel) : -1;
    }

    public int GetTotalLevelCount()
    {
        return database != null ? database.GetTotalLevelCount() : 0;
    }

    public int GetSpawnedCharacterCountForCurrentLevel() => _spawnedCharacterCount;

    public int GetDisplayLevelNumber()
    {
        int idx = GetLevelIndexInDatabase();
        return database != null && idx >= 0 ? database.GetDisplayLevelNumber(idx) : 1;
    }

    public ResolvedLevelSettings GetResolvedLevelSettings()
    {
        int idx = GetLevelIndexInDatabase();
        if (database == null || idx < 0) return default;
        return database.GetResolvedSettings(idx);
    }

    private void Clear()
    {
        _spawnedCharacterCount = 0;
        for (int i = 0; i < spawnedDahans.Count; i++)
        {
            if (spawnedDahans[i] != null && spawnedDahans[i].gameObject != null)
                Destroy(spawnedDahans[i].gameObject);
        }
        spawnedDahans.Clear();
    }

    private void SpawnLevel(SortLevelData data)
    {
        int slotsPerBranch = Mathf.Clamp(data.slotsPerBranch, 1, 8);
        var leftBranches = data.leftBranches ?? new BranchEntry[0];
        var rightBranches = data.rightBranches ?? new BranchEntry[0];

        for (int i = 0; i < leftBranches.Length; i++)
        {
            Transform parent = GetSpawnParent(leftSpawnPoints, i);
            Vector3 pos = parent != null ? parent.position : new Vector3(-3f - i * 2f, 0f, 0f);
            var dahan = SpawnDahan(leftDahanPrefab, parent, pos);
            if (dahan != null)
            {
                dahan.SetTopIsHighIndex(true);
                spawnedDahans.Add(dahan);
                FillBranch(dahan, leftBranches[i], slotsPerBranch, isRight: false);
            }
        }

        for (int i = 0; i < rightBranches.Length; i++)
        {
            Transform parent = GetSpawnParent(rightSpawnPoints, i);
            Vector3 pos = parent != null ? parent.position : new Vector3(3f + i * 2f, 0f, 0f);
            var dahan = SpawnDahan(rightDahanPrefab, parent, pos);
            if (dahan != null)
            {
                dahan.SetTopIsHighIndex(true);
                spawnedDahans.Add(dahan);
                FillBranch(dahan, rightBranches[i], slotsPerBranch, isRight: true);
            }
        }
    }

    private Transform GetSpawnParent(Transform[] points, int index)
    {
        if (points == null || index < 0 || index >= points.Length) return null;
        return points[index];
    }

    private SortDahan SpawnDahan(GameObject prefab, Transform parent, Vector3 worldPos)
    {
        if (prefab == null) return null;
        Transform root = parent != null ? parent : transform;
        GameObject go = Instantiate(prefab, worldPos, Quaternion.identity, root);
        if (parent != null)
            go.transform.localPosition = Vector3.zero;
        return go.GetComponent<SortDahan>();
    }

    private void FillBranch(SortDahan dahan, BranchEntry entry, int slotsPerBranch, bool isRight)
    {
        if (dahan == null || entry == null || entry.slots == null) return;
        int emptyIdx = SortKindSettings.Instance != null ? SortKindSettings.Instance.EmptyIndex : 0;
        for (int s = 0; s < slotsPerBranch && s < entry.slots.Length; s++)
        {
            int kind = entry.slots[s];
            if (kind == emptyIdx) continue;
            Transform slotParent = dahan.GetSlotTransform(s);
            Vector3 slotPos = slotParent != null ? slotParent.position : dahan.GetSlotPosition(s);
            Transform parent = slotParent != null ? slotParent : dahan.transform;
            GameObject go = Instantiate(characterPrefab, slotPos, Quaternion.identity, parent);
            if (slotParent != null) go.transform.localPosition = Vector3.zero;
            SortKarakter character = go.GetComponent<SortKarakter>();
            if (character != null)
            {
                character.SetKind(kind);
                dahan.AddCharacterAtSlot(character, s);
                _spawnedCharacterCount++;
            }
        }
    }
}
