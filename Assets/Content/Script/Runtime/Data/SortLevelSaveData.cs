using System;

[Serializable]
public class SortLevelSaveData
{
    public int highestCompletedGlobalIndex = -1;
    public long lastSavedTimestampUtc;
}
