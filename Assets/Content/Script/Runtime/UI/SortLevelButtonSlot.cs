using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum LevelSlotState
{
    Locked,
    Available,
    Completed
}

public class SortLevelButtonSlot : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject lockBackground;
    [SerializeField] private GameObject unlockBackground;
    [SerializeField] private GameObject unlockIcon;
    [SerializeField] private TMP_Text levelNumberText;

    [Header("Stars")]
    [SerializeField] private GameObject star1;
    [SerializeField] private GameObject star2;
    [SerializeField] private GameObject star3;

    private Button _button;

    public Button Button => _button != null ? _button : _button = GetComponent<Button>();

    private void Awake()
    {
        HideStars();
    }

    private void HideStars()
    {
        if (star1 != null) star1.SetActive(false);
        if (star2 != null) star2.SetActive(false);
        if (star3 != null) star3.SetActive(false);
    }

    public void SetLevelNumber(int levelNumber)
    {
        if (levelNumberText != null)
            levelNumberText.text = levelNumber > 0 ? levelNumber.ToString() : "";
    }

    public void SetState(LevelSlotState state, int levelNumber, int stars = 0)
    {
        int s = (state == LevelSlotState.Completed) ? (stars <= 0 ? 1 : Mathf.Clamp(stars, 1, 3)) : 0;

        SetLevelNumber(levelNumber);
        if (lockBackground != null)
            lockBackground.SetActive(state == LevelSlotState.Completed);
        if (unlockBackground != null)
            unlockBackground.SetActive(state == LevelSlotState.Locked || state == LevelSlotState.Available);
        if (unlockIcon != null)
            unlockIcon.SetActive(state == LevelSlotState.Locked);
        if (levelNumberText != null)
            levelNumberText.gameObject.SetActive(state == LevelSlotState.Available || state == LevelSlotState.Completed);

        bool show1 = s >= 1, show2 = s >= 2, show3 = s >= 3;
        if (star1 != null) star1.SetActive(show1);
        if (star2 != null) star2.SetActive(show2);
        if (star3 != null) star3.SetActive(show3);

        var btn = Button;
        if (btn != null)
            btn.interactable = state != LevelSlotState.Locked;
    }

    public void SetLockBackground(Sprite sprite)
    {
        if (lockBackground == null || sprite == null) return;
        var img = lockBackground.GetComponent<Image>();
        if (img != null)
            img.sprite = sprite;
    }
}
