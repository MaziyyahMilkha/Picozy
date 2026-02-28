using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SortKarakter))]
public class SortKarakterEditor : Editor
{
    private static readonly string[] KindLabels = System.Enum.GetNames(typeof(SortKind));

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty kind = serializedObject.FindProperty("kind");
        SerializedProperty kindVisuals = serializedObject.FindProperty("kindVisuals");

        EditorGUILayout.PropertyField(kind, new GUIContent("Kind (default Tomat)"));

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Visual per kind", EditorStyles.boldLabel);

        if (kindVisuals.arraySize != KindLabels.Length)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Resize ke " + KindLabels.Length + " (sesuai enum)"))
                kindVisuals.arraySize = KindLabels.Length;
            EditorGUILayout.EndHorizontal();
        }

        for (int i = 0; i < kindVisuals.arraySize; i++)
        {
            string label = i < KindLabels.Length ? (i + ": " + KindLabels[i]) : (i + ": ?");
            EditorGUILayout.PropertyField(kindVisuals.GetArrayElementAtIndex(i), new GUIContent(label));
        }

        serializedObject.ApplyModifiedProperties();
    }
}
