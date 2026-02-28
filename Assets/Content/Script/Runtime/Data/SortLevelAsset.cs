using UnityEngine;

[CreateAssetMenu(fileName = "SortLevel", menuName = "Sort/Level Asset")]
public class SortLevelAsset : ScriptableObject
{
    public SortLevelData data = new SortLevelData();

    public SortLevelData GetData() => data;
}
