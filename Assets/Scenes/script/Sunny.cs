using UnityEngine;

public enum SunnyKind
{
    Jamur,
    Tomat,
    Matahari
}

public enum SunnyType
{
    Red,
    Blue,
    Yellow,
    Purple,
    Green
}

[RequireComponent(typeof(SunnyMovement))]
public class Sunny : MonoBehaviour
{
    [Header("Sunny Identity")]
    public SunnyKind kind;          // 🔥 INI UNTUK SORT & PECAH
    public SunnyType sunnyType;     // 🎨 WARNA (OPSIONAL)

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
    // MOVE API
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

    private void OnMouseDown()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager TIDAK ADA");
            return;
        }

        GameManager.Instance.SelectSunny(this);
    }
}
