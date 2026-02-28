using System;
using System.Collections;
using UnityEngine;

public class CharacterMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float arriveDistance = 0.02f;

    private bool isMoving;

    public bool IsMoving => isMoving;

    public void MoveTo(Vector3 target, Action onComplete = null)
    {
        if (isMoving) return;
        StartCoroutine(MoveRoutine(target, onComplete));
    }

    private IEnumerator MoveRoutine(Vector3 target, Action onComplete)
    {
        isMoving = true;
        while (Vector3.Distance(transform.position, target) > arriveDistance)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = target;
        isMoving = false;
        onComplete?.Invoke();
    }
}
