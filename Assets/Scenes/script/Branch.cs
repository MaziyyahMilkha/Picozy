using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class Branch : MonoBehaviour
{
    public enum BranchDirection { Left, Right, Up, Down, Any }

    [Header("Stand Points")]
    public Transform[] standPoints = new Transform[3];

    private Sunny[] slots;

    private void Awake()
    {
        slots = new Sunny[standPoints.Length];
        GetComponent<BoxCollider>().isTrigger = true;
    }

    // =========================
    // SLOT API
    // =========================

    public bool IsSlotEmpty(int index)
    {
        if (index < 0 || index >= slots.Length) return false;
        return slots[index] == null;
    }

    public Vector3 GetSlotPosition(int index)
    {
        return standPoints[index].position;
    }

    public void AddSunnyAtSlot(Sunny sunny, int index)
    {
        if (!IsSlotEmpty(index)) return;

        slots[index] = sunny;
        sunny.SetCurrentBranch(this);
        sunny.transform.position = standPoints[index].position;

        Debug.Log($"Sunny masuk {name} → Slot_{index + 1}");
    }

    public void RemoveSunny(Sunny sunny)
    {
        for (int i = 0; i < slots.Length; i++)
            if (slots[i] == sunny)
                slots[i] = null;
    }

    // =========================
    // FUNGSI TAMBAHAN UNTUK GAME MANAGER
    // =========================

    // Mengecek apakah masih ada slot kosong
    public bool HasSpace()
    {
        foreach (var s in slots)
            if (s == null) return true;
        return false;
    }

    // Ambil posisi slot kosong pertama
    public Vector3 GetNextSlotPosition()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
                return standPoints[i].position;
        }
        // Jika tidak ada slot kosong, kembalikan posisi branch sendiri
        return transform.position;
    }

    // Tambahkan Sunny ke slot kosong pertama
    public void AddSunny(Sunny sunny)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
            {
                AddSunnyAtSlot(sunny, i);
                return;
            }
        }

        Debug.LogWarning("Branch penuh, tidak bisa menambahkan Sunny!");
    }
}
