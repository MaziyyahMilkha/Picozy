using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SortKarakter))]
public class SortKarakterEditor : Editor
{
    private static int KindCount => SortKindSettings.Count;

    private int _lastKindCountForRepaint = -1;
    private string _lastKindNamesForRepaint = "";

    private void OnEnable()
    {
        EditorApplication.update += CheckKindSettingsAndRepaint;
        SortKindSettingsEditor.OnKindSettingsChanged += Repaint;
    }

    private void OnDisable()
    {
        EditorApplication.update -= CheckKindSettingsAndRepaint;
        SortKindSettingsEditor.OnKindSettingsChanged -= Repaint;
    }

    private void CheckKindSettingsAndRepaint()
    {
        int count = KindCount;
        string names = (SortKindColors.GetDisplayNameByIndex(0) ?? "") + "|" + (SortKindColors.GetDisplayNameByIndex(1) ?? "") + "|" + (SortKindColors.GetDisplayNameByIndex(2) ?? "");
        if (count != _lastKindCountForRepaint || names != _lastKindNamesForRepaint)
        {
            _lastKindCountForRepaint = count;
            _lastKindNamesForRepaint = names;
            Repaint();
        }
    }

    public override void OnInspectorGUI()
    {
        SortKindSettings.ClearCacheForEditor();

        serializedObject.Update();

        SerializedProperty kind = serializedObject.FindProperty("kind");
        SerializedProperty kindVisuals = serializedObject.FindProperty("kindVisuals");

        int count = KindCount;
        if (kindVisuals.arraySize != count)
            kindVisuals.arraySize = count;

        var labels = new string[count];
        for (int i = 0; i < count; i++) labels[i] = SortKindColors.GetDisplayNameByIndex(i);
        int current = kind.intValue;
        int sel = EditorGUILayout.Popup("Kind", Mathf.Clamp(current, 0, count - 1), labels);
        if (sel >= 0 && sel < count) kind.intValue = sel;

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Visual per kind", EditorStyles.boldLabel);

        for (int i = 0; i < kindVisuals.arraySize; i++)
        {
            string label = i < count ? (i + ": " + labels[i]) : (i + ": ?");
            EditorGUILayout.PropertyField(kindVisuals.GetArrayElementAtIndex(i), new GUIContent(label));
        }

        serializedObject.ApplyModifiedProperties();
    }
}
