using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

[CustomEditor(typeof(SortLevelAsset))]
public class SortLevelAssetEditor : Editor
{
    private const float SlotColorStripWidth = 8f;
    private const int MaxSlots = 8;
    private static int KindCountNoEmpty
    {
        get
        {
            var so = SortKindSettings.Instance;
            if (so == null || so.entries == null || so.entries.Length <= 1) return 0;
            return so.GetNonEmptyKindIndices().Length;
        }
    }
    private static int[] NonEmptyKindIndices
    {
        get
        {
            var so = SortKindSettings.Instance;
            return so != null ? so.GetNonEmptyKindIndices() : new int[0];
        }
    }
    private static string GetKindName(int index)
    {
        return SortKindColors.GetDisplayNameByIndex(index);
    }
    private static string[] KindNamesForMask
    {
        get
        {
            var indices = NonEmptyKindIndices;
            var arr = new string[indices.Length];
            for (int i = 0; i < arr.Length; i++) arr[i] = GetKindName(indices[i]);
            return arr;
        }
    }
    private static int EmptyIndexInEditor => SortKindSettings.Instance != null ? SortKindSettings.Instance.EmptyIndex : 0;
    private const int KindMultipliersCap = 16;

    private static void EnsureKindMultipliersSize(SerializedProperty data, int minSize)
    {
        var arr = data.FindPropertyRelative("kindMultipliers");
        if (arr == null) return;
        int need = Mathf.Clamp(minSize, 1, KindMultipliersCap);
        if (arr.arraySize < need)
        {
            int old = arr.arraySize;
            arr.arraySize = need;
            for (int i = old; i < need; i++)
                arr.GetArrayElementAtIndex(i).intValue = 1;
        }
    }

    private static int GetKindMultiplier(SerializedProperty data, int kindIndex)
    {
        var arr = data.FindPropertyRelative("kindMultipliers");
        if (arr == null || kindIndex < 0 || kindIndex >= arr.arraySize) return 1;
        return Mathf.Max(1, arr.GetArrayElementAtIndex(kindIndex).intValue);
    }

    private static readonly Color FilledBg = new Color(0.95f, 0.98f, 1f, 0.6f);
    private const float BranchRowHeight = 36f;
    private const float SlotRowHeight = 38f;
    private const float PaddingRight = 12f;
    private const float PaddingInner = 8f;
    private const float ButtonAreaWidth = 60f;
    private const float ChipWidth = 52f;
    private const float ChipHeight = 26f;
    private const float ChipGap = 5f;
    private const float LabelWidth = 32f;
    private static readonly Color ChipBorder = new Color(0.45f, 0.45f, 0.5f, 0.6f);

    private static Color[] KindColors => SortKindColors.Colors != null && SortKindColors.Colors.Length > 0 ? SortKindColors.Colors : FallbackKindColors;
    private static readonly Color[] FallbackKindColors = new Color[]
    {
        new Color(0.95f, 0.2f, 0.15f, 1f), new Color(0.7f, 0.5f, 0.3f, 1f), new Color(0.65f, 0.15f, 0.2f, 1f),
        new Color(1f, 0.92f, 0.4f, 1f), new Color(0.45f, 0.3f, 0.15f, 1f), new Color(0.78f, 0.8f, 0.85f, 1f)
    };

    private ReorderableList leftList;
    private ReorderableList rightList;
    private ReorderableList slotEditList;
    private bool editingLeft;
    private int editingIndex = -1;
    private SerializedProperty currentSlotsPerBranch;
    private int currentKindMask;
    private readonly int[] _kindCounts = new int[16];
    private readonly int[] _countWithoutSlot = new int[16];
    private readonly List<int> _optionKinds = new List<int>(16);
    private int _cachedKindMask = -1;
    private int[] _cachedAllowedKinds;
    private int currentSlotCount => currentSlotsPerBranch != null ? Mathf.Clamp(currentSlotsPerBranch.intValue, 1, MaxSlots) : 4;

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
        var so = SortKindSettings.Instance;
        int count = so != null && so.entries != null && so.entries.Length > 0 ? so.entries.Length - 1 : 5;
        string names = (GetKindName(0) ?? "") + "|" + (GetKindName(1) ?? "") + "|" + (GetKindName(2) ?? "");
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
        SerializedProperty data = serializedObject.FindProperty("data");
        if (data == null) { serializedObject.ApplyModifiedProperties(); return; }

        SerializedProperty slotsPerBranch = data.FindPropertyRelative("slotsPerBranch");
        SerializedProperty kindMultipliers = data.FindPropertyRelative("kindMultipliers");
        SerializedProperty kindMask = data.FindPropertyRelative("kindMask");
        SerializedProperty useGlobalSettings = data.FindPropertyRelative("useGlobalSettings");
        SerializedProperty levelDurationSeconds = data.FindPropertyRelative("levelDurationSeconds");
        SerializedProperty undoCount = data.FindPropertyRelative("undoCount");
        SerializedProperty destroyBranchWhenComplete = data.FindPropertyRelative("destroyBranchWhenComplete");
        SerializedProperty backgroundTheme = data.FindPropertyRelative("backgroundTheme");
        SerializedProperty audioId = data.FindPropertyRelative("audioId");
        SerializedProperty leftBranches = data.FindPropertyRelative("leftBranches");
        SerializedProperty rightBranches = data.FindPropertyRelative("rightBranches");

        EditorGUILayout.Space(4f);
        slotsPerBranch.intValue = Mathf.Clamp(EditorGUILayout.IntField("Slots per branch", slotsPerBranch.intValue), 1, MaxSlots);
        int kindCountFromSettings = SortKindSettings.Instance != null && SortKindSettings.Instance.entries != null ? SortKindSettings.Instance.entries.Length : KindMultipliersCap;
        EnsureKindMultipliersSize(data, kindCountFromSettings);
        int maskBits = (1 << KindCountNoEmpty) - 1;
        int currentMask = kindMask.intValue & maskBits;
        string kindsLabel = GetKindsMaskSummary(currentMask, maskBits);
        Rect kindsRect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
        if (EditorGUI.DropdownButton(kindsRect, new GUIContent("Kinds: " + kindsLabel), FocusType.Passive))
        {
            var menu = new GenericMenu();
            for (int i = 0; i < KindCountNoEmpty; i++)
            {
                int idx = i;
                bool on = (currentMask & (1 << i)) != 0;
                menu.AddItem(new GUIContent(KindNamesForMask[i]), on, () =>
                {
                    serializedObject.Update();
                    var d = serializedObject.FindProperty("data");
                    if (d != null)
                    {
                        var km = d.FindPropertyRelative("kindMask");
                        int m = (km.intValue & maskBits) ^ (1 << idx);
                        km.intValue = m;
                        serializedObject.ApplyModifiedProperties();
                    }
                    Repaint();
                });
            }
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Nothing"), currentMask == 0, () =>
            {
                serializedObject.Update();
                var d = serializedObject.FindProperty("data");
                if (d != null) { d.FindPropertyRelative("kindMask").intValue = 0; serializedObject.ApplyModifiedProperties(); }
                Repaint();
            });
            menu.AddItem(new GUIContent("Everything"), currentMask == maskBits, () =>
            {
                serializedObject.Update();
                var d = serializedObject.FindProperty("data");
                if (d != null) { d.FindPropertyRelative("kindMask").intValue = maskBits; serializedObject.ApplyModifiedProperties(); }
                Repaint();
            });
            menu.DropDown(kindsRect);
        }
        var indices = NonEmptyKindIndices;
        int currentMaskForMult = kindMask.intValue & maskBits;
        if (indices.Length > 0 && kindMultipliers != null)
        {
            EditorGUILayout.Space(2f);
            EditorGUILayout.LabelField("Branches per kind", EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel++;
            for (int i = 0; i < indices.Length; i++)
            {
                if ((currentMaskForMult & (1 << i)) == 0) continue;
                int kindIdx = indices[i];
                if (kindIdx >= kindMultipliers.arraySize) continue;
                var elem = kindMultipliers.GetArrayElementAtIndex(kindIdx);
                elem.intValue = Mathf.Max(1, EditorGUILayout.IntField(GetKindName(kindIdx), elem.intValue));
            }
            EditorGUI.indentLevel--;
        }
        if (useGlobalSettings != null)
            useGlobalSettings.boolValue = EditorGUILayout.Toggle("Use global settings", useGlobalSettings.boolValue);
        if (useGlobalSettings == null || !useGlobalSettings.boolValue)
        {
            EditorGUI.indentLevel++;
            levelDurationSeconds.floatValue = Mathf.Max(0f, EditorGUILayout.FloatField("Level duration (sec)", levelDurationSeconds.floatValue));
            undoCount.intValue = Mathf.Max(0, EditorGUILayout.IntField("Undo count", undoCount.intValue));
            if (destroyBranchWhenComplete != null)
                destroyBranchWhenComplete.boolValue = EditorGUILayout.Toggle("Destroy branch when complete", destroyBranchWhenComplete.boolValue);
            if (backgroundTheme != null)
                EditorGUILayout.PropertyField(backgroundTheme, new GUIContent("Background theme"));
            if (audioId != null)
                EditorGUILayout.PropertyField(audioId, new GUIContent("Audio ID", "Group ID in SortAudioData (e.g. level BGM). Leave empty if not used."));
            EditorGUI.indentLevel--;
        }

        int kindCount = CountKinds(kindMask.intValue);
        int slotsPer = Mathf.Clamp(slotsPerBranch.intValue, 1, MaxSlots);
        int filledSlots = 0;
        var selectedIndices = NonEmptyKindIndices;
        for (int i = 0; i < selectedIndices.Length; i++)
        {
            if ((kindMask.intValue & (1 << i)) == 0) continue;
            filledSlots += slotsPer * GetKindMultiplier(data, selectedIndices[i]);
        }
        int totalBranches = leftBranches.arraySize + rightBranches.arraySize;
        int totalSlots = totalBranches * slotsPer;
        int emptySlotCount = totalSlots - filledSlots;
        if (kindCount > 0)
            EditorGUILayout.HelpBox("Filled: " + filledSlots + ". Empty: " + emptySlotCount + ". Total: " + totalSlots, MessageType.Info);

        EditorGUILayout.Space(4f);

        currentSlotsPerBranch = slotsPerBranch;
        currentKindMask = kindMask.intValue;
        if (_cachedKindMask != currentKindMask) { _cachedKindMask = currentKindMask; _cachedAllowedKinds = GetKindsFromMask(currentKindMask); }
        EnsureSlotsSize(leftBranches, slotsPerBranch.intValue);
        EnsureSlotsSize(rightBranches, slotsPerBranch.intValue);

        string leftPath = leftBranches.propertyPath;
        string rightPath = rightBranches.propertyPath;
        if (leftList == null || leftList.serializedProperty.propertyPath != leftPath)
        {
            leftList = new ReorderableList(serializedObject, leftBranches, true, true, true, true)
            {
                elementHeight = BranchRowHeight,
                drawHeaderCallback = r => EditorGUI.LabelField(r, "Left branches"),
                drawElementCallback = (rect, index, active, focused) => DrawBranchRow(rect, data, leftBranches.GetArrayElementAtIndex(index), index, "L" + (index + 1), true)
            };
            leftList.onAddCallback = l => { leftBranches.arraySize++; InitEntry(leftBranches.GetArrayElementAtIndex(leftBranches.arraySize - 1)); };
        }
        if (rightList == null || rightList.serializedProperty.propertyPath != rightPath)
        {
            rightList = new ReorderableList(serializedObject, rightBranches, true, true, true, true)
            {
                elementHeight = BranchRowHeight,
                drawHeaderCallback = r => EditorGUI.LabelField(r, "Right branches"),
                drawElementCallback = (rect, index, active, focused) => DrawBranchRow(rect, data, rightBranches.GetArrayElementAtIndex(index), index, "R" + (index + 1), false)
            };
            rightList.onAddCallback = l => { rightBranches.arraySize++; InitEntry(rightBranches.GetArrayElementAtIndex(rightBranches.arraySize - 1)); };
        }

        float halfW = (EditorGUIUtility.currentViewWidth - 80f - PaddingRight * 2f) * 0.5f;
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(4f);
        EditorGUILayout.BeginVertical(GUILayout.Width(halfW));
        leftList.DoList(EditorGUILayout.GetControlRect(false, leftList.GetHeight()));
        EditorGUILayout.EndVertical();
        EditorGUILayout.BeginVertical(GUILayout.Width(80f));
        GUILayout.Space(22f);
        if (leftList.index >= 0 && leftList.index < leftBranches.arraySize && GUILayout.Button("→\nRight"))
            MoveBetween(leftBranches, rightBranches, leftList.index);
        if (rightList.index >= 0 && rightList.index < rightBranches.arraySize && GUILayout.Button("←\nLeft"))
            MoveBetween(rightBranches, leftBranches, rightList.index);
        EditorGUILayout.EndVertical();
        EditorGUILayout.BeginVertical(GUILayout.Width(halfW));
        rightList.DoList(EditorGUILayout.GetControlRect(false, rightList.GetHeight()));
        EditorGUILayout.EndVertical();
        GUILayout.Space(PaddingRight);
        EditorGUILayout.EndHorizontal();

        if (editingIndex >= 0)
        {
            SerializedProperty editBranches = editingLeft ? leftBranches : rightBranches;
            if (editingIndex < editBranches.arraySize)
            {
                SerializedProperty editEntry = editBranches.GetArrayElementAtIndex(editingIndex);
                SerializedProperty editSlots = editEntry.FindPropertyRelative("slots");
                string editLabel = (editingLeft ? "L" : "R") + (editingIndex + 1);
                EditorGUILayout.Space(6f);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Edit " + editLabel + " (drag to reorder)", EditorStyles.boldLabel);
                if (GUILayout.Button("Close", GUILayout.Width(50f)))
                { editingIndex = -1; slotEditList = null; }
                EditorGUILayout.EndHorizontal();
                EnsureSlotEditList(editSlots);
                if (slotEditList != null)
                    slotEditList.DoList(EditorGUILayout.GetControlRect(false, slotEditList.GetHeight()));
                EditorGUILayout.EndVertical();
            }
            else
                editingIndex = -1;
        }

        ValidateAndWarn(data, leftBranches, rightBranches, kindMask.intValue, slotsPerBranch.intValue);
        EditorGUILayout.Space(4f);
        if (GUILayout.Button("Randomize"))
            DoRandomizeOnly(data);
        if (GUILayout.Button("Compact (non-empty to left per branch)"))
        {
            int sc = Mathf.Clamp(slotsPerBranch.intValue, 1, MaxSlots);
            CompactAllBranches(leftBranches, sc);
            CompactAllBranches(rightBranches, sc);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawBranchRow(Rect rect, SerializedProperty data, SerializedProperty entry, int entryIndex, string label, bool isLeft)
    {
        float rowPad = 6f;
        rect.x += rowPad;
        rect.width -= rowPad * 2f;
        if (rect.width < LabelWidth + ButtonAreaWidth + 20f) return;

        SerializedProperty slots = entry.FindPropertyRelative("slots");
        int slotCount = currentSlotCount;
        bool isEditing = editingLeft == isLeft && editingIndex == entryIndex;

        bool allEmpty = true;
        if (slots != null && slots.arraySize >= slotCount)
        {
            for (int s = 0; s < slotCount && allEmpty; s++)
                if (slots.GetArrayElementAtIndex(s).intValue != EmptyIndexInEditor) allEmpty = false;
        }
        Color prev = GUI.backgroundColor;
        GUI.backgroundColor = allEmpty ? KindColors[EmptyIndexInEditor] : FilledBg;
        GUI.Box(rect, GUIContent.none);
        GUI.backgroundColor = prev;

        float lineH = EditorGUIUtility.singleLineHeight;
        float centerY = rect.y + (rect.height - lineH) * 0.5f;
        float buttonW = 52f;
        float chipAreaW = rect.width - LabelWidth - ButtonAreaWidth;
        float buttonX = rect.x + rect.width - ButtonAreaWidth + (ButtonAreaWidth - buttonW) * 0.5f;

        EditorGUI.LabelField(new Rect(rect.x, centerY - 1f, LabelWidth, lineH), label, EditorStyles.miniBoldLabel);
        if (chipAreaW > 0f)
            DrawSlotChips(new Rect(rect.x + LabelWidth, centerY - ChipHeight * 0.5f, chipAreaW, ChipHeight), slots, slotCount, isLeft);
        if (GUI.Button(new Rect(buttonX, centerY - 2f, buttonW, lineH + 4f), isEditing ? "Close" : "Edit"))
        {
            if (isEditing) { editingIndex = -1; slotEditList = null; }
            else { editingLeft = isLeft; editingIndex = entryIndex; slotEditList = null; }
        }
    }

    private void DrawSlotChips(Rect area, SerializedProperty slots, int slotCount, bool isLeft)
    {
        if (slots == null || slotCount <= 0 || area.width <= 0) return;
        float totalNeeded = slotCount * ChipWidth + (slotCount - 1) * ChipGap;
        float chipW = ChipWidth;
        float gap = ChipGap;
        if (totalNeeded > area.width)
        {
            chipW = Mathf.Max(20f, (area.width - (slotCount - 1) * 2f) / slotCount);
            gap = 2f;
        }
        float totalW = slotCount * chipW + (slotCount - 1) * gap;
        float x = area.x + Mathf.Max(0, (area.width - totalW) * 0.5f);
        float y = area.y + (area.height - ChipHeight) * 0.5f;
        for (int s = 0; s < slotCount; s++)
        {
            int dataIndex = isLeft ? s : (slotCount - 1 - s);
            if (dataIndex < 0 || dataIndex >= slots.arraySize) continue;
            int k = slots.GetArrayElementAtIndex(dataIndex).intValue;
            int kindIdx = Mathf.Clamp(k, 0, KindColors.Length - 1);
            Rect box = new Rect(x + s * (chipW + gap), y, chipW, ChipHeight);
            EditorGUI.DrawRect(box, KindColors[kindIdx]);
            EditorGUI.DrawRect(new Rect(box.x, box.y, box.width, 1f), ChipBorder);
            EditorGUI.DrawRect(new Rect(box.x, box.yMax - 1f, box.width, 1f), ChipBorder);
            EditorGUI.DrawRect(new Rect(box.x, box.y, 1f, box.height), ChipBorder);
            EditorGUI.DrawRect(new Rect(box.xMax - 1f, box.y, 1f, box.height), ChipBorder);
            Color textColor = Luminance(KindColors[kindIdx]) < 0.45f ? Color.white : new Color(0.15f, 0.15f, 0.18f, 1f);
            var style = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter, normal = { textColor = textColor }, fontSize = chipW < 36 ? 8 : 10 };
            EditorGUI.LabelField(box, GetKindName(kindIdx), style);
        }
    }

    private static float Luminance(Color c)
    {
        return 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
    }

    private void EnsureSlotEditList(SerializedProperty slots)
    {
        if (slotEditList == null)
        {
            slotEditList = new ReorderableList(serializedObject, slots, true, false, false, false)
            {
                elementHeight = SlotRowHeight,
                drawHeaderCallback = r => EditorGUI.LabelField(r, "Slot order (drag to reorder)"),
                drawElementCallback = (rect, index, active, focused) =>
                {
                    if (index >= slots.arraySize) return;
                    var slot = slots.GetArrayElementAtIndex(index);
                    int currentVal = slot.intValue;
                    int kindIdx = Mathf.Clamp(currentVal, 0, KindColors.Length - 1);
                    float cardW = 96f;
                    Rect cardRect = new Rect(rect.x + 2f, rect.y + 2f, cardW, rect.height - 4f);
                    EditorGUI.DrawRect(cardRect, KindColors[kindIdx]);
                    EditorGUI.DrawRect(new Rect(cardRect.x, cardRect.y, cardRect.width, 1f), ChipBorder);
                    EditorGUI.DrawRect(new Rect(cardRect.x, cardRect.yMax - 1f, cardRect.width, 1f), ChipBorder);
                    EditorGUI.DrawRect(new Rect(cardRect.x, cardRect.y, 1f, cardRect.height), ChipBorder);
                    EditorGUI.DrawRect(new Rect(cardRect.xMax - 1f, cardRect.y, 1f, cardRect.height), ChipBorder);
                    string cardLabel = GetKindName(kindIdx);
                    Color textColor = Luminance(KindColors[kindIdx]) < 0.45f ? Color.white : new Color(0.15f, 0.15f, 0.18f, 1f);
                    var cardStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter, normal = { textColor = textColor } };
                    EditorGUI.LabelField(cardRect, cardLabel, cardStyle);
                    Rect popupRect = new Rect(rect.x + cardW + 10f, rect.y + 2f, rect.width - cardW - 14f, rect.height - 4f);
                    CountKindsInLevel(serializedObject.FindProperty("data"), currentSlotCount, _kindCounts);
                    int cap = _kindCounts.Length;
                    for (int i = 0; i < cap; i++) _countWithoutSlot[i] = _kindCounts[i];
                    if (currentVal >= 0 && currentVal < cap) _countWithoutSlot[currentVal]--;
                    var dataProp = serializedObject.FindProperty("data");
                    int mask = dataProp != null ? dataProp.FindPropertyRelative("kindMask").intValue : 0;
                    int maskBitsPopup = (1 << KindCountNoEmpty) - 1;
                    mask &= maskBitsPopup;
                    int maxPerKind = dataProp != null ? currentSlotCount * GetKindMultiplier(dataProp, currentVal) : currentSlotCount;
                    var allowedKinds = GetKindsFromMask(mask);
                    _optionKinds.Clear();
                    foreach (int ki in allowedKinds)
                    {
                        int maxKi = dataProp != null ? currentSlotCount * GetKindMultiplier(dataProp, ki) : currentSlotCount;
                        if (ki < cap && (_countWithoutSlot[ki] < maxKi || ki == currentVal)) _optionKinds.Add(ki);
                    }
                    _optionKinds.Add(EmptyIndexInEditor);
                    if (_optionKinds.Count == 0) _optionKinds.Add(currentVal);
                    int selected = 0;
                    for (int k = 0; k < _optionKinds.Count; k++)
                        if (_optionKinds[k] == currentVal) { selected = k; break; }
                    var popupOpts = new string[_optionKinds.Count];
                    for (int k = 0; k < _optionKinds.Count; k++) popupOpts[k] = GetKindName(_optionKinds[k]);
                    int newSel = EditorGUI.Popup(popupRect, selected, popupOpts);
                    if (newSel >= 0 && newSel < _optionKinds.Count) slot.intValue = _optionKinds[newSel];
                }
            };
        }
    }

    private static string GetKindsMaskSummary(int mask, int maskBits)
    {
        if (mask == 0) return "Nothing";
        if (mask == maskBits) return "Everything";
        var parts = new List<string>();
        for (int i = 0; i < KindCountNoEmpty; i++)
            if ((mask & (1 << i)) != 0) parts.Add(KindNamesForMask[i]);
        return parts.Count > 0 ? string.Join(", ", parts) : "Nothing";
    }

    private int CountKinds(int mask)
    {
        int n = 0;
        for (int i = 0; i < KindCountNoEmpty; i++)
            if ((mask & (1 << i)) != 0) n++;
        return n;
    }

    private int[] GetKindsFromMask(int mask)
    {
        var indices = NonEmptyKindIndices;
        var list = new List<int>();
        for (int i = 0; i < indices.Length; i++)
            if ((mask & (1 << i)) != 0) list.Add(indices[i]);
        return list.ToArray();
    }

    private void CountKindsInLevel(SerializedProperty data, int slotsPerBranch, int[] counts)
    {
        for (int i = 0; i < counts.Length; i++) counts[i] = 0;
        var leftBranches = data.FindPropertyRelative("leftBranches");
        var rightBranches = data.FindPropertyRelative("rightBranches");
        void CountList(SerializedProperty branches)
        {
            for (int i = 0; i < branches.arraySize; i++)
            {
                var slots = branches.GetArrayElementAtIndex(i).FindPropertyRelative("slots");
                for (int s = 0; s < slotsPerBranch && s < slots.arraySize; s++)
                {
                    int k = slots.GetArrayElementAtIndex(s).intValue;
                    if (k >= 0 && k < counts.Length) counts[k]++;
                }
            }
        }
        CountList(leftBranches);
        CountList(rightBranches);
    }

    private void ValidateAndWarn(SerializedProperty data, SerializedProperty leftBranches, SerializedProperty rightBranches, int kindMask, int slotsPerBranch)
    {
        int kindCount = CountKinds(kindMask);
        if (kindCount == 0) return;
        int cap = Mathf.Max(KindCountNoEmpty + 1, 16);
        var counts = new int[cap];
        void CountEntry(SerializedProperty entry)
        {
            var slots = entry.FindPropertyRelative("slots");
            for (int s = 0; s < slotsPerBranch && s < slots.arraySize; s++)
            {
                int k = slots.GetArrayElementAtIndex(s).intValue;
                if (k >= 0 && k < counts.Length) counts[k]++;
            }
        }
        for (int i = 0; i < leftBranches.arraySize; i++) CountEntry(leftBranches.GetArrayElementAtIndex(i));
        for (int i = 0; i < rightBranches.arraySize; i++) CountEntry(rightBranches.GetArrayElementAtIndex(i));
        var indices = NonEmptyKindIndices;
        for (int i = 0; i < indices.Length; i++)
        {
            if ((kindMask & (1 << i)) == 0) continue;
            int kindIdx = indices[i];
            int expected = slotsPerBranch * GetKindMultiplier(data, kindIdx);
            if (kindIdx < counts.Length && counts[kindIdx] != expected)
                EditorGUILayout.HelpBox("Kind \"" + GetKindName(kindIdx) + "\" should be " + expected + ", got " + counts[kindIdx], MessageType.Warning);
        }
    }

    private void MoveBetween(SerializedProperty fromList, SerializedProperty toList, int fromIndex)
    {
        if (fromIndex < 0 || fromIndex >= fromList.arraySize) return;
        var entry = GetEntry(fromList.GetArrayElementAtIndex(fromIndex));
        fromList.DeleteArrayElementAtIndex(fromIndex);
        toList.arraySize++;
        SetEntry(toList.GetArrayElementAtIndex(toList.arraySize - 1), entry);
    }

    private void EnsureSlotsSize(SerializedProperty branches, int slotCount)
    {
        int n = Mathf.Clamp(slotCount, 1, MaxSlots);
        for (int i = 0; i < branches.arraySize; i++)
        {
            var slots = branches.GetArrayElementAtIndex(i).FindPropertyRelative("slots");
            if (slots.arraySize != n) slots.arraySize = n;
        }
    }

    private void InitEntry(SerializedProperty entry)
    {
        int n = Mathf.Clamp(currentSlotCount, 1, MaxSlots);
        var slots = entry.FindPropertyRelative("slots");
        slots.arraySize = n;
        int emptyIdx = EmptyIndexInEditor;
        for (int s = 0; s < slots.arraySize; s++)
            slots.GetArrayElementAtIndex(s).intValue = emptyIdx;
    }

    private BranchEntry GetEntry(SerializedProperty entryProp)
    {
        var e = new BranchEntry();
        var slots = entryProp.FindPropertyRelative("slots");
        e.slots = new int[slots.arraySize];
        for (int i = 0; i < slots.arraySize; i++)
            e.slots[i] = slots.GetArrayElementAtIndex(i).intValue;
        return e;
    }

    private void SetEntry(SerializedProperty entryProp, BranchEntry e)
    {
        var slots = entryProp.FindPropertyRelative("slots");
        if (slots.arraySize < e.slots.Length) slots.arraySize = e.slots.Length;
        for (int i = 0; i < e.slots.Length; i++)
            slots.GetArrayElementAtIndex(i).intValue = e.slots[i];
    }

    private static void Shuffle<T>(IList<T> list)
    {
        var r = new System.Random();
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = r.Next(i + 1);
            var t = list[i]; list[i] = list[j]; list[j] = t;
        }
    }

    private void DoRandomizeOnly(SerializedProperty data)
    {
        var slotsPerBranch = data.FindPropertyRelative("slotsPerBranch");
        var kindMask = data.FindPropertyRelative("kindMask");
        var leftBranches = data.FindPropertyRelative("leftBranches");
        var rightBranches = data.FindPropertyRelative("rightBranches");
        int slotCount = Mathf.Clamp(slotsPerBranch.intValue, 1, MaxSlots);
        var kinds = new List<int>(GetKindsFromMask(kindMask.intValue));
        if (kinds.Count == 0) { EditorUtility.DisplayDialog("Randomize", "Select at least 1 kind.", "OK"); return; }

        int filledCount = 0;
        foreach (int k in kinds) filledCount += slotCount * GetKindMultiplier(data, k);

        int emptyIdx = EmptyIndexInEditor;
        int totalBranches = leftBranches.arraySize + rightBranches.arraySize;
        int totalSlots = totalBranches * slotCount;
        int emptySlotCount = totalSlots - filledCount;
        if (emptySlotCount < 0) { EditorUtility.DisplayDialog("Randomize", "Not enough slots for selected kinds. Add branches or reduce branches per kind.", "OK"); return; }

        var pile = new List<int>();
        foreach (int k in kinds) for (int i = 0; i < slotCount * GetKindMultiplier(data, k); i++) pile.Add(k);
        for (int i = 0; i < emptySlotCount; i++) pile.Add(emptyIdx);
        Shuffle(pile);

        int idx = 0;
        void FillBranches(SerializedProperty branches)
        {
            for (int i = 0; i < branches.arraySize; i++)
            {
                var slots = branches.GetArrayElementAtIndex(i).FindPropertyRelative("slots");
                for (int s = 0; s < slotCount && s < slots.arraySize; s++)
                    slots.GetArrayElementAtIndex(s).intValue = idx < pile.Count ? pile[idx++] : emptyIdx;
            }
        }
        FillBranches(leftBranches);
        FillBranches(rightBranches);
        CompactAllBranches(leftBranches, slotCount);
        CompactAllBranches(rightBranches, slotCount);
    }

    private void CompactBranchSlots(SerializedProperty entry, int slotCount)
    {
        var slots = entry.FindPropertyRelative("slots");
        if (slots.arraySize < slotCount) return;
        int emptyIdx = EmptyIndexInEditor;
        var nonEmpty = new List<int>();
        int emptyCount = 0;
        for (int s = 0; s < slotCount; s++)
        {
            int k = slots.GetArrayElementAtIndex(s).intValue;
            if (k == emptyIdx) emptyCount++; else nonEmpty.Add(k);
        }
        int i = 0;
        foreach (int k in nonEmpty) slots.GetArrayElementAtIndex(i++).intValue = k;
        for (int j = 0; j < emptyCount; j++) slots.GetArrayElementAtIndex(i++).intValue = emptyIdx;
    }

    private void CompactAllBranches(SerializedProperty branches, int slotCount)
    {
        for (int i = 0; i < branches.arraySize; i++)
            CompactBranchSlots(branches.GetArrayElementAtIndex(i), slotCount);
    }
}
