using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

[CustomEditor(typeof(SortLevelAsset))]
public class SortLevelAssetEditor : Editor
{
    private const float SlotColorStripWidth = 8f;
    private const int MaxSlots = 8;
    private static readonly string[] KindNames = System.Enum.GetNames(typeof(SortKind));
    private const int KindCountNoKosong = 5;
    private static readonly string[] KindNamesForMask = new string[] { "Tomat", "Jamur", "BawangMerah", "Daisy", "Kelapa" };
    private static readonly Color FilledBg = new Color(0.95f, 0.98f, 1f, 0.6f);
    private const float DahanRowHeight = 36f;
    private const float SlotRowHeight = 38f;
    private const float PaddingRight = 28f;
    private const float PaddingInner = 10f;
    private const float ChipWidth = 52f;
    private const float ChipHeight = 26f;
    private const float ChipGap = 5f;
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
    private SerializedProperty currentSlotPerDahan;
    private int currentKindMask;
    private readonly int[] _kindCounts = new int[16];
    private readonly int[] _countWithoutSlot = new int[16];
    private readonly System.Collections.Generic.List<SortKind> _optionKinds = new System.Collections.Generic.List<SortKind>(16);
    private int _cachedKindMask = -1;
    private SortKind[] _cachedAllowedKinds;
    private int currentSlotCount => currentSlotPerDahan != null ? Mathf.Clamp(currentSlotPerDahan.intValue, 1, MaxSlots) : 4;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        SerializedProperty data = serializedObject.FindProperty("data");
        if (data == null) { serializedObject.ApplyModifiedProperties(); return; }

        SerializedProperty slotPerDahan = data.FindPropertyRelative("slotPerDahan");
        SerializedProperty kindMask = data.FindPropertyRelative("kindMask");
        SerializedProperty randomEachPlay = data.FindPropertyRelative("randomEachPlay");
        SerializedProperty leftDahans = data.FindPropertyRelative("leftDahans");
        SerializedProperty rightDahans = data.FindPropertyRelative("rightDahans");

        EditorGUILayout.Space(4f);
        slotPerDahan.intValue = Mathf.Clamp(EditorGUILayout.IntField("Slot per dahan", slotPerDahan.intValue), 1, MaxSlots);
        kindMask.intValue = EditorGUILayout.MaskField("Kind yang dipakai", kindMask.intValue & 0x1F, KindNamesForMask) & 0x1F;
        randomEachPlay.boolValue = EditorGUILayout.Toggle("Random tiap play", randomEachPlay.boolValue);

        int kindCount = CountKinds(kindMask.intValue);
        int slotsPer = Mathf.Clamp(slotPerDahan.intValue, 1, MaxSlots);
        if (kindCount > 0)
            EditorGUILayout.HelpBox("Tiap kind tepat " + slotsPer + " (sesuai slot/dahan). Sisanya Kosong.", MessageType.Info);

        EditorGUILayout.Space(4f);

        currentSlotPerDahan = slotPerDahan;
        currentKindMask = kindMask.intValue;
        if (_cachedKindMask != currentKindMask) { _cachedKindMask = currentKindMask; _cachedAllowedKinds = GetKindsFromMask(currentKindMask); }
        EnsureSlotsSize(leftDahans, slotPerDahan.intValue);
        EnsureSlotsSize(rightDahans, slotPerDahan.intValue);

        string leftPath = leftDahans.propertyPath;
        string rightPath = rightDahans.propertyPath;
        if (leftList == null || leftList.serializedProperty.propertyPath != leftPath)
        {
            leftList = new ReorderableList(serializedObject, leftDahans, true, true, true, true)
            {
                elementHeight = DahanRowHeight,
                drawHeaderCallback = r => EditorGUI.LabelField(r, "Kiri (1,2,3,4)"),
                drawElementCallback = (rect, index, active, focused) => DrawDahanRow(rect, data, leftDahans.GetArrayElementAtIndex(index), index, "L" + (index + 1), true)
            };
            leftList.onAddCallback = l => { leftDahans.arraySize++; InitEntry(leftDahans.GetArrayElementAtIndex(leftDahans.arraySize - 1)); };
        }
        if (rightList == null || rightList.serializedProperty.propertyPath != rightPath)
        {
            rightList = new ReorderableList(serializedObject, rightDahans, true, true, true, true)
            {
                elementHeight = DahanRowHeight,
                drawHeaderCallback = r => EditorGUI.LabelField(r, "Kanan (4,3,2,1)"),
                drawElementCallback = (rect, index, active, focused) => DrawDahanRow(rect, data, rightDahans.GetArrayElementAtIndex(index), index, "R" + (index + 1), false)
            };
            rightList.onAddCallback = l => { rightDahans.arraySize++; InitEntry(rightDahans.GetArrayElementAtIndex(rightDahans.arraySize - 1)); };
        }

        float halfW = (EditorGUIUtility.currentViewWidth - 80f - PaddingRight * 2f) * 0.5f;
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(4f);
        EditorGUILayout.BeginVertical(GUILayout.Width(halfW));
        leftList.DoList(EditorGUILayout.GetControlRect(false, leftList.GetHeight()));
        EditorGUILayout.EndVertical();
        EditorGUILayout.BeginVertical(GUILayout.Width(80f));
        GUILayout.Space(22f);
        if (leftList.index >= 0 && leftList.index < leftDahans.arraySize && GUILayout.Button("→\nKanan"))
            MoveBetween(leftDahans, rightDahans, leftList.index);
        if (rightList.index >= 0 && rightList.index < rightDahans.arraySize && GUILayout.Button("←\nKiri"))
            MoveBetween(rightDahans, leftDahans, rightList.index);
        EditorGUILayout.EndVertical();
        EditorGUILayout.BeginVertical(GUILayout.Width(halfW));
        rightList.DoList(EditorGUILayout.GetControlRect(false, rightList.GetHeight()));
        EditorGUILayout.EndVertical();
        GUILayout.Space(PaddingRight);
        EditorGUILayout.EndHorizontal();

        if (editingIndex >= 0)
        {
            SerializedProperty editDahans = editingLeft ? leftDahans : rightDahans;
            if (editingIndex < editDahans.arraySize)
            {
                SerializedProperty editEntry = editDahans.GetArrayElementAtIndex(editingIndex);
                SerializedProperty editSlots = editEntry.FindPropertyRelative("slots");
                string editLabel = (editingLeft ? "L" : "R") + (editingIndex + 1);
                EditorGUILayout.Space(6f);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Edit dahan " + editLabel + " — drag untuk ubah urutan", EditorStyles.boldLabel);
                if (GUILayout.Button("Tutup", GUILayout.Width(50f)))
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

        ValidateAndWarn(leftDahans, rightDahans, kindMask.intValue, slotPerDahan.intValue);
        EditorGUILayout.Space(4f);
        if (GUILayout.Button("Randomize (" + Mathf.Clamp(slotPerDahan.intValue, 1, MaxSlots) + " per kind, sisanya Kosong)"))
            RandomizeLevel(data);
        if (GUILayout.Button("Urutkan (non-Kosong ke kiri per dahan)"))
        {
            int sc = Mathf.Clamp(slotPerDahan.intValue, 1, MaxSlots);
            CompactAllDahans(leftDahans, sc);
            CompactAllDahans(rightDahans, sc);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawDahanRow(Rect rect, SerializedProperty data, SerializedProperty entry, int entryIndex, string label, bool isLeft)
    {
        SerializedProperty slots = entry.FindPropertyRelative("slots");
        int slotCount = currentSlotCount;
        bool isEditing = editingLeft == isLeft && editingIndex == entryIndex;

        bool allKosong = true;
        if (slots != null && slots.arraySize >= slotCount)
        {
            for (int s = 0; s < slotCount && allKosong; s++)
                if (slots.GetArrayElementAtIndex(s).enumValueIndex != (int)SortKind.Kosong) allKosong = false;
        }
        Color prev = GUI.backgroundColor;
        GUI.backgroundColor = allKosong ? KindColors[(int)SortKind.Kosong] : FilledBg;
        GUI.Box(rect, GUIContent.none);
        GUI.backgroundColor = prev;

        rect.x += PaddingInner; rect.width -= PaddingInner * 2f;
        float lineH = EditorGUIUtility.singleLineHeight;
        float centerY = rect.y + (rect.height - lineH) * 0.5f;

        EditorGUI.LabelField(new Rect(rect.x, centerY - 1f, 30f, lineH), label, EditorStyles.miniBoldLabel);
        float chipStartX = rect.x + 32f;
        DrawSlotChips(new Rect(chipStartX, centerY - ChipHeight * 0.5f, rect.width - 32f - 58f, ChipHeight), slots, slotCount, isLeft);
        if (GUI.Button(new Rect(rect.xMax - 56f, centerY - 2f, 56f, lineH + 4f), isEditing ? "Tutup" : "Edit"))
        {
            if (isEditing) { editingIndex = -1; slotEditList = null; }
            else { editingLeft = isLeft; editingIndex = entryIndex; slotEditList = null; }
        }
    }

    private void DrawSlotChips(Rect area, SerializedProperty slots, int slotCount, bool isLeft)
    {
        if (slots == null || slotCount <= 0) return;
        float totalW = slotCount * ChipWidth + (slotCount - 1) * ChipGap;
        float x = area.x + Mathf.Max(0, (area.width - totalW) * 0.5f);
        float y = area.y + (area.height - ChipHeight) * 0.5f;
        for (int s = 0; s < slotCount; s++)
        {
            int dataIndex = isLeft ? s : (slotCount - 1 - s);
            if (dataIndex < 0 || dataIndex >= slots.arraySize) continue;
            int k = slots.GetArrayElementAtIndex(dataIndex).enumValueIndex;
            int kindIdx = Mathf.Clamp(k, 0, KindColors.Length - 1);
            Rect box = new Rect(x + s * (ChipWidth + ChipGap), y, ChipWidth, ChipHeight);
            EditorGUI.DrawRect(box, KindColors[kindIdx]);
            EditorGUI.DrawRect(new Rect(box.x, box.y, box.width, 1f), ChipBorder);
            EditorGUI.DrawRect(new Rect(box.x, box.yMax - 1f, box.width, 1f), ChipBorder);
            EditorGUI.DrawRect(new Rect(box.x, box.y, 1f, box.height), ChipBorder);
            EditorGUI.DrawRect(new Rect(box.xMax - 1f, box.y, 1f, box.height), ChipBorder);
            Color textColor = Luminance(KindColors[kindIdx]) < 0.45f ? Color.white : new Color(0.15f, 0.15f, 0.18f, 1f);
            var style = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter, normal = { textColor = textColor } };
            EditorGUI.LabelField(box, KindNames[kindIdx], style);
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
                drawHeaderCallback = r => EditorGUI.LabelField(r, "Urutan slot — drag baris untuk pindah"),
                drawElementCallback = (rect, index, active, focused) =>
                {
                    if (index >= slots.arraySize) return;
                    var slot = slots.GetArrayElementAtIndex(index);
                    int currentVal = slot.enumValueIndex;
                    int kindIdx = Mathf.Clamp(currentVal, 0, KindColors.Length - 1);
                    float cardW = 96f;
                    Rect cardRect = new Rect(rect.x + 2f, rect.y + 2f, cardW, rect.height - 4f);
                    EditorGUI.DrawRect(cardRect, KindColors[kindIdx]);
                    EditorGUI.DrawRect(new Rect(cardRect.x, cardRect.y, cardRect.width, 1f), ChipBorder);
                    EditorGUI.DrawRect(new Rect(cardRect.x, cardRect.yMax - 1f, cardRect.width, 1f), ChipBorder);
                    EditorGUI.DrawRect(new Rect(cardRect.x, cardRect.y, 1f, cardRect.height), ChipBorder);
                    EditorGUI.DrawRect(new Rect(cardRect.xMax - 1f, cardRect.y, 1f, cardRect.height), ChipBorder);
                    string cardLabel = KindNames[kindIdx];
                    Color textColor = Luminance(KindColors[kindIdx]) < 0.45f ? Color.white : new Color(0.15f, 0.15f, 0.18f, 1f);
                    var cardStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter, normal = { textColor = textColor } };
                    EditorGUI.LabelField(cardRect, cardLabel, cardStyle);
                    Rect popupRect = new Rect(rect.x + cardW + 10f, rect.y + 2f, rect.width - cardW - 14f, rect.height - 4f);
                    CountKindsInLevel(serializedObject.FindProperty("data"), currentSlotCount, _kindCounts);
                    int cap = _kindCounts.Length;
                    for (int i = 0; i < cap; i++) _countWithoutSlot[i] = _kindCounts[i];
                    if (currentVal >= 0 && currentVal < cap) _countWithoutSlot[currentVal]--;
                    var allowedKinds = _cachedAllowedKinds != null ? _cachedAllowedKinds : GetKindsFromMask(currentKindMask);
                    _optionKinds.Clear();
                    foreach (var k in allowedKinds)
                    {
                        int ki = (int)k;
                        if (ki < cap && (_countWithoutSlot[ki] < currentSlotCount || ki == currentVal)) _optionKinds.Add(k);
                    }
                    _optionKinds.Add(SortKind.Kosong);
                    if (_optionKinds.Count == 0) _optionKinds.Add((SortKind)currentVal);
                    int selected = 0;
                    for (int k = 0; k < _optionKinds.Count; k++)
                        if ((int)_optionKinds[k] == currentVal) { selected = k; break; }
                    var popupOpts = new string[_optionKinds.Count];
                    for (int k = 0; k < _optionKinds.Count; k++) popupOpts[k] = KindNames[(int)_optionKinds[k]];
                    int newSel = EditorGUI.Popup(popupRect, selected, popupOpts);
                    if (newSel >= 0 && newSel < _optionKinds.Count) slot.enumValueIndex = (int)_optionKinds[newSel];
                }
            };
        }
    }

    private int CountKinds(int mask)
    {
        int n = 0;
        for (int i = 0; i < KindCountNoKosong; i++)
            if ((mask & (1 << i)) != 0) n++;
        return n;
    }

    private SortKind[] GetKindsFromMask(int mask)
    {
        var list = new System.Collections.Generic.List<SortKind>();
        for (int i = 0; i < KindCountNoKosong; i++)
            if ((mask & (1 << i)) != 0) list.Add((SortKind)i);
        return list.ToArray();
    }

    private void CountKindsInLevel(SerializedProperty data, int slotPerDahan, int[] counts)
    {
        for (int i = 0; i < counts.Length; i++) counts[i] = 0;
        var leftDahans = data.FindPropertyRelative("leftDahans");
        var rightDahans = data.FindPropertyRelative("rightDahans");
        void CountList(SerializedProperty dahans)
        {
            for (int i = 0; i < dahans.arraySize; i++)
            {
                var slots = dahans.GetArrayElementAtIndex(i).FindPropertyRelative("slots");
                for (int s = 0; s < slotPerDahan && s < slots.arraySize; s++)
                {
                    int k = slots.GetArrayElementAtIndex(s).enumValueIndex;
                    if (k >= 0 && k < counts.Length) counts[k]++;
                }
            }
        }
        CountList(leftDahans);
        CountList(rightDahans);
    }

    private void ValidateAndWarn(SerializedProperty leftDahans, SerializedProperty rightDahans, int kindMask, int slotPerDahan)
    {
        int kindCount = CountKinds(kindMask);
        if (kindCount == 0) return;
        var counts = new int[KindNames.Length];
        void CountEntry(SerializedProperty entry)
        {
            var slots = entry.FindPropertyRelative("slots");
            for (int s = 0; s < slotPerDahan && s < slots.arraySize; s++)
            {
                int k = slots.GetArrayElementAtIndex(s).enumValueIndex;
                if (k >= 0 && k < counts.Length) counts[k]++;
            }
        }
        for (int i = 0; i < leftDahans.arraySize; i++) CountEntry(leftDahans.GetArrayElementAtIndex(i));
        for (int i = 0; i < rightDahans.arraySize; i++) CountEntry(rightDahans.GetArrayElementAtIndex(i));
        for (int i = 0; i < KindCountNoKosong; i++)
        {
            if ((kindMask & (1 << i)) == 0) continue;
            if (counts[i] != slotPerDahan)
                EditorGUILayout.HelpBox("Kind \"" + KindNames[i] + "\" harus tepat " + slotPerDahan + ", sekarang: " + counts[i], MessageType.Warning);
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

    private void EnsureSlotsSize(SerializedProperty dahans, int slotCount)
    {
        int n = Mathf.Clamp(slotCount, 1, MaxSlots);
        for (int i = 0; i < dahans.arraySize; i++)
        {
            var slots = dahans.GetArrayElementAtIndex(i).FindPropertyRelative("slots");
            if (slots.arraySize != n) slots.arraySize = n;
        }
    }

    private void InitEntry(SerializedProperty entry)
    {
        int n = Mathf.Clamp(currentSlotCount, 1, MaxSlots);
        var slots = entry.FindPropertyRelative("slots");
        slots.arraySize = n;
        for (int s = 0; s < slots.arraySize; s++)
            slots.GetArrayElementAtIndex(s).enumValueIndex = (int)SortKind.Kosong;
    }

    private DahanEntry GetEntry(SerializedProperty entryProp)
    {
        var e = new DahanEntry();
        var slots = entryProp.FindPropertyRelative("slots");
        e.slots = new SortKind[slots.arraySize];
        for (int i = 0; i < slots.arraySize; i++)
            e.slots[i] = (SortKind)slots.GetArrayElementAtIndex(i).enumValueIndex;
        return e;
    }

    private void SetEntry(SerializedProperty entryProp, DahanEntry e)
    {
        var slots = entryProp.FindPropertyRelative("slots");
        if (slots.arraySize < e.slots.Length) slots.arraySize = e.slots.Length;
        for (int i = 0; i < e.slots.Length; i++)
            slots.GetArrayElementAtIndex(i).enumValueIndex = (int)e.slots[i];
    }

    private static void Shuffle<T>(System.Collections.Generic.IList<T> list)
    {
        var r = new System.Random();
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = r.Next(i + 1);
            var t = list[i]; list[i] = list[j]; list[j] = t;
        }
    }

    private void RandomizeLevel(SerializedProperty data)
    {
        var slotPerDahan = data.FindPropertyRelative("slotPerDahan");
        var kindMask = data.FindPropertyRelative("kindMask");
        var leftDahans = data.FindPropertyRelative("leftDahans");
        var rightDahans = data.FindPropertyRelative("rightDahans");
        int slotCount = Mathf.Clamp(slotPerDahan.intValue, 1, MaxSlots);
        var kinds = new System.Collections.Generic.List<SortKind>(GetKindsFromMask(kindMask.intValue));
        if (kinds.Count == 0) { EditorUtility.DisplayDialog("Randomize", "Pilih minimal 1 kind.", "OK"); return; }

        var pile = new System.Collections.Generic.List<SortKind>();
        foreach (var k in kinds) for (int i = 0; i < slotCount; i++) pile.Add(k);
        for (int i = 0; i < slotCount; i++) pile.Add(SortKind.Kosong);
        Shuffle(pile);

        int idx = 0;
        void FillDahans(SerializedProperty dahans)
        {
            for (int i = 0; i < dahans.arraySize; i++)
            {
                var slots = dahans.GetArrayElementAtIndex(i).FindPropertyRelative("slots");
                for (int s = 0; s < slotCount && s < slots.arraySize; s++)
                    slots.GetArrayElementAtIndex(s).enumValueIndex = idx < pile.Count ? (int)pile[idx++] : (int)SortKind.Kosong;
            }
        }
        FillDahans(leftDahans);
        FillDahans(rightDahans);
        CompactAllDahans(leftDahans, slotCount);
        CompactAllDahans(rightDahans, slotCount);
    }

    private void CompactDahanSlots(SerializedProperty entry, int slotCount)
    {
        var slots = entry.FindPropertyRelative("slots");
        if (slots.arraySize < slotCount) return;
        var nonKosong = new System.Collections.Generic.List<int>();
        int kosongCount = 0;
        for (int s = 0; s < slotCount; s++)
        {
            int k = slots.GetArrayElementAtIndex(s).enumValueIndex;
            if (k == (int)SortKind.Kosong) kosongCount++; else nonKosong.Add(k);
        }
        int i = 0;
        foreach (int k in nonKosong) slots.GetArrayElementAtIndex(i++).enumValueIndex = k;
        for (int j = 0; j < kosongCount; j++) slots.GetArrayElementAtIndex(i++).enumValueIndex = (int)SortKind.Kosong;
    }

    private void CompactAllDahans(SerializedProperty dahans, int slotCount)
    {
        for (int i = 0; i < dahans.arraySize; i++)
            CompactDahanSlots(dahans.GetArrayElementAtIndex(i), slotCount);
    }
}
