using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SortInputManager : MonoBehaviour
{
    public static SortInputManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Camera mainCamera;

    [Header("Feedback")]
    [SerializeField] private float selectScale = 1.08f;
    [SerializeField] private float shakeDuration = 0.15f;
    [SerializeField] private float shakeStrength = 0.08f;

    private SortDahan selectedDahan;
    private SortKind? selectedKind;
    private int selectedCount;
    private Vector3 selectedDahanBaseScale;
    private static readonly List<int> _groupSlots = new List<int>(8);

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
        if (!Physics.Raycast(ray, out RaycastHit hit, 200f)) { Deselect(); return; }

        SortDahan hitDahan = hit.collider.GetComponent<SortDahan>();
        if (hitDahan == null)
        {
            SortKarakter hitCharacter = hit.collider.GetComponent<SortKarakter>();
            if (hitCharacter != null)
                hitDahan = hitCharacter.GetDahan();
        }
        if (hitDahan == null) { Deselect(); return; }

        if (selectedDahan != null)
        {
            TryMoveToDahan(hitDahan);
            return;
        }

        SelectDahan(hitDahan);
    }

    private void SelectDahan(SortDahan dahan)
    {
        Deselect();
        if (dahan == null) return;

        _groupSlots.Clear();
        dahan.GetTopGroup(out SortKind? kind, out int count, _groupSlots);
        if (!kind.HasValue || count <= 0) return;

        selectedDahan = dahan;
        selectedKind = kind.Value;
        selectedCount = count;
        selectedDahanBaseScale = dahan.transform.localScale;
        dahan.transform.localScale = selectedDahanBaseScale * selectScale;
    }

    private void Deselect()
    {
        if (selectedDahan != null)
        {
            selectedDahan.transform.localScale = selectedDahanBaseScale;
            selectedDahan = null;
        }
        selectedKind = null;
        selectedCount = 0;
    }

    private void TryMoveToDahan(SortDahan dest)
    {
        if (selectedDahan == null || !selectedKind.HasValue || selectedCount <= 0) { Deselect(); return; }
        if (selectedDahan == dest) { Deselect(); return; }

        var gameplay = SortGameplayController.Instance;
        if (gameplay == null) { Deselect(); return; }

        if (gameplay.CanMove(selectedDahan, dest, selectedKind.Value, selectedCount))
        {
            SortDahan from = selectedDahan;
            int count = selectedCount;
            Deselect();
            gameplay.DoMove(from, dest, count);
        }
        else
        {
            var dahanToShake = selectedDahan;
            StartCoroutine(ShakeInvalidRoutine(dahanToShake, Deselect));
        }
    }

    private IEnumerator ShakeInvalidRoutine(SortDahan dahan, Action onDone)
    {
#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif
        if (dahan == null) { onDone?.Invoke(); yield break; }
        Transform t = dahan.transform;
        Vector3 pos = t.position;
        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float x = UnityEngine.Random.Range(-1f, 1f) * shakeStrength;
            float y = UnityEngine.Random.Range(-1f, 1f) * shakeStrength;
            t.position = pos + new Vector3(x, y, 0f);
            yield return null;
        }
        t.position = pos;
        onDone?.Invoke();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
