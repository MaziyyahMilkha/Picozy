using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SortDahan : MonoBehaviour
{
    [Header("Slots")]
    [SerializeField] private Transform[] standPoints;

    private SortKarakter[] slots;
    private bool isBroken;

    private void Awake()
    {
        if (standPoints == null || standPoints.Length == 0)
        {
            Debug.LogWarning("SortDahan: standPoints kosong, pakai default 3.");
            standPoints = new Transform[3];
        }
        slots = new SortKarakter[standPoints.Length];
    }

    public bool CanAccept(SortKarakter karakter)
    {
        if (karakter == null || isBroken) return false;

        SortKind? existingKind = null;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
            {
                if (!existingKind.HasValue)
                    existingKind = slots[i].Kind;
                else if (slots[i].Kind != existingKind.Value)
                    return false;
            }
        }

        if (!existingKind.HasValue) return true;
        return karakter.Kind == existingKind.Value;
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

    /// <summary>Transform slot (standPoint) untuk parent karakter. Null jika index invalid.</summary>
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

    public void AddKarakter(SortKarakter karakter)
    {
        if (isBroken || karakter == null) return;
        int idx = GetNextSlotIndex();
        if (idx < 0) return;

        slots[idx] = karakter;
        karakter.SetDahan(this);
        karakter.transform.position = GetSlotPosition(idx);
        CheckTerkumpul();
    }

    public void AddKarakterAtSlot(SortKarakter karakter, int index)
    {
        if (isBroken || karakter == null || index < 0 || index >= slots.Length) return;
        if (slots[index] != null) return;

        slots[index] = karakter;
        karakter.SetDahan(this);
        karakter.transform.position = GetSlotPosition(index);
        CheckTerkumpul();
    }

    public void RemoveKarakter(SortKarakter karakter)
    {
        if (isBroken) return;
        for (int i = 0; i < slots.Length; i++)
            if (slots[i] == karakter)
            {
                slots[i] = null;
                return;
            }
    }

    private void CheckTerkumpul()
    {
        if (isBroken) return;
        for (int i = 0; i < slots.Length; i++)
            if (slots[i] == null) return;

        SortKind kind = slots[0].Kind;
        for (int i = 1; i < slots.Length; i++)
            if (slots[i].Kind != kind) return;

        StartCoroutine(TerkumpulRoutine());
    }

    private IEnumerator TerkumpulRoutine()
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
        Destroy(gameObject);
    }

    public int GetSlotIndex(SortKarakter karakter)
    {
        for (int i = 0; i < slots.Length; i++)
            if (slots[i] == karakter) return i;
        return -1;
    }
}
