using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    private Sunny selectedSunny;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("GameManager aktif: " + gameObject.name);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // =========================
    // INPUT HANDLER (FIX UTAMA)
    // =========================
    private void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray);

        if (hits.Length == 0) return;

        // 🔑 SORT: yang PALING DEPAN diproses dulu
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            // 🟢 PRIORITAS 1 — SUNNY
            Sunny sunny = hit.collider.GetComponent<Sunny>();
            if (sunny != null)
            {
                SelectSunny(sunny);
                return;
            }

            // 🟡 PRIORITAS 2 — SLOT POINT
            SlotPoint slot = hit.collider.GetComponent<SlotPoint>();
            if (slot != null)
            {
                Branch b = slot.GetComponentInParent<Branch>();
                if (b != null)
                    MoveSelectedSunnyToSpecificSlot(b, slot.slotIndex);
                return;
            }

            // 🔵 PRIORITAS 3 — BRANCH
            Branch branch = hit.collider.GetComponent<Branch>();
            if (branch != null)
            {
                MoveSelectedSunnyToBranch(branch);
                return;
            }
        }
    }

    // =========================
    // SELECT SUNNY
    // =========================
    public void SelectSunny(Sunny sunny)
    {
        if (sunny == null) return;

        selectedSunny = sunny;
        Debug.Log("Selected Sunny: " + sunny.name);
    }

    // =========================
    // MOVE SUNNY TO BRANCH
    // =========================
    public void MoveSelectedSunnyToBranch(Branch branch)
    {
        if (selectedSunny == null) return;
        if (!branch.HasSpace())
        {
            Debug.Log("Branch penuh!");
            return;
        }

        Sunny movingSunny = selectedSunny;
        selectedSunny = null;

        Branch oldBranch = movingSunny.GetCurrentBranch();
        if (oldBranch != null)
            oldBranch.RemoveSunny(movingSunny);

        Vector3 target = branch.GetNextSlotPosition();

        SunnyMovement move = movingSunny.GetComponent<SunnyMovement>();
        if (move == null) return;

        move.MoveTo(target, () =>
        {
            branch.AddSunny(movingSunny);
        });
    }

    // =========================
    // MOVE SUNNY TO SPECIFIC SLOT
    // =========================
    public void MoveSelectedSunnyToSpecificSlot(Branch branch, int slotIndex)
    {
        if (selectedSunny == null) return;
        if (!branch.IsSlotEmpty(slotIndex)) return;

        Sunny movingSunny = selectedSunny;
        selectedSunny = null;

        Branch old = movingSunny.GetCurrentBranch();
        if (old != null)
            old.RemoveSunny(movingSunny);

        Vector3 target = branch.GetSlotPosition(slotIndex);

        SunnyMovement move = movingSunny.GetComponent<SunnyMovement>();
        if (move == null) return;

        move.MoveTo(target, () =>
        {
            branch.AddSunnyAtSlot(movingSunny, slotIndex);
        });
    }

    // =========================
    // FLASK (TIDAK DIUBAH)
    // =========================
    public void SelectFlask(FlaskController flask)
    {
        if (selectedSunny == null) return;
        if (!flask.CanAddSunny(selectedSunny)) return;

        FlaskController oldFlask = selectedSunny.GetFlask();
        if (oldFlask != null)
            oldFlask.RemoveTopSunny();

        flask.AddSunny(selectedSunny);
        selectedSunny = null;
    }
}
