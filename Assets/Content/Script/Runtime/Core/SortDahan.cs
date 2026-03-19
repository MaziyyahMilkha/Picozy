using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Feedbacks;

[RequireComponent(typeof(Collider))]
public class SortDahan : MonoBehaviour
{
    [Header("Slots")]
    [SerializeField] private Transform[] standPoints;

    [Header("Idle sway")]
    [SerializeField] private bool enableSway = true;
    [SerializeField] private float swayAngleMax = 0.5f;
    [SerializeField] private float swaySpeedMin = 0.35f;
    [SerializeField] private float swaySpeedMax = 0.7f;
    [SerializeField] private MMSpringFloat rotationSpring = new MMSpringFloat();

    [Header("Click feedback")]
    [SerializeField] private bool enableSelectFeedback = true;
    [SerializeField] private float selectScaleMultiplier = 1.08f;
    [SerializeField] private MMSpringFloat scaleSpring = new MMSpringFloat();

    [Header("Transfer feedback")]
    [SerializeField] private bool enableTransferFeedback = true;
    [SerializeField] private float transferOutRotationBump = 0.35f;
    [SerializeField] private float transferOutScaleBump = 0.02f;
    [SerializeField] private float transferInRotationBump = 0.6f;
    [SerializeField] private float transferInScaleBump = 0.05f;

    [Header("Complete feedback")]
    [SerializeField] private bool enableCompleteFeedback = true;
    [SerializeField] private float popUpHeight = 20f;
    [SerializeField] private float popDuration = 7f;
    [SerializeField] private float popHorizontalDrift = 0.6f;
    [SerializeField] private float popTopHoldDuration = 0.2f;
    [SerializeField] private float popDelayBetween = 0.08f;
    [SerializeField] private float branchFallAngle = 18f;
    [SerializeField] private float branchFallDownDistance = 10f;
    [SerializeField] private float branchFallDuration = 0.45f;
    [SerializeField] private float destroyAfterCompleteBuffer = 2.5f;


    private SortKarakter[] slots;
    private bool isBroken;
    private bool topIsHighIndex = true;
    private float _swayPhase;
    private float _swayAngleThis;
    private float _swaySpeedThis;
    private float _swayPhaseOffset;
    private Vector3 _restLocalEuler;
    private Vector3 _restLocalScale;
    private float _targetScaleMultiplier = 1f;

    private void Awake()
    {
        if (standPoints == null || standPoints.Length == 0)
            standPoints = new Transform[3];
        slots = new SortKarakter[standPoints.Length];
    }

    private void OnEnable()
    {
        _restLocalEuler = transform.localEulerAngles;
        _restLocalScale = transform.localScale;
        scaleSpring.CurrentValue = 1f;
        scaleSpring.TargetValue = 1f;
        scaleSpring.Velocity = 0f;

        ComputePerDahanSwayParams();
        rotationSpring.CurrentValue = 0f;
        rotationSpring.TargetValue = 0f;
        rotationSpring.Velocity = 0f;
    }

    private void ComputePerDahanSwayParams()
    {
        int id = GetInstanceID();
        float t = ((id * 0.618f) % 1f + 1f) % 1f;
        _swayAngleThis = swayAngleMax * (0.4f + 0.6f * t);
        _swaySpeedThis = Mathf.Lerp(swaySpeedMin, swaySpeedMax, 1f - t);
        _swayPhaseOffset = (id % 100) * 0.0628f;
    }

    private void Update()
    {
        if (enableSway)
        {
            _swayPhase += Time.deltaTime * _swaySpeedThis;
            float targetZ = Mathf.Sin(_swayPhase + _swayPhaseOffset) * _swayAngleThis;
            rotationSpring.MoveTo(targetZ);
            rotationSpring.UpdateSpringValue(Time.deltaTime);
            float z = _restLocalEuler.z + rotationSpring.CurrentValue;
            transform.localEulerAngles = new Vector3(_restLocalEuler.x, _restLocalEuler.y, z);
        }

        if (enableSelectFeedback)
        {
            scaleSpring.MoveTo(_targetScaleMultiplier);
            scaleSpring.UpdateSpringValue(Time.deltaTime);
            float s = scaleSpring.CurrentValue;
            transform.localScale = new Vector3(_restLocalScale.x * s, _restLocalScale.y * s, _restLocalScale.z * s);
        }
    }

    public void SetRestScale(Vector3 restScale)
    {
        _restLocalScale = restScale;
    }

    public void OnSelected()
    {
        if (!enableSelectFeedback) return;
        _targetScaleMultiplier = selectScaleMultiplier;
    }

    public void OnDeselected()
    {
        if (!enableSelectFeedback) return;
        _targetScaleMultiplier = 1f;
    }

    public void OnTransferOut()
    {
        if (!enableTransferFeedback) return;
        // Small recoil on source branch when a character leaves.
        rotationSpring.Bump(-transferOutRotationBump);
        scaleSpring.Bump(transferOutScaleBump);
    }

    public void OnTransferIn()
    {
        if (!enableTransferFeedback) return;
        // Slight stronger bump on destination branch when character lands.
        rotationSpring.Bump(transferInRotationBump);
        scaleSpring.Bump(transferInScaleBump);
    }

    public bool CanAccept(SortKarakter character)
    {
        if (character == null || isBroken) return false;
        int? existingKind = null;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;
            if (!existingKind.HasValue)
                existingKind = slots[i].Kind;
            else if (slots[i].Kind != existingKind.Value)
                return false;
        }
        if (!existingKind.HasValue) return true;
        return character.Kind == existingKind.Value;
    }

    public bool HasSpace()
    {
        if (isBroken) return false;
        for (int i = 0; i < slots.Length; i++)
            if (slots[i] == null) return true;
        return false;
    }

    public bool IsSlotEmpty(int index)
    {
        if (index < 0 || index >= slots.Length) return false;
        return slots[index] == null;
    }

    public Vector3 GetSlotPosition(int index)
    {
        if (index < 0 || index >= standPoints.Length) return transform.position;
        return standPoints[index].position;
    }

    public Transform GetSlotTransform(int index)
    {
        if (standPoints == null || index < 0 || index >= standPoints.Length) return null;
        return standPoints[index];
    }

    public Vector3 GetNextSlotPosition()
    {
        for (int i = 0; i < slots.Length; i++)
            if (slots[i] == null)
                return GetSlotPosition(i);
        return transform.position;
    }

    public int GetNextSlotIndex()
    {
        for (int i = 0; i < slots.Length; i++)
            if (slots[i] == null) return i;
        return -1;
    }

    public void AddCharacter(SortKarakter character)
    {
        if (isBroken || character == null) return;
        int idx = GetNextSlotIndex();
        if (idx < 0) return;
        slots[idx] = character;
        character.SetDahan(this);
        Transform slotParent = GetSlotTransform(idx) ?? transform;
        character.transform.SetParent(slotParent, false);
        character.transform.localPosition = Vector3.zero;
        CheckAllMatched();
    }

    public void AddCharacterAtSlot(SortKarakter character, int index)
    {
        if (isBroken || character == null || index < 0 || index >= slots.Length) return;
        if (slots[index] != null) return;
        slots[index] = character;
        character.SetDahan(this);
        Transform slotParent = GetSlotTransform(index) ?? transform;
        character.transform.SetParent(slotParent, false);
        character.transform.localPosition = Vector3.zero;
        CheckAllMatched();
    }

    public void RemoveCharacter(SortKarakter character)
    {
        if (isBroken) return;
        for (int i = 0; i < slots.Length; i++)
            if (slots[i] == character) { slots[i] = null; return; }
    }

    private void CheckAllMatched()
    {
        if (isBroken) return;
        for (int i = 0; i < slots.Length; i++)
            if (slots[i] == null) return;
        int kind = slots[0].Kind;
        for (int i = 1; i < slots.Length; i++)
            if (slots[i].Kind != kind) return;
        if (SortGameplayController.Instance != null)
            SortGameplayController.Instance.OnDahanComplete(this);
        else
            StartCoroutine(CollectedRoutine(false));
    }

    public void CollectAndClear()
    {
        StartCoroutine(CollectedRoutine(false));
    }

    public void CollectAndDestroyAfterFeedback()
    {
        StartCoroutine(CollectedRoutine(true));
    }

    private IEnumerator CollectedRoutine(bool destroyBranchAtEnd)
    {
        isBroken = true;
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        if (enableCompleteFeedback)
        {
            int start = topIsHighIndex ? slots.Length - 1 : 0;
            int step = topIsHighIndex ? -1 : 1;

            for (int i = start; i >= 0 && i < slots.Length; i += step)
            {
                var k = slots[i];
                if (k == null) continue;
                slots[i] = null;
                StartCoroutine(PopAndDestroyCharacter(k));
                yield return new WaitForSeconds(popDelayBetween);
            }

            yield return StartCoroutine(PlayBranchFall(destroyBranchAtEnd));
        }
        else
        {
            yield return new WaitForSeconds(0.2f);
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null)
                {
                    Destroy(slots[i].gameObject);
                    slots[i] = null;
                }
            }
        }

        isBroken = false;
        if (col != null) col.enabled = true;
        if (SortGameplayController.Instance != null)
            SortGameplayController.Instance.CheckLevelComplete();

        if (destroyBranchAtEnd)
        {
            if (destroyAfterCompleteBuffer > 0f)
                yield return new WaitForSeconds(destroyAfterCompleteBuffer);
            Destroy(gameObject);
        }
    }

    private IEnumerator PopAndDestroyCharacter(SortKarakter k)
    {
        if (k == null) yield break;

        Transform t = k.transform;
        // Detach from branch so branch fall won't drag character down.
        t.SetParent(null, true);
        Vector3 startPos = t.position;
        Vector3 upPos = startPos + Vector3.up * popUpHeight;
        float sideDir = transform.position.x <= 0f ? 1f : -1f;
        float drift = Random.Range(popHorizontalDrift * 0.55f, popHorizontalDrift) * sideDir;
        float elapsed = 0f;

        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float t01 = Mathf.Clamp01(elapsed / popDuration);
            float p = 1f - Mathf.Pow(1f - t01, 3f); 

            Vector3 pos = Vector3.Lerp(startPos, upPos, p);
            float sideLerp = Mathf.SmoothStep(0f, 1f, t01);
            pos.x += drift * sideLerp;
            t.position = pos;
            yield return null;
        }

        if (popTopHoldDuration > 0f)
            yield return new WaitForSeconds(popTopHoldDuration);

        Destroy(k.gameObject);
    }

    private IEnumerator PlayBranchFall(bool keepFallenPose)
    {
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        Vector3 endPos = startPos + Vector3.down * branchFallDownDistance;
        Quaternion endRot = startRot * Quaternion.Euler(0f, 0f, -branchFallAngle);

        float elapsed = 0f;
        while (elapsed < branchFallDuration)
        {
            elapsed += Time.deltaTime;
            float t01 = Mathf.Clamp01(elapsed / branchFallDuration);
            float p = t01 * t01; // easeInQuad
            transform.position = Vector3.Lerp(startPos, endPos, p);
            transform.rotation = Quaternion.Slerp(startRot, endRot, p);
            yield return null;
        }

        if (!keepFallenPose)
        {
            // Snap back to original pose for reuse.
            transform.position = startPos;
            transform.rotation = startRot;
        }
    }

    public int GetSlotIndex(SortKarakter karakter)
    {
        for (int i = 0; i < slots.Length; i++)
            if (slots[i] == karakter) return i;
        return -1;
    }

    public SortKarakter GetCharacterAtSlot(int index)
    {
        if (index < 0 || index >= slots.Length) return null;
        return slots[index];
    }

    public void SetTopIsHighIndex(bool high) { topIsHighIndex = high; }

    public void GetTopGroup(out int? kind, out int count, List<int> outSlotIndices)
    {
        kind = null;
        count = 0;
        outSlotIndices?.Clear();
        if (slots == null || isBroken) return;

        int step = topIsHighIndex ? -1 : 1;
        int start = topIsHighIndex ? slots.Length - 1 : 0;
        int i = start;
        int? firstKind = null;
        while (i >= 0 && i < slots.Length)
        {
            if (slots[i] == null) { i += step; continue; }
            if (!firstKind.HasValue) firstKind = slots[i].Kind;
            if (slots[i].Kind != firstKind.Value) break;
            count++;
            outSlotIndices?.Add(i);
            i += step;
        }
        kind = firstKind;
    }

    public int GetEmptySlotCount()
    {
        if (slots == null) return 0;
        int n = 0;
        for (int i = 0; i < slots.Length; i++)
            if (slots[i] == null) n++;
        return n;
    }

    public int? GetTopKind()
    {
        if (slots == null) return null;
        int start = topIsHighIndex ? slots.Length - 1 : 0;
        int step = topIsHighIndex ? -1 : 1;
        for (int i = start; i >= 0 && i < slots.Length; i += step)
            if (slots[i] != null) return slots[i].Kind;
        return null;
    }

    public void GetNextEmptySlotIndicesForAdd(int count, List<int> outIndices)
    {
        outIndices?.Clear();
        if (slots == null || count <= 0) return;
        for (int i = 0; i < slots.Length && outIndices.Count < count; i++)
            if (slots[i] == null) outIndices.Add(i);
    }

    public SortKarakter RemoveCharacterAtSlot(int index)
    {
        if (index < 0 || index >= slots.Length) return null;
        var k = slots[index];
        slots[index] = null;
        return k;
    }

    public void CompactSlots()
    {
        if (slots == null || isBroken) return;
        var filled = new List<SortKarakter>(slots.Length);
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null) filled.Add(slots[i]);
            slots[i] = null;
        }
        for (int i = 0; i < filled.Count && i < slots.Length; i++)
        {
            slots[i] = filled[i];
            Transform slotParent = GetSlotTransform(i) ?? transform;
            filled[i].transform.SetParent(slotParent, false);
            filled[i].transform.localPosition = Vector3.zero;
        }
    }
}
