using UnityEngine;
using UnityEditor;
using System.IO;

public class PicozyKindEditorWindow : EditorWindow
{
    private SortKindSettings _settings;
    private SerializedObject _serialized;
    private SerializedProperty _entries;
    private Vector2 _scroll;
    private static readonly string[] EnumNames = System.Enum.GetNames(typeof(SortKind));
    private const string ResourcesPath = "Assets/Content/Resources";
    private const string AssetPath = "Assets/Content/Resources/SortKindSettings.asset";

    [MenuItem("Window/Picozy/Kind Editor")]
    public static void Open()
    {
        var w = GetWindow<PicozyKindEditorWindow>(false, "Picozy Kind Editor", true);
        w.minSize = new Vector2(360f, 380f);
    }

    private void OnEnable()
    {
        EnsureSettings();
        RefreshSerialized();
    }

    private void EnsureSettings()
    {
        if (_settings != null) return;
        _settings = Resources.Load<SortKindSettings>("SortKindSettings");
        if (_settings != null) return;
        if (!AssetDatabase.IsValidFolder("Assets/Content")) AssetDatabase.CreateFolder("Assets", "Content");
        if (!AssetDatabase.IsValidFolder(ResourcesPath)) AssetDatabase.CreateFolder("Assets/Content", "Resources");
        if (!File.Exists(AssetPath))
        {
            var so = CreateInstance<SortKindSettings>();
            AssetDatabase.CreateAsset(so, AssetPath);
            AssetDatabase.SaveAssets();
        }
        _settings = AssetDatabase.LoadAssetAtPath<SortKindSettings>(AssetPath);
    }

    private void RefreshSerialized()
    {
        if (_settings == null) { _serialized = null; _entries = null; return; }
        _serialized = new SerializedObject(_settings);
        _entries = _serialized.FindProperty("entries");
    }

    private void OnGUI()
    {
        EnsureSettings();
        if (_settings == null)
        {
            EditorGUILayout.HelpBox("Could not load or create SortKindSettings.", MessageType.Warning);
            return;
        }

        _serialized.Update();
        if (_entries == null) { RefreshSerialized(); _serialized?.ApplyModifiedProperties(); return; }

        int n = _entries.arraySize;
        if (n < 6)
        {
            _entries.arraySize = 6;
            for (int i = n; i < 6; i++)
            {
                var e = _entries.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("displayName").stringValue = i < 5 ? EnumNames[i] : "Empty";
                if (i == 5) e.FindPropertyRelative("color").colorValue = new Color(0.78f, 0.8f, 0.85f, 1f);
            }
        }
        int lastIdx = _entries.arraySize - 1;
        var lastEntry = _entries.GetArrayElementAtIndex(lastIdx);
        lastEntry.FindPropertyRelative("displayName").stringValue = "Empty";

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Kinds (name + color). Empty is always last.", EditorStyles.boldLabel);
        EditorGUILayout.Space(4f);

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        for (int i = 0; i < _entries.arraySize; i++)
        {
            var entry = _entries.GetArrayElementAtIndex(i);
            SerializedProperty displayName = entry.FindPropertyRelative("displayName");
            SerializedProperty color = entry.FindPropertyRelative("color");
            bool isLastEmpty = (i == _entries.arraySize - 1);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField((i + 1) + ".", GUILayout.Width(24f));
            EditorGUILayout.LabelField(i < EnumNames.Length ? EnumNames[i] : ("#" + i), GUILayout.Width(52f));
            if (isLastEmpty)
            {
                GUI.enabled = false;
                EditorGUILayout.TextField("Empty", GUILayout.MinWidth(100f));
                GUI.enabled = true;
            }
            else
                displayName.stringValue = EditorGUILayout.TextField(displayName.stringValue, GUILayout.MinWidth(100f));
            color.colorValue = EditorGUILayout.ColorField(color.colorValue, GUILayout.Width(70f));
            if (isLastEmpty)
                EditorGUILayout.LabelField("(required)", EditorStyles.miniLabel, GUILayout.Width(56f));
            else if (GUILayout.Button("Delete", GUILayout.Width(48f)))
            {
                _entries.DeleteArrayElementAtIndex(i);
                break;
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(10f);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add", GUILayout.Height(26f)))
        {
            _entries.InsertArrayElementAtIndex(lastIdx);
            _entries.GetArrayElementAtIndex(lastIdx).FindPropertyRelative("displayName").stringValue = "New";
        }
        if (GUILayout.Button("Reload", GUILayout.Height(26f)))
        {
            _settings = Resources.Load<SortKindSettings>("SortKindSettings");
            RefreshSerialized();
        }
        if (GUILayout.Button("Save", GUILayout.Height(26f)))
        {
            _serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(_settings);
            AssetDatabase.SaveAssets();
        }
        EditorGUILayout.EndHorizontal();

        _serialized.ApplyModifiedProperties();
    }
}
