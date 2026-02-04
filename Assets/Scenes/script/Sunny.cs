using UnityEngine;
using System.Collections;

public enum SunnyType
{
    Red,
    Blue,
    Yellow,
    Purple,
    Green
}

public class Sunny : MonoBehaviour
{
    public SunnyType SunnyType;
    private FlaskController currentFlask;
    private Coroutine moveCoroutine;

    // Manual movement settings
    [SerializeField] private float moveSpeed = 5.0f;
    [SerializeField] private float stoppingDistance = 0.1f;

    public void SetFlask(FlaskController flask)
    {
        currentFlask = flask;
    }

    public FlaskController GetFlask()
    {
        return currentFlask;
    }

    void OnMouseDown()
    {
        GameManager.Instance.SelectSunny(this);
    }

    public void MoveTo(Vector3 targetPosition)
    {
        Debug.Log($"Sunny: MoveTo called. Target: {targetPosition}, Current: {transform.position}");
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
        }
        moveCoroutine = StartCoroutine(MoveCoroutine(targetPosition));
    }

    public void StopMoving()
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }
        
        // Also stop animation if present
        Animator anim = GetComponent<Animator>();
        if (anim != null)
        {
            anim.SetBool("IsRunning", false);
        }
    }

    private IEnumerator MoveCoroutine(Vector3 target)
    {
        Animator anim = GetComponent<Animator>();
        if (anim != null)
        {
            anim.SetBool("IsRunning", true);
        }

        while (Vector3.Distance(transform.position, target) > stoppingDistance)
        {
            float step = moveSpeed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, target, step);
            
            // Optional: Face direction
            Vector3 direction = (target - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 10f);
            }

            yield return null;
        }
        
        Debug.Log("Sunny: Reached destination.");


        // Ensure we land exactly there? Optional.
        // transform.position = target; 

        if (anim != null)
        {
            anim.SetBool("IsRunning", false);
        }
    }
}
