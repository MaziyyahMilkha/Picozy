using System.Collections;
using UnityEngine;
using MoreMountains.Feedbacks;

[RequireComponent(typeof(CharacterMovement))]
[RequireComponent(typeof(Collider))]
public class SortKarakter : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private int kind;

    [Header("Visual per kind")]
    [SerializeField] private GameObject[] kindVisuals;

    [Header("Move feedback (dahan A → B)")]
    [SerializeField] private bool enableMoveFeedback = true;
    [SerializeField] private float takeOffSquash = 0.92f;
    [SerializeField] private float takeOffPeak = 1.05f;
    [SerializeField] private float takeOffDuration = 0.08f;
    [SerializeField] private float landingBump = 1.12f;
    [SerializeField] private float landingDuration = 0.14f;

    [Header("Idle breathing")]
    [SerializeField] private bool enableBreathing = true;
    [SerializeField] private float breathScaleAmountMax = 0.038f;
    [SerializeField] private float breathSpeedMin = 1f;
    [SerializeField] private float breathSpeedMax = 1.55f;
    [SerializeField] private MMSpringFloat scaleSpring = new MMSpringFloat();

    private SortDahan currentDahan;
    private CharacterMovement movement;
    private Vector3 _restLocalScale;
    private float _breathPhase;
    private float _breathScaleAmount;
    private float _breathSpeed;
    private float _breathPhaseOffset;

    public int Kind => kind;

    private void Awake()
    {
        movement = GetComponent<CharacterMovement>();
        ApplyKindVisual();
    }

    private void OnEnable()
    {
        if (enableBreathing)
            StartBreathing();
    }

    private void OnDisable()
    {
        StopBreathing();
    }

    private void OnDestroy()
    {
        StopBreathing();
    }

    private void StartBreathing()
    {
        if (!enableBreathing) return;

        _restLocalScale = transform.localScale;
        ComputePerKindBreathParams();
        _breathPhase = _breathPhaseOffset;

        scaleSpring.CurrentValue = 1f;
        scaleSpring.TargetValue = 1f;
        scaleSpring.Velocity = 0f;
    }

    private void ComputePerKindBreathParams()
    {
        float t = KindToFloat(kind);
        _breathScaleAmount = breathScaleAmountMax * (0.5f + 0.5f * t);
        _breathSpeed = Mathf.Lerp(breathSpeedMin, breathSpeedMax, 1f - t);
        _breathPhaseOffset = (kind * 0.62f) % (2f * Mathf.PI);
    }

    private static float KindToFloat(int k)
    {
        return ((k * 0.618f) % 1f + 1f) % 1f;
    }

    private void StopBreathing()
    {
        scaleSpring.Stop();
        scaleSpring.Finish();
        transform.localScale = _restLocalScale;
    }

    private void Update()
    {
        if (!enableBreathing) return;
        if (movement != null && movement.IsMoving) return;

        _breathPhase += Time.deltaTime * _breathSpeed;
        float targetScale = 1f + Mathf.Sin(_breathPhase) * _breathScaleAmount;

        scaleSpring.MoveTo(targetScale);
        scaleSpring.UpdateSpringValue(Time.deltaTime);

        float s = scaleSpring.CurrentValue;
        transform.localScale = new Vector3(_restLocalScale.x * s, _restLocalScale.y * s, _restLocalScale.z * s);
    }

    public void SetKind(int newKind)
    {
        kind = newKind;
        ApplyKindVisual();
        if (enableBreathing)
            ComputePerKindBreathParams();
    }

    private void ApplyKindVisual()
    {
        if (kindVisuals == null || kindVisuals.Length == 0) return;

        for (int i = 0; i < kindVisuals.Length; i++)
        {
            if (kindVisuals[i] != null)
                kindVisuals[i].SetActive(false);
        }

        if (kind >= 0 && kind < kindVisuals.Length && kindVisuals[kind] != null)
            kindVisuals[kind].SetActive(true);
    }

    public void SetDahan(SortDahan dahan)
    {
        currentDahan = dahan;
    }

    public SortDahan GetDahan()
    {
        return currentDahan;
    }

    public bool IsMoving()
    {
        return movement != null && movement.IsMoving;
    }

    public void MoveTo(Vector3 position, System.Action onComplete = null)
    {
        StopBreathing();
        if (enableMoveFeedback)
            StartCoroutine(TakeOffThenMove(position, onComplete));
        else
            DoMoveOnly(position, onComplete);
    }

    private void DoMoveOnly(Vector3 position, System.Action onComplete)
    {
        if (movement != null)
            movement.MoveTo(position, () => { OnLandingDone(onComplete); });
        else
        {
            transform.position = position;
            OnLandingDone(onComplete);
        }
    }

    private IEnumerator TakeOffThenMove(Vector3 position, System.Action onComplete)
    {
        Vector3 baseScale = transform.localScale;
        float elapsed = 0f;
        while (elapsed < takeOffDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / takeOffDuration);
            float s;
            if (t < 0.5f)
                s = Mathf.Lerp(1f, takeOffSquash, t * 2f);
            else if (t < 0.8f)
                s = Mathf.Lerp(takeOffSquash, takeOffPeak, (t - 0.5f) / 0.3f);
            else
                s = Mathf.Lerp(takeOffPeak, 1f, (t - 0.8f) / 0.2f);
            transform.localScale = new Vector3(baseScale.x * s, baseScale.y * s, baseScale.z * s);
            yield return null;
        }
        transform.localScale = baseScale;

        if (movement != null)
            movement.MoveTo(position, () => StartCoroutine(LandingThenDone(baseScale, onComplete)));
        else
        {
            transform.position = position;
            StartCoroutine(LandingThenDone(baseScale, onComplete));
        }
    }

    private IEnumerator LandingThenDone(Vector3 baseScale, System.Action onComplete)
    {
        float elapsed = 0f;
        float upDuration = landingDuration * 0.35f;
        float downDuration = landingDuration - upDuration;

        while (elapsed < upDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / upDuration;
            float s = Mathf.Lerp(1f, landingBump, t);
            transform.localScale = new Vector3(baseScale.x * s, baseScale.y * s, baseScale.z * s);
            yield return null;
        }
        transform.localScale = new Vector3(baseScale.x * landingBump, baseScale.y * landingBump, baseScale.z * landingBump);

        elapsed = 0f;
        while (elapsed < downDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / downDuration;
            float s = Mathf.Lerp(landingBump, 1f, t * t);
            transform.localScale = new Vector3(baseScale.x * s, baseScale.y * s, baseScale.z * s);
            yield return null;
        }
        transform.localScale = baseScale;

        OnLandingDone(onComplete);
    }

    private void OnLandingDone(System.Action onComplete)
    {
        onComplete?.Invoke();
        if (enableBreathing)
            StartBreathing();
    }
}
