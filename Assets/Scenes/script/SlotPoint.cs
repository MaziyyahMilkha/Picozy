using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SlotPoint : MonoBehaviour
{
    public int slotIndex; // 0,1,2
    private Branch parentBranch;

    private void Awake()
    {
        parentBranch = GetComponentInParent<Branch>();

        if (parentBranch == null)
        {
            Debug.LogError("SlotPoint TIDAK menemukan Branch di parent: " + name);
        }
    }
}
