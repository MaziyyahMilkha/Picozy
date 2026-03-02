using System;
using UnityEngine;

[Serializable]
public class BranchEntry
{
    public SortKind[] slots = new SortKind[8];

    public bool IsEmpty(int slotsPerBranch)
    {
        if (slots == null) return true;
        int n = Mathf.Min(slotsPerBranch, slots.Length);
        for (int i = 0; i < n; i++)
            if (slots[i] != SortKind.Empty) return false;
        return true;
    }
}

[Serializable]
public class SortLevelData
{
    public int slotsPerBranch = 4;
    public int kindMask = 15;
    public float levelDurationSeconds = 60f;
    public int undoCount = 3;
    public bool destroyBranchWhenComplete = true;
    public Sprite backgroundTheme;
    public BranchEntry[] leftBranches = new BranchEntry[0];
    public BranchEntry[] rightBranches = new BranchEntry[0];
}
