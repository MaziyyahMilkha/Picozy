using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SortDahan : MonoBehaviour
{
    [Header("Slots")]
    [SerializeField] private Transform[] standPoints;

    private SortKarakter[] slots;
    private bool isBroken;
    private bool topIsHighIndex = true;

    private void Awake()
    {
        if (standPoints == null || standPoints.Length == 0)
            standPoints = new Transform[3];
        slots = new SortKarakter[standPoints.Length];
    }

    public bool CanAccept(SortKarakter character)
    {
        if (character == null || isBroken) return false;
        SortKind? existingKind = null;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;
            if (!existingKind.HasValue)
                existingKind = slots[i].Kind;
            else if (slots[i].Kind != existingKind.Value)
                return false;
        }
        if (!existingKind.HasValue) return true;
        return character.Kind == existingKind.Value;
    }

    public bool HasSpace()
    {
        if (isBroken) return false;
        for (int i = 0; i < slots.Length; i++)
            if (slots[i] == null) return true;
        return false;
    }

    public bool IsSlotEmpty(int index)
    {
        if (index < 0 || index >= slots.Length) return false;
        return slots[index] == null;
    }

    public Vector3 GetSlotPosition(int index)
    {
        if (index < 0 || index >= standPoints.Length) return transform.position;
        return standPoints[index].position;
    }

    public Transform GetSlotTransform(int index)
    {
        if (standPoints == null || index < 0 || index >= standPoints.Length) return null;
        return standPoints[index];
    }

    public Vector3 GetNextSlotPosition()
    {
        for (int i = 0; i < slots.Length; i++)
            if (slots[i] == null)
                return GetSlotPosition(i);
        return transform.position;
    }

    public int GetNextSlotIndex()
    {
        for (int i = 0; i < slots.Length; i++)
            if (slots[i] == null) return i;
        return -1;
    }

    public void AddCharacter(SortKarakter character)
    {
        if (isBroken || character == null) return;
        int idx = GetNextSlotIndex();
        if (idx < 0) return;
        slots[idx] = character;
        character.SetDahan(this);
        Transform slotParent = GetSlotTransform(idx) ?? transform;
        character.transform.SetParent(slotParent, false);
        character.transform.localPosition = Vector3.zero;
        CheckAllMatched();
    }

    public void AddCharacterAtSlot(SortKarakter character, int index)
    {
        if (isBroken || character == null || index < 0 || index >= slots.Length) return;
        if (slots[index] != null) return;
        slots[index] = character;
        character.SetDahan(this);
        Transform slotParent = GetSlotTransform(index) ?? transform;
        character.transform.SetParent(slotParent, false);
        character.transform.localPosition = Vector3.zero;
        CheckAllMatched();
    }

    public void RemoveCharacter(SortKarakter character)
    {
        if (isBroken) return;
        for (int i = 0; i < slots.Length; i++)
            if (slots[i] == character) { slots[i] = null; return; }
    }

    private void CheckAllMatched()
    {
        if (isBroken) return;
        for (int i = 0; i < slots.Length; i++)
            if (slots[i] == null) return;
        SortKind kind = slots[0].Kind;
        for (int i = 1; i < slots.Length; i++)
            if (slots[i].Kind != kind) return;
        StartCoroutine(CollectedRoutine());
    }

    private IEnumerator CollectedRoutine()
    {
        isBroken = true;
        yield return new WaitForSeconds(0.2f);
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
            {
                Destroy(slots[i].gameObject);
                slots[i] = null;
            }
        }
        isBroken = false;
        if (SortGameplayManager.Instance != null)
            SortGameplayManager.Instance.CheckLevelComplete();
    }

    public int GetSlotIndex(SortKarakter karakter)
    {
        for (int i = 0; i < slots.Length; i++)
            if (slots[i] == karakter) return i;
        return -1;
    }

    public SortKarakter GetCharacterAtSlot(int index)
    {
        if (index < 0 || index >= slots.Length) return null;
        return slots[index];
    }

    public void SetTopIsHighIndex(bool high) { topIsHighIndex = high; }

    public void GetTopGroup(out SortKind? kind, out int count, List<int> outSlotIndices)
    {
        kind = null;
        count = 0;
        outSlotIndices?.Clear();
        if (slots == null || isBroken) return;

        int step = topIsHighIndex ? -1 : 1;
        int start = topIsHighIndex ? slots.Length - 1 : 0;
        int i = start;
        SortKind? firstKind = null;
        while (i >= 0 && i < slots.Length)
        {
            if (slots[i] == null) { i += step; continue; }
            if (!firstKind.HasValue) firstKind = slots[i].Kind;
            if (slots[i].Kind != firstKind.Value) break;
            count++;
            outSlotIndices?.Add(i);
            i += step;
        }
        kind = firstKind;
    }

    public int GetEmptySlotCount()
    {
        if (slots == null) return 0;
        int n = 0;
        for (int i = 0; i < slots.Length; i++)
            if (slots[i] == null) n++;
        return n;
    }

    public SortKind? GetTopKind()
    {
        if (slots == null) return null;
        int start = topIsHighIndex ? slots.Length - 1 : 0;
        int step = topIsHighIndex ? -1 : 1;
        for (int i = start; i >= 0 && i < slots.Length; i += step)
            if (slots[i] != null) return slots[i].Kind;
        return null;
    }

    public void GetNextEmptySlotIndicesForAdd(int count, List<int> outIndices)
    {
        outIndices?.Clear();
        if (slots == null || count <= 0) return;
        for (int i = 0; i < slots.Length && outIndices.Count < count; i++)
            if (slots[i] == null) outIndices.Add(i);
    }

    public SortKarakter RemoveCharacterAtSlot(int index)
    {
        if (index < 0 || index >= slots.Length) return null;
        var k = slots[index];
        slots[index] = null;
        return k;
    }

    public void CompactSlots()
    {
        if (slots == null || isBroken) return;
        var filled = new List<SortKarakter>(slots.Length);
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null) filled.Add(slots[i]);
            slots[i] = null;
        }
        for (int i = 0; i < filled.Count && i < slots.Length; i++)
        {
            slots[i] = filled[i];
            Transform slotParent = GetSlotTransform(i) ?? transform;
            filled[i].transform.SetParent(slotParent, false);
            filled[i].transform.localPosition = Vector3.zero;
        }
    }
}
