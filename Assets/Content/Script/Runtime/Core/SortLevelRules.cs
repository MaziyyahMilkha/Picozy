using UnityEngine;

public static class SortLevelRules
{
    public static void ProcessCompleteDahan(SortDahan dahan, bool destroyBranchWhenComplete)
    {
        if (dahan == null) return;
        if (destroyBranchWhenComplete)
            dahan.CollectAndDestroyAfterFeedback();
        else
            dahan.CollectAndClear();
    }
}
