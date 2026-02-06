using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    private Sunny selectedSunny;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
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

        Debug.Log("Moving Sunny " + selectedSunny.name + " to branch " + branch.name);

        Branch oldBranch = selectedSunny.GetCurrentBranch();
        if (oldBranch != null)
            oldBranch.RemoveSunny(selectedSunny);

        Vector3 target = branch.GetNextSlotPosition();
        Debug.Log("Target position: " + target);

        SunnyMovement move = selectedSunny.GetComponent<SunnyMovement>();
        if (move == null)
        {
            Debug.LogError("SunnyMovement not found on " + selectedSunny.name);
            return;
        }

        move.MoveTo(target, () =>
        {
            branch.AddSunny(selectedSunny);
            Debug.Log("Sunny moved!");
        });

        selectedSunny = null;
    }

    // =========================
    // MOVE SUNNY TO SPECIFIC SLOT (Optional)
    // =========================
    public void MoveSelectedSunnyToSpecificSlot(Branch branch, int slotIndex)
    {
        if (selectedSunny == null) return;
        if (!branch.IsSlotEmpty(slotIndex)) return;

        Branch old = selectedSunny.GetCurrentBranch();
        if (old != null)
            old.RemoveSunny(selectedSunny);

        Vector3 target = branch.GetSlotPosition(slotIndex);

        SunnyMovement move = selectedSunny.GetComponent<SunnyMovement>();
        move.MoveTo(target, () =>
        {
            branch.AddSunnyAtSlot(selectedSunny, slotIndex);
        });

        selectedSunny = null;
    }

    // =========================
    // FLASK (AMAN, TIDAK DIUBAH)
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
