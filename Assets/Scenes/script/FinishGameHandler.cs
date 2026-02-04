using UnityEngine;

public class FinishGameHandler : MonoBehaviour
{
    public int CurrentFilledFlaskCount;

    private void Start()
    {
        GlobalEvents.OnFlaskFilledByOneColor.AddListener(IncrementFilledFlaskCount);
    }

    private void OnDestroy()
    {
        GlobalEvents.OnFlaskFilledByOneColor.RemoveListener(IncrementFilledFlaskCount);
    }

    private void IncrementFilledFlaskCount()
    {
        CurrentFilledFlaskCount++;
        Debug.Log($"Filled Flasks: {CurrentFilledFlaskCount}");
        
        // Potential win condition check could can be added here
        // e.g. if (CurrentFilledFlaskCount >= TotalFlasks) GlobalEvents.SendLevelEnd(1);
    }
}
