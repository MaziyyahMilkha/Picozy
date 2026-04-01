using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SortStarDisplay : MonoBehaviour
{
    [Header("Level name")]
    [SerializeField] private TMP_Text levelNameText;

    [Header("Timer fill")]
    [SerializeField] private Image timerFillImage;

    [Header("Stars")]
    [SerializeField] private Graphic star1;
    [SerializeField] private Graphic star2;
    [SerializeField] private Graphic star3;

    [Header("Lerp")]
    [SerializeField] private float lerpSpeed = 6f;

    private const float Star3Threshold = 0.6f;
    private const float Star2Threshold = 0.3f;
    private const float Star1Threshold = 0f;

    private float _currentFill = 1f;
    private float _currentAlpha1 = 1f, _currentAlpha2 = 1f, _currentAlpha3 = 1f;
    private float _targetFill = 1f;
    private float _targetAlpha1 = 1f, _targetAlpha2 = 1f, _targetAlpha3 = 1f;

    public void SetNormalizedTime(float normalized)
    {
        _targetFill = Mathf.Clamp01(normalized);
        _targetAlpha1 = normalized > Star1Threshold ? 1f : 0f;
        _targetAlpha2 = normalized > Star2Threshold ? 1f : 0f;
        _targetAlpha3 = normalized > Star3Threshold ? 1f : 0f;
    }

    public void SetLevelNumber(int levelNumber)
    {
        if (levelNameText != null)
            levelNameText.text = "Level " + (levelNumber > 0 ? levelNumber : 1);
    }

    public void ResetToFull()
    {
        _targetFill = 1f;
        _targetAlpha1 = _targetAlpha2 = _targetAlpha3 = 1f;
        _currentFill = 1f;
        _currentAlpha1 = _currentAlpha2 = _currentAlpha3 = 1f;
        ApplyFillAndStars();
    }

    private void Update()
    {
        float t = lerpSpeed * Time.deltaTime;
        _currentFill = Mathf.Lerp(_currentFill, _targetFill, t);
        _currentAlpha1 = Mathf.Lerp(_currentAlpha1, _targetAlpha1, t);
        _currentAlpha2 = Mathf.Lerp(_currentAlpha2, _targetAlpha2, t);
        _currentAlpha3 = Mathf.Lerp(_currentAlpha3, _targetAlpha3, t);
        ApplyFillAndStars();
    }

    private void ApplyFillAndStars()
    {
        if (timerFillImage != null)
        {
            timerFillImage.type = Image.Type.Filled;
            timerFillImage.fillAmount = _currentFill;
        }
        SetAlpha(star1, _currentAlpha1);
        SetAlpha(star2, _currentAlpha2);
        SetAlpha(star3, _currentAlpha3);
    }

    private static void SetAlpha(Graphic g, float alpha)
    {
        if (g == null) return;
        var c = g.color;
        c.a = alpha;
        g.color = c;
    }
}
