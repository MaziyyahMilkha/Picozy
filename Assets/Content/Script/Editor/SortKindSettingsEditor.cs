using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SortKindSettings))]
public class SortKindSettingsEditor : Editor
{
    public static System.Action OnKindSettingsChanged;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (GUI.changed)
        {
            if (OnKindSettingsChanged != null)
                OnKindSettingsChanged.Invoke();
        }
    }
}
