using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SortKarakter))]
public class SortKarakterEditor : Editor
{
    private static int KindCount => SortKindSettings.Count;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty kind = serializedObject.FindProperty("kind");
        SerializedProperty kindVisuals = serializedObject.FindProperty("kindVisuals");

        int count = KindCount;
        var labels = new string[count];
        for (int i = 0; i < count; i++) labels[i] = SortKindColors.GetDisplayNameByIndex(i);
        int current = kind.intValue;
        int sel = EditorGUILayout.Popup("Kind", Mathf.Clamp(current, 0, count - 1), labels);
        if (sel >= 0 && sel < count) kind.intValue = sel;

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Visual per kind", EditorStyles.boldLabel);

        if (kindVisuals.arraySize != count)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Resize to " + count))
                kindVisuals.arraySize = count;
            EditorGUILayout.EndHorizontal();
        }

        for (int i = 0; i < kindVisuals.arraySize; i++)
        {
            string label = i < count ? (i + ": " + labels[i]) : (i + ": ?");
            EditorGUILayout.PropertyField(kindVisuals.GetArrayElementAtIndex(i), new GUIContent(label));
        }

        serializedObject.ApplyModifiedProperties();
    }
}
