using System;
using System.Collections.Generic;

[Serializable]
public class SortLevelSaveData
{
    public int highestCompletedGlobalIndex = -1;
    public long lastSavedTimestampUtc;
    public List<int> starsPerLevel = new List<int>();
}
