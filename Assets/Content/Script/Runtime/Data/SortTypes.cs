using UnityEngine;

public enum SortKind
{
    Tomat,
    Jamur,
    BawangMerah,
    Daisy,
    Kelapa,
    Kosong
}

public static class SortKindColors
{
    public static readonly Color[] Colors = new Color[]
    {
        new Color(0.95f, 0.2f, 0.15f, 1f),   // Tomat - merah
        new Color(0.7f, 0.5f, 0.3f, 1f),     // Jamur - coklat
        new Color(0.65f, 0.15f, 0.2f, 1f),   // BawangMerah - merah gelap
        new Color(1f, 0.92f, 0.4f, 1f),       // Daisy - kuning
        new Color(0.45f, 0.3f, 0.15f, 1f),   // Kelapa - coklat gelap
        new Color(0.78f, 0.8f, 0.85f, 1f)    // Kosong - abu
    };

    public static Color Get(SortKind kind)
    {
        int i = (int)kind;
        return i >= 0 && i < Colors.Length ? Colors[i] : Color.gray;
    }
}
