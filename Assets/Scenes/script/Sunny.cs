using UnityEngine;

public enum SunnyKind
{
    Jamur,
    Tomat,
    Matahari,
    Daisy,
    Kelapa
}

public enum SunnyType
{
    Red,
    Blue,
    Yellow,
    Purple,
    Brown
}

[RequireComponent(typeof(SunnyMovement))]
public class Sunny : MonoBehaviour
{
    [Header("Sunny Identity")]
    public SunnyKind kind;
    public SunnyType sunnyType;

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float stoppingDistance = 0.5f;

    // =========================
    // INTERNAL STATE
    // =========================
    private FlaskController currentFlask;
    private SunnyMovement movement;
    private Branch currentBranch;

    // =========================
    // RESET DATA
    // =========================
    private Vector3 startPosition;
    private Branch startBranch;

    // =========================
    // UNDO GLOBAL DATA (PINDAH KE SINI)
    // =========================
    private Branch undoBranch;
    private Vector3 undoPosition;
    private bool hasUndoData = false;

    // =========================
    // UNITY
    // =========================
    private void Awake()
    {
        movement = GetComponent<SunnyMovement>();

        if (movement == null)
        {
            Debug.LogError("SunnyMovement TIDAK ditemukan di " + gameObject.name);
        }
        else
        {
            movement.SetData(moveSpeed, stoppingDistance);
        }

        startPosition = transform.position;
        startBranch = GetComponentInParent<Branch>();
    }

    // =========================
    // FLASK API
    // =========================
    public FlaskController GetFlask() => currentFlask;
    public void SetFlask(FlaskController flask) => currentFlask = flask;
    public bool HasFlask() => currentFlask != null;

    // =========================
    // BRANCH API
    // =========================
    public void SetCurrentBranch(Branch branch)
    {
        currentBranch = branch;
    }

    public Branch GetCurrentBranch()
    {
        return currentBranch;
    }

    public bool HasBranch()
    {
        return currentBranch != null;
    }

    // =========================
    // MOVE API (DENGAN UNDO)
    // =========================
    public void MoveToBranch(Branch targetBranch)
    {
        if (targetBranch == null) return;

        // SIMPAN UNDO
        SaveUndoState();

        if (currentBranch != null)
            currentBranch.RemoveSunny(this);

        targetBranch.AddSunny(this);
    }

    // =========================
    // MOVEMENT API
    // =========================
    public void MoveTo(Vector3 targetPosition)
    {
        if (movement == null) return;
        movement.MoveTo(targetPosition);
    }

    public bool IsMoving()
    {
        return movement != null && movement.IsMoving;
    }

    // =========================
    // RESET API
    // =========================
    public void ResetToStart()
    {
        transform.position = startPosition;

        if (startBranch != null)
        {
            startBranch.AddSunny(this);
        }
    }

    // =========================
    // SAVE UNDO
    // =========================
    public void SaveUndoState()
    {
        undoBranch = currentBranch;
        undoPosition = transform.position;
        hasUndoData = true;
    }

    // =========================
    // EXECUTE UNDO
    // =========================
    public void UndoMove()
    {
        if (!hasUndoData) return;

        if (currentBranch != null)
            currentBranch.RemoveSunny(this);

        if (undoBranch != null)
            undoBranch.AddSunny(this);

        transform.position = undoPosition;
        currentBranch = undoBranch;

        hasUndoData = false;
    }

    private void OnDisable()
{
    if (GameManager.Instance != null)
        GameManager.Instance.CheckLevelComplete();
}

}
