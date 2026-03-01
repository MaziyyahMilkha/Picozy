using System;
using System.Collections.Generic;
using UnityEngine;

public class SortGameplayManager : MonoBehaviour
{
    public static SortGameplayManager Instance { get; private set; }
    public static event Action OnLevelComplete;

    private static readonly List<int> _tempSlots = new List<int>(8);
    private static readonly List<int> _tempDestSlots = new List<int>(8);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public bool CanMove(SortDahan source, SortDahan dest, SortKind kind, int count)
    {
        if (source == null || dest == null || source == dest || count <= 0) return false;
        if (dest.GetEmptySlotCount() < count) return false;
        SortKind? topDest = dest.GetTopKind();
        if (topDest.HasValue && topDest.Value != kind) return false;
        return true;
    }

    public void DoMove(SortDahan source, SortDahan dest, int count, Action onComplete = null)
    {
        if (source == null || dest == null || count <= 0) { onComplete?.Invoke(); return; }

        source.GetTopGroup(out _, out int actualCount, _tempSlots);
        if (actualCount == 0 || _tempSlots.Count == 0) { onComplete?.Invoke(); return; }

        int moveCount = Mathf.Min(count, actualCount, _tempSlots.Count);
        dest.GetNextEmptySlotIndicesForAdd(moveCount, _tempDestSlots);
        if (_tempDestSlots.Count < moveCount) { onComplete?.Invoke(); return; }

        var moving = new List<SortKarakter>(moveCount);
        for (int i = 0; i < moveCount; i++)
        {
            var c = source.RemoveCharacterAtSlot(_tempSlots[i]);
            if (c != null)
            {
                c.transform.SetParent(dest.transform, true);
                moving.Add(c);
            }
        }

        if (moving.Count == 0) { onComplete?.Invoke(); return; }

        int moveCountFinal = moving.Count;
        int arrived = 0;
        for (int i = 0; i < moving.Count && i < _tempDestSlots.Count; i++)
        {
            int slotIndex = _tempDestSlots[i];
            Vector3 targetPos = dest.GetSlotPosition(slotIndex);
            SortKarakter c = moving[i];
            c.MoveTo(targetPos, () =>
            {
                dest.AddCharacterAtSlot(c, slotIndex);
                arrived++;
                if (arrived >= moveCountFinal)
                {
                    dest.CompactSlots();
                    onComplete?.Invoke();
                    CheckLevelComplete();
                }
            });
        }
    }

    public void CheckLevelComplete()
    {
        if (FindObjectsOfType<SortKarakter>().Length == 0)
            OnLevelComplete?.Invoke();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
