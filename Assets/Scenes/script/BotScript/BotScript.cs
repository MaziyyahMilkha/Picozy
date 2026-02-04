using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BotScript : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float jumpHeight = 2f; // Height of the jump arc
    [SerializeField] private float duration = 0.5f; // Time to reach destination

    private bool isMoving = false;

    // Call this method to start moving the object
    public void MoveTo(Vector3 targetPosition, System.Action onComplete = null)
    {
        if (!isMoving)
        {
            StartCoroutine(AnimateMove(targetPosition, onComplete));
        }
    }

    private IEnumerator AnimateMove(Vector3 targetPos, System.Action onComplete)
    {
        isMoving = true;
        Vector3 startPos = transform.position;
        float timeElapsed = 0f;

        while (timeElapsed < duration)
        {
            timeElapsed += Time.deltaTime;
            float t = timeElapsed / duration;

            // Simple Parabola / Arc movement for "Jump" effect
            // Linear interpolation for X and Z
            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
            
            // Add arc to Y axis: 4 * height * t * (1-t) creates a parabola (0 at t=0, 1 at t=0.5, 0 at t=1)
            currentPos.y += jumpHeight * 4 * t * (1 - t);

            transform.position = currentPos;
            yield return null;
        }

        transform.position = targetPos;
        isMoving = false;
        
        onComplete?.Invoke();
    }
}
