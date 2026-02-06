using UnityEngine;

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
    [Header("Sunny Data")]
    public SunnyType sunnyType;

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float stoppingDistance = 0.5f;

    // =========================
    // INTERNAL STATE
    // =========================
    private FlaskController currentFlask;
    private SunnyMovement movement;

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
    public FlaskController GetFlask()
    {
        return currentFlask;
    }

    public void SetFlask(FlaskController flask)
    {
        currentFlask = flask;
    }

    public bool HasFlask()
    {
        return currentFlask != null;
    }

    // =========================
// BRANCH API (WAJIB UNTUK SORTING)
// =========================
private Branch currentBranch;

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
    // MOVE API (INI YANG DICARI ERROR)
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
