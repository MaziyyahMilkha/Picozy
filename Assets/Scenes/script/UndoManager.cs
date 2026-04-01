using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class UndoData
{
    public Vector3 position;
    public Branch branch;
    public int slotIndex;
    public Sunny sunny;
}

public class UndoManager : MonoBehaviour
{
    public static UndoManager Instance;

    [SerializeField] private float undoDuration = 0.35f;

    private Stack<UndoData> undoStack = new Stack<UndoData>();
    private Coroutine undoCoroutine;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    // =========================
    // SIMPAN DATA (PANGGIL SEBELUM PINDAH)
    // =========================
    public void SaveMove(Sunny sunny)
    {
        if (sunny == null) return;

        UndoData data = new UndoData();
        data.sunny = sunny;
        data.position = sunny.transform.position;

        Branch currentBranch = sunny.GetCurrentBranch();
        data.branch = currentBranch;

        if (currentBranch != null)
            data.slotIndex = currentBranch.GetSlotIndex(sunny);
        else
            data.slotIndex = -1;

        undoStack.Push(data);

        Debug.Log("UNDO SAVE: " + sunny.name);
    }

    // =========================
    // UNDO GERAKAN TERAKHIR (SMOOTH)
    // =========================
    public void UndoMove()
    {
        if (undoStack.Count == 0) return;

        if (undoCoroutine != null)
            StopCoroutine(undoCoroutine);

        UndoData data = undoStack.Pop();
        if (data.sunny == null) return;

        undoCoroutine = StartCoroutine(UndoMoveRoutine(data));
    }

    IEnumerator UndoMoveRoutine(UndoData data)
    {
        Sunny sunny = data.sunny;

        // keluar dari branch sekarang
        Branch currentBranch = sunny.GetCurrentBranch();
        if (currentBranch != null)
            currentBranch.RemoveSunny(sunny);

        sunny.SetCurrentBranch(null);

        Vector3 startPos = sunny.transform.position;
        Vector3 targetPos;

        if (data.branch != null && data.slotIndex != -1)
            targetPos = data.branch.GetSlotPosition(data.slotIndex);
        else
            targetPos = data.position;

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / undoDuration;
            sunny.transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        sunny.transform.position = targetPos;

        // set branch & slot SETELAH sampai
        if (data.branch != null && data.slotIndex != -1)
        {
            data.branch.AddSunnyAtSlot(sunny, data.slotIndex);
        }

        Debug.Log("UNDO MOVE SMOOTH: " + sunny.name);
    }

    // =========================
    // RESET HISTORY
    // =========================
    public void ClearHistory()
    {
        undoStack.Clear();
    }
}
