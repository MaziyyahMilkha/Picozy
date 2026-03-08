using UnityEngine;

[System.Serializable]
public class SortKindEntry
{
    public string displayName = "";
    public Color color = Color.gray;
}

[CreateAssetMenu(fileName = "SortKindSettings", menuName = "Sort/Kind Settings", order = 0)]
public class SortKindSettings : ScriptableObject
{
    public SortKindEntry[] entries = new SortKindEntry[]
    {
        new SortKindEntry { displayName = "Tomato", color = new Color(0.95f, 0.2f, 0.15f, 1f) },
        new SortKindEntry { displayName = "Mushroom", color = new Color(0.7f, 0.5f, 0.3f, 1f) },
        new SortKindEntry { displayName = "Red Onion", color = new Color(0.65f, 0.15f, 0.2f, 1f) },
        new SortKindEntry { displayName = "Daisy", color = new Color(1f, 0.92f, 0.4f, 1f) },
        new SortKindEntry { displayName = "Coconut", color = new Color(0.45f, 0.3f, 0.15f, 1f) },
        new SortKindEntry { displayName = "Empty", color = new Color(0.78f, 0.8f, 0.85f, 1f) }
    };

    private static SortKindSettings _instance;

    public static SortKindSettings Instance
    {
        get
        {
            if (_instance == null)
                _instance = Resources.Load<SortKindSettings>("SortKindSettings");
            return _instance;
        }
    }

    public static int Count
    {
        get
        {
            var so = Instance;
            return so != null && so.entries != null && so.entries.Length > 0 ? so.entries.Length : 6;
        }
    }

    public string GetDisplayNameByIndex(int index)
    {
        if (entries == null || index < 0 || index >= entries.Length) return "?";
        var e = entries[index];
        return string.IsNullOrEmpty(e.displayName) ? "Kind " + index : e.displayName;
    }

    public Color GetColorByIndex(int index)
    {
        if (entries == null || index < 0 || index >= entries.Length) return Color.gray;
        return entries[index].color;
    }

    public int EmptyIndex => entries != null && entries.Length > 0 ? entries.Length - 1 : 5;

    public bool IsEmptyIndex(int index) => index == EmptyIndex;
}
