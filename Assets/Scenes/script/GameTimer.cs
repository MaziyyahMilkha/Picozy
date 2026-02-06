using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameTimer : MonoBehaviour
{
    [Header("Timer")]
    public float totalTime = 180f;
    private float currentTime;

    [Header("UI")]
    public TMP_Text timerText;
    public Image star1;
    public Image star2;
    public Image star3;

    [Header("Colors")]
    public Color activeColor = new Color(1f, 0.5f, 0f);
    public Color inactiveColor = Color.gray;

    void Start()
    {
        currentTime = totalTime;
        UpdateTimerText();
        UpdateStars();
    }

    void Update()
    {
        if (currentTime <= 0)
        {
            currentTime = 0;
            UpdateTimerText();
            return;
        }

        currentTime -= Time.deltaTime;
        UpdateTimerText();
        UpdateStars();
    }

    void UpdateTimerText()
    {
        int minutes = Mathf.FloorToInt(currentTime / 60);
        int seconds = Mathf.FloorToInt(currentTime % 60);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    void UpdateStars()
    {
        if (currentTime > 120)
        {
            star1.color = activeColor;
            star2.color = activeColor;
            star3.color = activeColor;
        }
        else if (currentTime > 60)
        {
            star1.color = activeColor;
            star2.color = activeColor;
            star3.color = inactiveColor;
        }
        else
        {
            star1.color = activeColor;
            star2.color = inactiveColor;
            star3.color = inactiveColor;
        }
    }
}
