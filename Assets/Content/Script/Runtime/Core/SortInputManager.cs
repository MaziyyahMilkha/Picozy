using UnityEngine;

public class SortInputManager : MonoBehaviour
{
    public static SortInputManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Camera mainCamera;

    private SortKarakter selectedKarakter;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (mainCamera == null) mainCamera = Camera.main;
    }

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        Ray ray = mainCamera != null ? mainCamera.ScreenPointToRay(Input.mousePosition) : Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 200f)) return;

        if (selectedKarakter != null)
        {
            SortDahan dahan = hit.collider.GetComponent<SortDahan>();
            if (dahan != null)
            {
                TryMoveToDahan(dahan);
                return;
            }
        }

        SortKarakter karakter = hit.collider.GetComponent<SortKarakter>();
        if (karakter != null)
        {
            if (karakter.IsMoving()) return;
            selectedKarakter = karakter;
            return;
        }

        selectedKarakter = null;
    }

    private void TryMoveToDahan(SortDahan dahan)
    {
        if (selectedKarakter == null) return;
        if (selectedKarakter.IsMoving()) return;
        if (!dahan.HasSpace()) return;
        if (selectedKarakter.GetDahan() == dahan) return;
        if (!dahan.CanAccept(selectedKarakter)) return;

        SortKarakter moving = selectedKarakter;
        selectedKarakter = null;

        SortDahan oldDahan = moving.GetDahan();
        if (oldDahan != null)
            oldDahan.RemoveKarakter(moving);

        Vector3 targetPos = dahan.GetNextSlotPosition();
        moving.MoveTo(targetPos, () =>
        {
            dahan.AddKarakter(moving);
            NotifyMoveDone();
        });
    }

    private void NotifyMoveDone()
    {
        var gameplay = SortGameplayManager.Instance;
        if (gameplay != null)
            gameplay.CheckLevelComplete();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
