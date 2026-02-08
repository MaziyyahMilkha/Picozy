using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SlotPoint : MonoBehaviour
{
    public int slotIndex; // 0,1,2
    private Branch parentBranch;
    private Collider col;

    private void Awake()
    {
        col = GetComponent<Collider>();

        // 🔑 AUTO ambil Branch dari parent
        parentBranch = GetComponentInParent<Branch>();

        if (parentBranch == null)
        {
            Debug.LogError("SlotPoint TIDAK menemukan Branch di parent: " + name);
        }
    }

    private void Update()
{
    if (parentBranch == null) return;

    // 🔥 SLOT SELALU BISA DIKLIK
    col.enabled = true;
}

    
}
