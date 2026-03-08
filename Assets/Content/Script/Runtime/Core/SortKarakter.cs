using UnityEngine;

[RequireComponent(typeof(CharacterMovement))]
[RequireComponent(typeof(Collider))]
public class SortKarakter : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private int kind;

    [Header("Visual per kind")]
    [SerializeField] private GameObject[] kindVisuals;

    private SortDahan currentDahan;
    private CharacterMovement movement;

    public int Kind => kind;

    private void Awake()
    {
        movement = GetComponent<CharacterMovement>();
        ApplyKindVisual();
    }

    public void SetKind(int newKind)
    {
        kind = newKind;
        ApplyKindVisual();
    }

    private void ApplyKindVisual()
    {
        if (kindVisuals == null || kindVisuals.Length == 0) return;

        for (int i = 0; i < kindVisuals.Length; i++)
        {
            if (kindVisuals[i] != null)
                kindVisuals[i].SetActive(false);
        }

        if (kind >= 0 && kind < kindVisuals.Length && kindVisuals[kind] != null)
            kindVisuals[kind].SetActive(true);
    }

    public void SetDahan(SortDahan dahan)
    {
        currentDahan = dahan;
    }

    public SortDahan GetDahan()
    {
        return currentDahan;
    }

    public bool IsMoving()
    {
        return movement != null && movement.IsMoving;
    }

    public void MoveTo(Vector3 position, System.Action onComplete = null)
    {
        if (movement != null)
            movement.MoveTo(position, onComplete);
        else
        {
            transform.position = position;
            onComplete?.Invoke();
        }
    }
}
