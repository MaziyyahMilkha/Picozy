using UnityEngine;

public static class SortLevelRules
{
    public static void ProcessCompleteDahan(SortDahan dahan, SortLevelData data)
    {
        if (dahan == null) return;
        if (data != null && data.destroyBranchWhenComplete)
            Object.Destroy(dahan.gameObject);
        else
            dahan.CollectAndClear();
    }
}
