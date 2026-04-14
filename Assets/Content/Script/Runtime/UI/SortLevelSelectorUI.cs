using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SortLevelSelectorUI : MonoBehaviour
{
    private const bool UseDebugLog = true;

    [Header("Slots")]
    [SerializeField] private List<SortLevelButtonSlot> levelSlots = new List<SortLevelButtonSlot>();

    [SerializeField] private Image levelSelectorBackgroundImage;
    [SerializeField] private Scrollbar levelPageScrollbar;
    [SerializeField] private ScrollRect levelScrollRect;

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
        float t0 = Time.realtimeSinceStartup;
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

        EnsureAvailableLevelIsVisible(manager);
        UpdatePageScrollbar(manager);

        float elapsed = Time.realtimeSinceStartup - t0;
        if (UseDebugLog)
        {
            Debug.LogWarning(
                $"[Perf][LevelSelectorUI] RefreshDisplay map={manager.CurrentMapIndex} page={manager.CurrentPageIndex} " +
                $"slots={levelSlots.Count} visible={countOnPage} total={elapsed * 1000f:0.0}ms");
        }
    }

    private void UpdatePageScrollbar(SortLevelSelectManager manager)
    {
        if (manager == null) return;
        ForceScrollToDefault();
    }

    private void EnsureAvailableLevelIsVisible(SortLevelSelectManager manager)
    {
        if (manager == null || levelScrollRect == null) return;

        SortLevelButtonSlot targetSlot = null;
        int countOnPage = manager.GetLevelCountOnCurrentPage();
        for (int i = 0; i < levelSlots.Count && i < countOnPage; i++)
        {
            var slot = levelSlots[i];
            if (slot == null || !slot.gameObject.activeInHierarchy) continue;
            int globalIndex = manager.GetGlobalLevelIndexForSlot(i);
            if (manager.GetSlotState(globalIndex) == LevelSlotState.Available)
            {
                targetSlot = slot;
                break;
            }
            if (targetSlot == null) targetSlot = slot;
        }

        if (targetSlot == null) return;
        RectTransform viewport = levelScrollRect.viewport != null
            ? levelScrollRect.viewport
            : levelScrollRect.GetComponent<RectTransform>();
        RectTransform targetRect = targetSlot.transform as RectTransform;
        if (viewport == null || targetRect == null) return;

        if (!IsRectFullyVisible(viewport, targetRect))
            ForceScrollToDefault();
    }

    private void ForceScrollToDefault()
    {
        if (levelPageScrollbar != null)
            levelPageScrollbar.value = 0f;
        if (levelScrollRect != null)
        {
            levelScrollRect.StopMovement();
            levelScrollRect.verticalNormalizedPosition = 0f;
            levelScrollRect.horizontalNormalizedPosition = 0f;
        }
    }

    private static bool IsRectFullyVisible(RectTransform viewport, RectTransform target)
    {
        Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, target);
        Rect r = viewport.rect;
        return bounds.min.x >= r.xMin && bounds.max.x <= r.xMax
            && bounds.min.y >= r.yMin && bounds.max.y <= r.yMax;
    }
}
