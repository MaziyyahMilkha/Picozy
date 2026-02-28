using System;
using UnityEngine;

[Serializable]
public class DahanEntry
{
    public SortKind[] slots = new SortKind[8];

    public bool IsEmpty(int slotPerDahan)
    {
        if (slots == null) return true;
        int n = Mathf.Min(slotPerDahan, slots.Length);
        for (int i = 0; i < n; i++)
            if (slots[i] != SortKind.Kosong) return false;
        return true;
    }
}

[Serializable]
public class SortLevelData
{
    public int slotPerDahan = 4;
    public int kindMask = 15;
    public bool randomEachPlay = true;
    public DahanEntry[] leftDahans = new DahanEntry[0];
    public DahanEntry[] rightDahans = new DahanEntry[0];
}
