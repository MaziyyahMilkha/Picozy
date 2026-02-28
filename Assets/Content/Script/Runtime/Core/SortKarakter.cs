using UnityEngine;

[RequireComponent(typeof(CharacterMovement))]
[RequireComponent(typeof(Collider))]
public class SortKarakter : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private SortKind kind;

    [Header("Visual per kind (urut = enum SortKind)")]
    [SerializeField] private GameObject[] kindVisuals;

    private SortDahan currentDahan;
    private CharacterMovement movement;

    public SortKind Kind => kind;

    private void Awake()
    {
        movement = GetComponent<CharacterMovement>();
        ApplyKindVisual();
    }

    public void SetKind(SortKind newKind)
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

        int idx = (int)kind;
        GameObject activeVisual = (idx >= 0 && idx < kindVisuals.Length && kindVisuals[idx] != null) ? kindVisuals[idx] : null;
        if (activeVisual != null)
        {
            activeVisual.SetActive(true);
            ApplyColorToRenderers(activeVisual, SortKindColors.Get(kind));
        }
    }

    private static MaterialPropertyBlock _colorBlock;

    private void ApplyColorToRenderers(GameObject root, Color color)
    {
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers == null) return;
        if (_colorBlock == null) _colorBlock = new MaterialPropertyBlock();
        _colorBlock.SetColor("_BaseColor", color);
        _colorBlock.SetColor("_Color", color);
        foreach (var r in renderers)
        {
            if (r == null) continue;
            r.SetPropertyBlock(_colorBlock);
        }
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
