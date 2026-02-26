using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class Branch : MonoBehaviour
{
    [Header("Stand Points")]
    public Transform[] standPoints = new Transform[3];

    [Header("Click Priority")]
    public LayerMask sunnyLayer;

    private Sunny[] slots;
    private bool isBreaking = false;

    private void Awake()
    {
        slots = new Sunny[standPoints.Length];
        GetComponent<BoxCollider>().isTrigger = false;
    }

    // 🟢 INIT SLOT SAAT GAME MULAI
    private void Start()
    {
        InitSlotsFromScene();
    }

    void InitSlotsFromScene()
{
    Sunny[] allSunny = FindObjectsOfType<Sunny>();

    for (int i = 0; i < standPoints.Length; i++)
    {
        Transform point = standPoints[i];
        if (point == null) continue;

        foreach (Sunny sunny in allSunny)
        {
            if (sunny == null) continue;

            float distance = Vector3.Distance(sunny.transform.position, point.position);

            if (distance < 0.2f) // toleransi jarak
            {
                slots[i] = sunny;
                sunny.SetCurrentBranch(this);
                sunny.transform.position = point.position;

                Debug.Log($"INIT SLOT → {sunny.name} di {name} slot {i}");
            }
        }
    }
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
        if (isBreaking) return;
        if (sunny == null) return;
        if (index < 0 || index >= slots.Length) return;

        if (slots[index] != null)
        {
            slots[index].SetCurrentBranch(null);
            slots[index] = null;
        }

        slots[index] = sunny;
        sunny.SetCurrentBranch(this);
        sunny.transform.position = standPoints[index].position;

        Debug.Log($"Sunny masuk {name} → Slot {index}");

        CheckBreakCondition();
    }

    public void RemoveSunny(Sunny sunny)
    {
        if (isBreaking) return;

        for (int i = 0; i < slots.Length; i++)
            if (slots[i] == sunny)
                slots[i] = null;
    }

    // =========================
    // BREAK LOGIC
    // =========================
    void CheckBreakCondition()
    {
        if (isBreaking) return;

        foreach (Sunny s in slots)
            if (s == null) return;

        SunnyKind kind = slots[0].kind;

        foreach (Sunny s in slots)
            if (s.kind != kind)
                return;

        BreakBranch();
    }

    void BreakBranch()
    {
        if (isBreaking) return;

        isBreaking = true;
        Debug.Log("BRANCH PECAH: " + name);

        GetComponent<Collider>().enabled = false;
        StartCoroutine(BreakAnimation());
    }

    IEnumerator BreakAnimation()
    {
        foreach (Sunny s in slots)
        {
            if (s == null) continue;

            if (!s.TryGetComponent<Rigidbody>(out _))
            {
                Rigidbody rb = s.gameObject.AddComponent<Rigidbody>();
                rb.useGravity = true;
            }
        }

        Rigidbody branchRb = gameObject.AddComponent<Rigidbody>();
        branchRb.useGravity = true;

        yield return new WaitForSeconds(2f);

        foreach (Sunny s in slots)
            if (s != null)
                Destroy(s.gameObject);

        Destroy(gameObject);
    }

    // =========================
    // AUTO SLOT
    // =========================
    public bool HasSpace()
    {
        if (isBreaking) return false;

        foreach (Sunny s in slots)
            if (s == null) return true;
        return false;
    }

    public Vector3 GetNextSlotPosition()
    {
        for (int i = 0; i < slots.Length; i++)
            if (slots[i] == null)
                return standPoints[i].position;

        return transform.position;
    }

    public void AddSunny(Sunny sunny)
    {
        if (isBreaking) return;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
            {
                AddSunnyAtSlot(sunny, i);
                return;
            }
        }
    }
    public int GetSlotIndex(Sunny sunny)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == sunny)
                return i;
    }
    return -1;
}

    // =========================
    // RESET BRANCH (TAMBAHAN)
    // =========================
    public void ResetBranch()
    {
        isBreaking = false;

        GetComponent<Collider>().enabled = true;

        for (int i = 0; i < slots.Length; i++)
        {
            slots[i] = null;
        }
    }
}
