using System;
using UnityEngine;

[Serializable]
public class BranchEntry
{
    public int[] slots = new int[8];

    public bool IsEmpty(int slotsPerBranch)
    {
        if (slots == null) return true;
        int emptyIdx = SortKindSettings.Instance != null ? SortKindSettings.Instance.EmptyIndex : 0;
        int n = Mathf.Min(slotsPerBranch, slots.Length);
        for (int i = 0; i < n; i++)
            if (slots[i] != emptyIdx) return false;
        return true;
    }
}

[Serializable]
public class SortLevelData
{
    public int slotsPerBranch = 4;
    public int[] kindMultipliers = new int[16];
    public int kindMask = 15;

    [Tooltip("True = pakai global settings dari map. False = pakai setting di bawah ini.")]
    public bool useGlobalSettings = true;

    public float levelDurationSeconds = 60f;
    public int undoCount = 3;
    public bool destroyBranchWhenComplete = true;
    public Sprite backgroundTheme;
    public string audioId;
    public BranchEntry[] leftBranches = new BranchEntry[0];
    public BranchEntry[] rightBranches = new BranchEntry[0];
}
