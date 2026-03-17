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

    private Button _button;

    public Button Button => _button != null ? _button : _button = GetComponent<Button>();

    public void SetLevelNumber(int levelNumber)
    {
        if (levelNumberText != null)
            levelNumberText.text = levelNumber > 0 ? levelNumber.ToString() : "";
    }

    public void SetState(LevelSlotState state, int levelNumber)
    {
        SetLevelNumber(levelNumber);
        if (lockBackground != null)
            lockBackground.SetActive(state == LevelSlotState.Completed);
        if (unlockBackground != null)
            unlockBackground.SetActive(state == LevelSlotState.Locked || state == LevelSlotState.Available);
        if (unlockIcon != null)
            unlockIcon.SetActive(state == LevelSlotState.Locked);
        if (levelNumberText != null)
            levelNumberText.gameObject.SetActive(state == LevelSlotState.Available || state == LevelSlotState.Completed);
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
