using System;
using System.Collections;
using UnityEngine;

public class CharacterMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 8f;

    [Header("Move animation (dahan ke dahan)")]
    [SerializeField] private float arcHeight = 0.2f;
    [SerializeField] private AnimationCurve moveEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

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
        Vector3 start = transform.position;
        float distance = Vector3.Distance(start, target);
        float duration = Mathf.Max(0.15f, distance / moveSpeed);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float tRaw = Mathf.Clamp01(elapsed / duration);
            float t = moveEase != null && moveEase.keys.Length > 0 ? moveEase.Evaluate(tRaw) : tRaw;

            Vector3 linear = Vector3.Lerp(start, target, t);
            float parabola = 4f * t * (1f - t);
            Vector3 arc = Vector3.up * (arcHeight * parabola);
            transform.position = linear + arc;

            yield return null;
        }

        transform.position = target;
        isMoving = false;
        onComplete?.Invoke();
    }
}
