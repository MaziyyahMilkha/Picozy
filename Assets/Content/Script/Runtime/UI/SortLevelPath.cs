using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class SortLevelPath : MonoBehaviour
{
    private const bool UseDebugLog = true;

    [Serializable]
    public class LinkCurveSettings
    {
        public Vector2 startHandleOffset = new Vector2(120f, 0f);
        public Vector2 endHandleOffset = new Vector2(-120f, 0f);

        [Tooltip("Aktifkan jika ingin sample manual untuk link ini.")]
        public bool overrideAutoSampleCount;
        [Min(2)] public int sampleCount = 14;

        [Header("Dot Position Tuning")]
        [Range(0f, 1f)] public float dotTStart = 0f;
        [Range(0f, 1f)] public float dotTEnd = 1f;
        public Vector2 dotOffset = Vector2.zero;
    }

    [Header("Nodes (urut level)")]
    [SerializeField] private List<RectTransform> levelNodes = new List<RectTransform>();
    [SerializeField] private List<LinkCurveSettings> linkCurves = new List<LinkCurveSettings>();

    [Header("Auto Sample By Length")]
    [SerializeField] [Min(0.1f)] private float samplesPer100Units = 8f;
    [SerializeField] [Min(2)] private int autoSampleMin = 8;
    [SerializeField] [Min(2)] private int autoSampleMax = 80;

    [Header("Gizmo")]
    [SerializeField] private bool drawSampleDots = true;
    [SerializeField] [Min(0.001f)] private float gizmoDotRadius = 6f;

    [Header("Path Dot Visual")]
    [SerializeField] private GameObject dotLightOnPrefab;
    [SerializeField] private GameObject dotLightOffPrefab;

    [Header("Node Display")]
    [SerializeField] private bool showNodes = true;
    [SerializeField] private bool useWalkAnimation = true;

    private const int CurveResolution = 40;
    private const string PointNamePrefix = "PathPoint_";
    private static readonly Vector2 FallbackDotSize = new Vector2(16f, 16f);

    // Lebih pelan dari versi sebelumnya, tidak diexpose ke inspector.
    private const float AnimateStepSeconds = 0.22f;
    private const float AnimateLoopDelaySeconds = 0.5f;

    private enum LinkLightMode
    {
        Hidden,
        On,
        Animate
    }

    private sealed class RuntimeDot
    {
        public int LinkIndex;
        public int OrderIndex;
        public GameObject OnObject;
        public GameObject OffObject;
    }

    private readonly List<RuntimeDot> _runtimeDots = new List<RuntimeDot>();
    private bool _runtimeBuilt;
    private int _animatedLinkIndex = -1;
    private int _animatedDotCursor;
    private float _animatedTimer;
    private bool _animateWaitingDelay;

    private void OnEnable()
    {
        _animatedLinkIndex = -1;
        _animatedDotCursor = 0;
        _animatedTimer = 0f;
        _animateWaitingDelay = false;
    }

    private void OnDisable()
    {
        _animatedLinkIndex = -1;
        _animatedDotCursor = 0;
        _animatedTimer = 0f;
        _animateWaitingDelay = false;
    }

    private void OnValidate()
    {
        EnsureLinkCurveSize();
    }

    private void Update()
    {
        if (!Application.isPlaying) return;
        EnsureRuntimeDotsBuilt();
        UpdateRuntimeLightDots();
    }

    [ContextMenu("Auto Collect Level Nodes")]
    public void AutoCollectLevelNodes()
    {
        levelNodes.Clear();
        var slots = GetComponentsInChildren<SortLevelButtonSlot>(true);
        for (int i = 0; i < slots.Length; i++)
        {
            var rt = slots[i] != null ? slots[i].transform as RectTransform : null;
            if (rt != null)
                levelNodes.Add(rt);
        }
        EnsureLinkCurveSize();
    }

    [ContextMenu("Spawn Path Points")]
    public void SpawnPathPoints()
    {
        RectTransform parent = transform as RectTransform;
        if (parent == null)
        {
            Debug.LogWarning("[SortLevelPath] Parent RectTransform tidak valid.");
            return;
        }

        ClearPathPoints();
        int pointIndex = 0;
        for (int i = 0; i < GetLinkCount(); i++)
        {
            if (!TryGetLink(i, out var start, out var end, out var settings))
                continue;

            int sample = ResolveSampleCount(start, end, settings);
            for (int s = 0; s < sample; s++)
            {
                float t = ResolveDotT(settings, s, sample);
                Vector3 world = EvaluateDotWorldPosition(start.position, end.position, settings, t);
                InstantiateDotPair(parent, world, pointIndex++);
            }
        }
    }

    [ContextMenu("Clear Path Points")]
    public void ClearPathPoints()
    {
        RectTransform parent = transform as RectTransform;
        if (parent == null) return;

        var toRemove = new List<GameObject>();
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            if (child != null && child.name.StartsWith(PointNamePrefix, StringComparison.Ordinal))
                toRemove.Add(child.gameObject);
        }

        for (int i = 0; i < toRemove.Count; i++)
        {
            if (Application.isPlaying) Destroy(toRemove[i]);
            else DestroyImmediate(toRemove[i]);
        }
        _runtimeDots.Clear();
        _runtimeBuilt = false;
    }

    private void OnDrawGizmos()
    {
        EnsureLinkCurveSize();
        Gizmos.color = Color.white;
        for (int i = 0; i < GetLinkCount(); i++)
        {
            if (!TryGetLink(i, out var start, out var end, out var settings))
                continue;

            Vector3 prev = EvaluateBezier(start.position, end.position, settings, 0f);
            for (int step = 1; step <= CurveResolution; step++)
            {
                float t = step / (float)CurveResolution;
                Vector3 curr = EvaluateBezier(start.position, end.position, settings, t);
                Gizmos.DrawLine(prev, curr);
                prev = curr;
            }

            if (!drawSampleDots) continue;
            int sample = ResolveSampleCount(start, end, settings);
            float radius = gizmoDotRadius * 0.01f;
            for (int s = 0; s < sample; s++)
            {
                float t = ResolveDotT(settings, s, sample);
                Vector3 point = EvaluateDotWorldPosition(start.position, end.position, settings, t);
                Gizmos.DrawSphere(point, radius);
            }
        }
    }

    private int GetLinkCount() => Mathf.Max(0, levelNodes.Count - 1);

    private void EnsureLinkCurveSize()
    {
        int linkCount = GetLinkCount();
        while (linkCurves.Count < linkCount) linkCurves.Add(new LinkCurveSettings());
        if (linkCurves.Count > linkCount) linkCurves.RemoveRange(linkCount, linkCurves.Count - linkCount);
    }

    private bool TryGetLink(int index, out RectTransform start, out RectTransform end, out LinkCurveSettings settings)
    {
        start = null;
        end = null;
        settings = null;
        if (index < 0 || index >= GetLinkCount()) return false;
        start = levelNodes[index];
        end = levelNodes[index + 1];
        if (start == null || end == null) return false;
        settings = index < linkCurves.Count ? linkCurves[index] : new LinkCurveSettings();
        return true;
    }

    private int ResolveSampleCount(RectTransform start, RectTransform end, LinkCurveSettings settings)
    {
        if (settings != null && settings.overrideAutoSampleCount)
            return Mathf.Max(2, settings.sampleCount);

        float length = EstimateBezierLength(start.position, end.position, settings, 20);
        int raw = Mathf.CeilToInt((length / 100f) * Mathf.Max(0.1f, samplesPer100Units)) + 1;
        int min = Mathf.Max(2, autoSampleMin);
        int max = Mathf.Max(min, autoSampleMax);
        return Mathf.Clamp(raw, min, max);
    }

    private void EnsureRuntimeDotsBuilt()
    {
        if (_runtimeBuilt) return;
        float t0 = Time.realtimeSinceStartup;
        _runtimeBuilt = false;
        _runtimeDots.Clear();

        RectTransform parent = transform as RectTransform;
        if (parent == null) return;

        ClearPathPoints();
        int pointIndex = 0;
        for (int i = 0; i < GetLinkCount(); i++)
        {
            if (!TryGetLink(i, out var start, out var end, out var settings))
                continue;

            int sample = ResolveSampleCount(start, end, settings);
            int order = 0;
            for (int s = 0; s < sample; s++)
            {
                float t = ResolveDotT(settings, s, sample);
                Vector3 world = EvaluateDotWorldPosition(start.position, end.position, settings, t);
                InstantiateDotPair(parent, world, pointIndex, out GameObject onObj, out GameObject offObj);
                if (onObj == null && offObj == null) continue;
                _runtimeDots.Add(new RuntimeDot
                {
                    LinkIndex = i,
                    OrderIndex = order++,
                    OnObject = onObj,
                    OffObject = offObj
                });
                pointIndex++;
            }
        }
        _runtimeBuilt = true;

        float elapsed = Time.realtimeSinceStartup - t0;
        if (UseDebugLog)
        {
            Debug.LogWarning(
                $"[Perf][SortLevelPath] BuildDots links={GetLinkCount()} dots={_runtimeDots.Count} total={elapsed * 1000f:0.0}ms");
        }
    }

    private void UpdateRuntimeLightDots()
    {
        if (!showNodes)
        {
            _animatedLinkIndex = -1;
            SetAllDotsHidden();
            return;
        }

        var manager = SortLevelSelectManager.Instance;
        if (manager == null) return;
        int linkCount = GetLinkCount();
        if (linkCount <= 0 || _runtimeDots.Count == 0) return;

        if (!useWalkAnimation)
        {
            _animatedLinkIndex = -1;
            SetAllDotsOn();
            return;
        }

        LinkLightMode[] modes = new LinkLightMode[linkCount];
        int animatedLink = -1;
        for (int i = 0; i < linkCount; i++)
        {
            int globalStart = manager.GetGlobalLevelIndexForSlot(i);
            int globalEnd = manager.GetGlobalLevelIndexForSlot(i + 1);
            if (globalStart < 0 || globalEnd < 0) { modes[i] = LinkLightMode.Hidden; continue; }

            LevelSlotState startState = manager.GetSlotState(globalStart);
            LevelSlotState endState = manager.GetSlotState(globalEnd);
            if (endState == LevelSlotState.Completed) { modes[i] = LinkLightMode.On; continue; }
            if (endState == LevelSlotState.Available && startState == LevelSlotState.Completed)
            {
                modes[i] = LinkLightMode.Animate;
                animatedLink = i;
                continue;
            }
            modes[i] = LinkLightMode.Hidden;
        }

        if (_animatedLinkIndex != animatedLink)
        {
            _animatedLinkIndex = animatedLink;
            _animatedDotCursor = 0;
            _animatedTimer = 0f;
            _animateWaitingDelay = false;
        }
        if (_animatedLinkIndex >= 0) StepAnimatedCursor();
        ApplyDotModes(modes);
    }

    private void StepAnimatedCursor()
    {
        _animatedTimer += Time.unscaledDeltaTime;
        float wait = _animateWaitingDelay ? AnimateLoopDelaySeconds : AnimateStepSeconds;
        if (_animatedTimer < Mathf.Max(0.01f, wait)) return;
        _animatedTimer = 0f;

        int dotCount = CountDotsInLink(_animatedLinkIndex);
        if (dotCount <= 0) return;
        if (_animateWaitingDelay)
        {
            _animateWaitingDelay = false;
            _animatedDotCursor = 0;
            return;
        }
        _animatedDotCursor++;
        if (_animatedDotCursor > dotCount) _animateWaitingDelay = true;
    }

    private int CountDotsInLink(int linkIndex)
    {
        int n = 0;
        for (int i = 0; i < _runtimeDots.Count; i++)
            if (_runtimeDots[i].LinkIndex == linkIndex) n++;
        return n;
    }

    private void ApplyDotModes(LinkLightMode[] modes)
    {
        for (int i = 0; i < _runtimeDots.Count; i++)
        {
            RuntimeDot dot = _runtimeDots[i];
            if (dot == null) continue;
            if (dot.LinkIndex < 0 || dot.LinkIndex >= modes.Length) { SetDotHidden(dot); continue; }

            switch (modes[dot.LinkIndex])
            {
                case LinkLightMode.On:
                    SetDotLight(dot, true);
                    break;
                case LinkLightMode.Animate:
                    SetDotLight(dot, dot.OrderIndex < _animatedDotCursor);
                    break;
                default:
                    SetDotHidden(dot);
                    break;
            }
        }
    }

    private void SetAllDotsOn()
    {
        for (int i = 0; i < _runtimeDots.Count; i++)
            SetDotLight(_runtimeDots[i], true);
    }

    private void SetAllDotsHidden()
    {
        for (int i = 0; i < _runtimeDots.Count; i++)
            SetDotHidden(_runtimeDots[i]);
    }

    private static void SetDotLight(RuntimeDot dot, bool on)
    {
        if (dot == null) return;
        if (dot.OnObject != null) dot.OnObject.SetActive(on);
        if (dot.OffObject != null) dot.OffObject.SetActive(!on);
    }

    private static void SetDotHidden(RuntimeDot dot)
    {
        if (dot == null) return;
        if (dot.OnObject != null) dot.OnObject.SetActive(false);
        if (dot.OffObject != null) dot.OffObject.SetActive(false);
    }

    private float EstimateBezierLength(Vector3 startWorld, Vector3 endWorld, LinkCurveSettings settings, int steps)
    {
        int safeSteps = Mathf.Max(4, steps);
        float total = 0f;
        Vector3 prev = EvaluateBezier(startWorld, endWorld, settings, 0f);
        for (int i = 1; i <= safeSteps; i++)
        {
            float t = i / (float)safeSteps;
            Vector3 curr = EvaluateBezier(startWorld, endWorld, settings, t);
            total += Vector3.Distance(prev, curr);
            prev = curr;
        }
        return total;
    }

    private static float ResolveDotT(LinkCurveSettings settings, int index, int sampleCount)
    {
        float baseT = sampleCount <= 1 ? 0f : index / (float)(sampleCount - 1);
        if (settings == null) return Mathf.Clamp01(baseT);
        float a = Mathf.Clamp01(settings.dotTStart);
        float b = Mathf.Clamp01(settings.dotTEnd);
        return Mathf.Lerp(a, b, baseT);
    }

    private Vector3 EvaluateDotWorldPosition(Vector3 startWorld, Vector3 endWorld, LinkCurveSettings settings, float t)
    {
        Vector3 world = EvaluateBezier(startWorld, endWorld, settings, t);
        if (settings == null || settings.dotOffset == Vector2.zero)
            return world;

        RectTransform space = transform as RectTransform;
        if (space == null)
            return world + (Vector3)settings.dotOffset;

        Vector3 worldOffset = space.TransformVector((Vector3)settings.dotOffset);
        return world + worldOffset;
    }

    private Vector3 EvaluateBezier(Vector3 startWorld, Vector3 endWorld, LinkCurveSettings settings, float t)
    {
        RectTransform space = transform as RectTransform;
        if (space == null)
            return CubicBezier(startWorld, startWorld, endWorld, endWorld, t);

        Vector3 startLocal = space.InverseTransformPoint(startWorld);
        Vector3 endLocal = space.InverseTransformPoint(endWorld);
        Vector3 c1Local = startLocal + (Vector3)settings.startHandleOffset;
        Vector3 c2Local = endLocal + (Vector3)settings.endHandleOffset;

        Vector3 p0 = space.TransformPoint(startLocal);
        Vector3 p1 = space.TransformPoint(c1Local);
        Vector3 p2 = space.TransformPoint(c2Local);
        Vector3 p3 = space.TransformPoint(endLocal);
        return CubicBezier(p0, p1, p2, p3, Mathf.Clamp01(t));
    }

    private static Vector3 CubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float u = 1f - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;
        return (uuu * p0) + (3f * uu * t * p1) + (3f * u * tt * p2) + (ttt * p3);
    }

    private void InstantiateDotPair(RectTransform parent, Vector3 worldPosition, int index)
    {
        InstantiateDotPair(parent, worldPosition, index, out _, out _);
    }

    private void InstantiateDotPair(RectTransform parent, Vector3 worldPosition, int index, out GameObject onObj, out GameObject offObj)
    {
        onObj = InstantiateDotObject(dotLightOnPrefab, parent, PointNamePrefix + index.ToString("000") + "_On");
        offObj = InstantiateDotObject(dotLightOffPrefab, parent, PointNamePrefix + index.ToString("000") + "_Off");

        if (onObj == null && offObj == null)
        {
            // Fallback kalau kedua prefab belum diisi.
            offObj = new GameObject("DotOff", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            offObj.transform.SetParent(parent, false);
            RectTransform rt = offObj.GetComponent<RectTransform>();
            if (rt != null) rt.sizeDelta = FallbackDotSize;
            Image img = offObj.GetComponent<Image>();
            if (img != null) img.raycastTarget = false;
        }

        if (onObj != null)
        {
            onObj.transform.position = worldPosition;
            onObj.SetActive(false);
        }
        if (offObj != null)
        {
            offObj.transform.position = worldPosition;
            offObj.SetActive(true);
        }
    }

    private GameObject InstantiateDotObject(GameObject prefab, RectTransform parent, string name)
    {
        if (prefab == null) return null;
        GameObject obj;
#if UNITY_EDITOR
        if (!Application.isPlaying)
            obj = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
        else
            obj = Instantiate(prefab, parent);
#else
        obj = Instantiate(prefab, parent);
#endif
        if (obj != null) obj.name = name;
        return obj;
    }
}
