using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    private Sunny selectedSunny;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SelectSunny(Sunny sunny)
    {
        if (selectedSunny == null)
        {
            selectedSunny = sunny;
            // Add visual feedback/logic here
             Debug.Log("Selected Sunny: " + sunny.name);
        }
        else
        {
             // If clicking another sunny, switch selection
             selectedSunny = sunny;
             Debug.Log("Switched selection to: " + sunny.name);
        }
    }

    public void SelectFlask(FlaskController flask)
    {
        if (selectedSunny != null)
        {
             if (flask.CanAddSunny(selectedSunny))
             {
                 // Move logic
                 FlaskController oldFlask = selectedSunny.GetFlask();
                 // Logic to remove from old flask might be needed if FlaskController doesn't handle "Remove" automatically
                 // But ProcessBotPosition in FlaskController seems to only handle "Add". 
                 // However, since we are moving GameObjects, we rely on the implementation details.
                 // NOTE: FlaskController has a Stack<GameObject> bots. We can't easily "Pop" a specific one if it's not the top.
                 // But in this game rules, we only move the TOP one.
                 
                 if (oldFlask != null)
                 {
                     oldFlask.RemoveTopSunny();
                 }
                 
                 flask.AddSunny(selectedSunny);
                 selectedSunny = null; // Deselect
             }
        }
    }
}