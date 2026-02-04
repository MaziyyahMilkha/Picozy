using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class BranchPoint : MonoBehaviour
{
    public enum BranchDirection
    {
        Left,
        Right,
        Up,
        Down,
        Any
    }

    [Header("Branch Settings")]
    public BranchDirection branchDirection = BranchDirection.Any;
    public bool isActive = true;

    [Header("Stand Point")]
    public Transform standPoint;

    [Header("Visual Effect")]
    public bool enableBounce = true;
    public float bounceAmount = 0.15f;
    public float bounceSpeed = 12f;

    [Header("Debug")]
    public bool showDebug = true;

    private Vector3 originalPosition;
    private float bounceTimer;

    private void Awake()
    {
        originalPosition = transform.position;

        // Auto create StandPoint if missing
        if (standPoint == null)
        {
            GameObject point = new GameObject("StandPoint");
            point.transform.SetParent(transform);
            point.transform.localPosition = Vector3.zero;
            standPoint = point.transform;
        }

        // Pastikan collider adalah trigger
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void Update()
    {
        if (!enableBounce) return;
        if (bounceTimer <= 0) return;

        bounceTimer -= Time.deltaTime;

        float offset = Mathf.Sin(Time.time * bounceSpeed) * bounceAmount;
        transform.position = originalPosition + Vector3.up * offset;

        if (bounceTimer <= 0)
        {
            transform.position = originalPosition;
        }
    }

    // =========================
    // INTERACTION
    // =========================

    public bool CanBeUsed()
    {
        return isActive;
    }

    public bool IsDirectionAllowed(Vector2 moveDirection)
    {
        switch (branchDirection)
        {
            case BranchDirection.Left:
                return moveDirection.x < 0;
            case BranchDirection.Right:
                return moveDirection.x > 0;
            case BranchDirection.Up:
                return moveDirection.y > 0;
            case BranchDirection.Down:
                return moveDirection.y < 0;
            case BranchDirection.Any:
                return true;
        }
        return false;
    }

    public void OnSunnyLanded()
    {
        if (!isActive) return;

        if (enableBounce)
            bounceTimer = 0.2f;

        if (showDebug)
            Debug.Log("Sunny mendarat di dahan: " + name);
    }

    // =========================
    // TRIGGER
    // =========================

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isActive) return;

        SunnyMovement sunny = collision.GetComponent<SunnyMovement>();
        if (sunny != null)
        {
            OnSunnyLanded();
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!showDebug) return;

        Gizmos.color = isActive ? Color.green : Color.red;
        Gizmos.DrawWireCube(transform.position, GetComponent<BoxCollider2D>().size);

        if (standPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(standPoint.position, 0.1f);
        }
    }
#endif
}
