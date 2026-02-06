using System;
using System.Collections;
using UnityEngine;

public class SunnyMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float arriveDistance = 0.001f;
    [SerializeField] private bool rotateTowardsTarget = false;
    [SerializeField] private float rotateSpeed = 10f;

    [Header("Debug")]
    [SerializeField] private bool showDebug = true;


    private bool isMoving;
    private Vector3 targetPosition;
    private Coroutine moveCoroutine;

    public bool IsMoving => isMoving;

    public void SetData(float speed, float stoppingDistance)
    {
        moveSpeed = speed;
        arriveDistance = stoppingDistance;
    }

    public void MoveTo(Vector3 target, Action onComplete = null)
    {
        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);

        targetPosition = target;
        moveCoroutine = StartCoroutine(MoveRoutine(onComplete));
    }

    private IEnumerator MoveRoutine(Action onComplete)
    {
        isMoving = true;

        if (showDebug)
            Debug.Log($"{name} bergerak ke {targetPosition}");

        while (Vector3.Distance(transform.position, targetPosition) > arriveDistance)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                moveSpeed * Time.deltaTime
            );

            yield return null;
        }

        transform.position = targetPosition;

        isMoving = false;
        moveCoroutine = null;

        if (showDebug)
            Debug.Log($"{name} sampai tujuan");

        onComplete?.Invoke();
    }

    
}
