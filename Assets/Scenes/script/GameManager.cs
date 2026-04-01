using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [Header("Undo Settings")]
    [SerializeField] private float undoDelay = 0.25f;

    [Header("Level Settings")]
    [SerializeField] private string nextSceneName;



    private bool levelCompleted = false;

    public static GameManager Instance;

    private Sunny selectedSunny;

    private List<Sunny> undoSunnys = new List<Sunny>();
    private bool isUndoing = false;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Update()
    {
        if (isUndoing) return;
        if (!Input.GetMouseButtonDown(0)) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 100f);

        if (hits.Length == 0) return;

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (selectedSunny != null)
            {
                SlotPoint slot = hit.collider.GetComponent<SlotPoint>();
                if (slot != null)
                {
                    Branch branch = slot.GetComponentInParent<Branch>();
                    if (branch != null)
                        MoveSelectedSunnyToSpecificSlot(branch, slot.slotIndex);
                    return;
                }

                Branch b = hit.collider.GetComponent<Branch>();
                if (b != null)
                {
                    MoveSelectedSunnyToBranch(b);
                    return;
                }
            }

            Sunny sunny = hit.collider.GetComponent<Sunny>();
            if (sunny != null)
            {
                SelectSunny(sunny);
                return;
            }
        }
    }

    public void SelectSunny(Sunny sunny)
    {
        if (sunny == null) return;
        if (sunny.IsMoving()) return;

        selectedSunny = sunny;
    }

    public void MoveSelectedSunnyToBranch(Branch branch)
    {
        if (selectedSunny == null) return;
        if (selectedSunny.IsMoving()) return;
        if (!branch.HasSpace()) return;
        if (selectedSunny.GetCurrentBranch() == branch) return;

        Sunny movingSunny = selectedSunny;
        selectedSunny = null;

        movingSunny.SaveUndoState();
        RegisterUndo(movingSunny);

        Branch oldBranch = movingSunny.GetCurrentBranch();
        if (oldBranch != null)
            oldBranch.RemoveSunny(movingSunny);

        Vector3 targetPos = branch.GetNextSlotPosition();
        SunnyMovement movement = movingSunny.GetComponent<SunnyMovement>();
        if (movement == null) return;

        movement.MoveTo(targetPos, () =>
        {
            branch.AddSunny(movingSunny);
        });
    }

    public void MoveSelectedSunnyToSpecificSlot(Branch branch, int slotIndex)
    {
        if (selectedSunny == null) return;
        if (selectedSunny.IsMoving()) return;
        if (!branch.IsSlotEmpty(slotIndex)) return;
        if (selectedSunny.GetCurrentBranch() == branch) return;

        Sunny movingSunny = selectedSunny;
        selectedSunny = null;

        movingSunny.SaveUndoState();
        RegisterUndo(movingSunny);

        Branch oldBranch = movingSunny.GetCurrentBranch();
        if (oldBranch != null)
            oldBranch.RemoveSunny(movingSunny);

        Vector3 targetPos = branch.GetSlotPosition(slotIndex);
        SunnyMovement movement = movingSunny.GetComponent<SunnyMovement>();
        if (movement == null) return;

        movement.MoveTo(targetPos, () =>
        {
            branch.AddSunnyAtSlot(movingSunny, slotIndex);
        });
    }

    public void SelectFlask(FlaskController flask)
    {
        if (selectedSunny == null) return;
        if (selectedSunny.IsMoving()) return;
        if (!flask.CanAddSunny(selectedSunny)) return;

        selectedSunny.SaveUndoState();
        RegisterUndo(selectedSunny);

        FlaskController oldFlask = selectedSunny.GetFlask();
        if (oldFlask != null)
            oldFlask.RemoveTopSunny();

        flask.AddSunny(selectedSunny);
        selectedSunny = null;
    }

    public void RegisterUndo(Sunny sunny)
    {
        if (!undoSunnys.Contains(sunny))
            undoSunnys.Add(sunny);
    }

    public void UndoAllSunny()
    {
        if (isUndoing) return;
        StartCoroutine(UndoCoroutine());
    }

    private IEnumerator UndoCoroutine()
    {
        isUndoing = true;

        for (int i = undoSunnys.Count - 1; i >= 0; i--)
        {
            Sunny sunny = undoSunnys[i];
            if (sunny != null)
                sunny.UndoMove();

            yield return new WaitForSeconds(undoDelay);
        }

        undoSunnys.Clear();
        isUndoing = false;
    }

    // =========================
    // LEVEL COMPLETE SYSTEM
    // =========================
    public void CheckLevelComplete()
    {
        if (levelCompleted) return;

        Sunny[] allSunny = FindObjectsOfType<Sunny>();
        int activeCount = 0;

        foreach (Sunny s in allSunny)
        {
            if (s.gameObject.activeInHierarchy)
                activeCount++;
        }

        if (activeCount == 0)
        {
            LevelComplete();
        }
    }

    private void LevelComplete()
{
    levelCompleted = true;

    Debug.Log("LEVEL COMPLETE!");

    GameTimer timer = FindObjectOfType<GameTimer>();
    if (timer != null)
        timer.StopTimer();

    GameResultManager result = FindObjectOfType<GameResultManager>();
    if (result != null)
        result.Win();
}



public void UndoLastMove()
{
    if (isUndoing) return;
    if (undoSunnys.Count == 0) return;

    Sunny lastSunny = undoSunnys[undoSunnys.Count - 1];
    undoSunnys.RemoveAt(undoSunnys.Count - 1);

    if (lastSunny != null)
        lastSunny.UndoMove();
}






    // Dipanggil dari tombol NEXT
    public void NextLevel()
    {
        SceneManager.LoadScene(nextSceneName);
    }

    // =========================
    // RESET LEVEL
    // =========================
    public void ResetLevel()
{
    levelCompleted = false;
    selectedSunny = null;

    Branch[] branches = FindObjectsOfType<Branch>();
    foreach (Branch branch in branches)
        branch.ResetBranch();

    Sunny[] sunnies = FindObjectsOfType<Sunny>();
    foreach (Sunny sunny in sunnies)
        sunny.ResetToStart();

    undoSunnys.Clear();

    GameTimer timer = FindObjectOfType<GameTimer>();
    if (timer != null)
        timer.ResetTimer();

    Debug.Log("LEVEL RESET");
}

}
