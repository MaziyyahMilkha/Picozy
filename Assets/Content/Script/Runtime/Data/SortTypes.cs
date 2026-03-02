using UnityEngine;

public enum SortKind
{
    Tomato,
    Mushroom,
    RedOnion,
    Daisy,
    Coconut,
    Empty
}

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

    public string GetDisplayName(SortKind kind)
    {
        int i = (int)kind;
        if (entries == null || i < 0 || i >= entries.Length) return kind.ToString();
        var e = entries[i];
        return string.IsNullOrEmpty(e.displayName) ? kind.ToString() : e.displayName;
    }

    public Color GetColor(SortKind kind)
    {
        int i = (int)kind;
        if (entries == null || i < 0 || i >= entries.Length) return Color.gray;
        return entries[i].color;
    }
}

public static class SortKindColors
{
    private static readonly Color[] DefaultColors = new Color[]
    {
        new Color(0.95f, 0.2f, 0.15f, 1f),
        new Color(0.7f, 0.5f, 0.3f, 1f),
        new Color(0.65f, 0.15f, 0.2f, 1f),
        new Color(1f, 0.92f, 0.4f, 1f),
        new Color(0.45f, 0.3f, 0.15f, 1f),
        new Color(0.78f, 0.8f, 0.85f, 1f)
    };

    public static Color[] Colors
    {
        get
        {
            var so = SortKindSettings.Instance;
            if (so != null && so.entries != null && so.entries.Length >= DefaultColors.Length)
            {
                var c = new Color[so.entries.Length];
                for (int i = 0; i < c.Length; i++)
                    c[i] = so.entries[i].color;
                return c;
            }
            return DefaultColors;
        }
    }

    public static Color Get(SortKind kind)
    {
        int i = (int)kind;
        var so = SortKindSettings.Instance;
        if (so != null && so.entries != null && i >= 0 && i < so.entries.Length)
            return so.entries[i].color;
        return i >= 0 && i < DefaultColors.Length ? DefaultColors[i] : Color.gray;
    }

    public static Color GetByIndex(int index)
    {
        var so = SortKindSettings.Instance;
        if (so != null) return so.GetColorByIndex(index);
        return index >= 0 && index < DefaultColors.Length ? DefaultColors[index] : Color.gray;
    }

    public static string GetDisplayNameByIndex(int index)
    {
        var so = SortKindSettings.Instance;
        if (so != null) return so.GetDisplayNameByIndex(index);
        return index >= 0 && index < DefaultColors.Length ? ((SortKind)index).ToString() : "?";
    }

    public static string GetDisplayName(SortKind kind)
    {
        var so = SortKindSettings.Instance;
        if (so != null) return so.GetDisplayName(kind);
        return kind.ToString();
    }
}
