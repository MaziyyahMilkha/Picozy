using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SunnyMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float arriveDistance = 0.05f;
    [SerializeField] private bool rotateTowardsTarget = true;
    [SerializeField] private float rotateSpeed = 10f;

    [Header("Debug")]
    [SerializeField] private bool showDebug = true;

    private Rigidbody rb;
    private bool isMoving;
    private Vector3 currentTarget;

    // =========================
    // PUBLIC STATE
    // =========================
    public bool IsMoving => isMoving;
    public Vector3 CurrentTarget => currentTarget;

    // =========================
    // EVENTS (OPSIONAL)
    // =========================
    public event Action OnMoveStart;
    public event Action OnMoveComplete;

    // =========================
    // UNITY
    // =========================
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Pastikan setting Rigidbody aman
        rb.useGravity = false;
        rb.isKinematic = true;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    // =========================
    // PUBLIC API (DIPANGGIL BRANCH)
    // =========================
    public void MoveTo(Vector3 target, Action onComplete = null)
    {
        if (isMoving)
        {
            if (showDebug)
                Debug.Log("Sunny sedang bergerak, perintah diabaikan");
            return;
        }

        currentTarget = target;
        StartCoroutine(MoveRoutine(onComplete));
    }

    // =========================
    // CORE MOVEMENT
    // =========================
    private IEnumerator MoveRoutine(Action onComplete)
    {
        isMoving = true;
        OnMoveStart?.Invoke();

        if (showDebug)
            Debug.Log("Sunny mulai bergerak ke: " + currentTarget);

        while (Vector3.Distance(transform.position, currentTarget) > arriveDistance)
        {
            // Gerak posisi
            Vector3 nextPos = Vector3.MoveTowards(
                transform.position,
                currentTarget,
                moveSpeed * Time.deltaTime
            );

            transform.position = nextPos;

            // Rotasi ke arah target
            if (rotateTowardsTarget)
                RotateTowards(currentTarget);

            yield return null;
        }

        // Snap posisi akhir
        transform.position = currentTarget;

        isMoving = false;
        OnMoveComplete?.Invoke();
        onComplete?.Invoke();

        if (showDebug)
            Debug.Log("Sunny tiba di target");
    }

    // =========================
    // ROTATION
    // =========================
    private void RotateTowards(Vector3 target)
    {
        Vector3 direction = (target - transform.position);
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotateSpeed * Time.deltaTime
        );
    }

    // =========================
    // FORCE STOP (OPSIONAL)
    // =========================
    public void StopMovement()
    {
        StopAllCoroutines();
        isMoving = false;

        if (showDebug)
            Debug.Log("Sunny movement dihentikan paksa");
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!showDebug) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(currentTarget, 0.08f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, currentTarget);
    }
#endif
}