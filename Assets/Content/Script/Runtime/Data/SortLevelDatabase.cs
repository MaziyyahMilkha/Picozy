using UnityEngine;

[CreateAssetMenu(fileName = "SortLevelDatabase", menuName = "Sort/Level Database")]
public class SortLevelDatabase : ScriptableObject
{
    public SortLevelAsset[] levels = new SortLevelAsset[0];

    public int LevelCount => levels != null ? levels.Length : 0;

    public SortLevelData GetLevel(int index)
    {
        if (levels == null || index < 0 || index >= levels.Length)
            return null;
        var asset = levels[index];
        return asset != null ? asset.GetData() : null;
    }
}
