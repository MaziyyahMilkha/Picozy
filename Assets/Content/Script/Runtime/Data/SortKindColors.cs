using UnityEngine;

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
        return index >= 0 && index < DefaultColors.Length ? ("Kind " + index) : "?";
    }
}
