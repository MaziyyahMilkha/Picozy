using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlotPoint : MonoBehaviour
{
    public Branch parentBranch;
    public int slotIndex; // 0,1,2


    private void OnMouseDown()
{
    Debug.Log("SlotPoint clicked: " + name + " ParentBranch: " + (parentBranch != null ? parentBranch.name : "NULL"));
    if (GameManager.Instance == null) return;
    GameManager.Instance.MoveSelectedSunnyToSpecificSlot(parentBranch, slotIndex);
}


}

