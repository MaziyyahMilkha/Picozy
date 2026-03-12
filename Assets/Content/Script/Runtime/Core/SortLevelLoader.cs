using System.Collections.Generic;
using UnityEngine;

public class SortLevelLoader : MonoBehaviour
{
    [Header("Level")]
    [SerializeField] private SortLevelAsset currentLevel;
    [SerializeField] private SortLevelDatabase database;

    [Header("Spawn")]
    [SerializeField] private Transform[] leftSpawnPoints = new Transform[0];
    [SerializeField] private Transform[] rightSpawnPoints = new Transform[0];

    [Header("Prefab")]
    [SerializeField] private GameObject leftDahanPrefab;
    [SerializeField] private GameObject rightDahanPrefab;
    [SerializeField] private GameObject characterPrefab;

    private List<SortDahan> spawnedDahans = new List<SortDahan>();

    private void Start()
    {
        LoadLevel();
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

    public int GetLevelIndexInDatabase()
    {
        if (database == null || currentLevel == null || database.levels == null) return -1;
        for (int i = 0; i < database.levels.Length; i++)
            if (database.levels[i] == currentLevel) return i;
        return -1;
    }

    private void Clear()
    {
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
            }
        }
    }
}
