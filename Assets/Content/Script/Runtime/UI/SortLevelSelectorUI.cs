using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SortLevelSelectorUI : MonoBehaviour
{
    [Header("Slots")]
    [SerializeField] private List<SortLevelButtonSlot> levelSlots = new List<SortLevelButtonSlot>();

    [SerializeField] private Image levelSelectorBackgroundImage;

    public int GetLevelButtonCount() => levelSlots != null ? levelSlots.Count : 0;

    private void OnEnable()
    {
        var manager = SortLevelSelectManager.Instance;
        if (manager != null)
            manager.OnPageOrMapChanged += RefreshDisplay;
        StartCoroutine(RefreshNextFrame());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        var manager = SortLevelSelectManager.Instance;
        if (manager != null)
            manager.OnPageOrMapChanged -= RefreshDisplay;
    }

    private IEnumerator RefreshNextFrame()
    {
        yield return null;
        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        var manager = SortLevelSelectManager.Instance;
        if (manager == null) return;

        if (levelSelectorBackgroundImage != null)
        {
            Sprite bg = manager.GetCurrentMapLevelSelectorBackground();
            if (bg != null)
                levelSelectorBackgroundImage.sprite = bg;
        }

        Sprite slotLockBg = manager.GetCurrentMapSlotLockBackground();
        int countOnPage = manager.GetLevelCountOnCurrentPage();

        for (int i = 0; i < levelSlots.Count; i++)
        {
            SortLevelButtonSlot slotView = levelSlots[i];
            if (slotView == null) continue;
            if (i < countOnPage)
            {
                slotView.gameObject.SetActive(true);
                int globalIndex = manager.GetGlobalLevelIndexForSlot(i);
                int levelNum = manager.GetLevelNumberForSlot(i);
                LevelSlotState state = manager.GetSlotState(globalIndex);
                int stars = manager.GetStarsForLevel(globalIndex);
                slotView.SetState(state, levelNum, stars);
                if (slotLockBg != null)
                    slotView.SetLockBackground(slotLockBg);
                Button btn = slotView.Button;
                if (btn != null)
                {
                    int slot = i;
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => manager.PlayLevelAtSlot(slot));
                }
            }
            else
            {
                slotView.gameObject.SetActive(false);
            }
        }

    }
}
