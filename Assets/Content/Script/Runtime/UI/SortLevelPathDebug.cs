using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class SortLevelPathDebug : MonoBehaviour
{
    [Serializable]
    public class LinkCurveSettings
    {
        [Tooltip("Offset handle dari start node (dalam koordinat reference space).")]
        public Vector2 startHandleOffset = new Vector2(120f, 0f);

        [Tooltip("Offset handle dari end node (dalam koordinat reference space).")]
        public Vector2 endHandleOffset = new Vector2(-120f, 0f);

        [Tooltip("Kalau aktif, pakai sample count manual untuk link ini.")]
        public bool overrideAutoSampleCount;

        [Min(2)]
        public int sampleCount = 14;

        public bool useCustomColor;
        public Color color = Color.white;
    }

    [Header("Nodes (urut level)")]
    [SerializeField] private List<RectTransform> levelNodes = new List<RectTransform>();

    [Header("Curve Space")]
    [SerializeField] private RectTransform referenceSpace;
    [SerializeField] private Color defaultGizmoColor = new Color(1f, 1f, 1f, 0.9f);
    [SerializeField] private List<LinkCurveSettings> linkCurves = new List<LinkCurveSettings>();

    [Header("Auto Sample By Length")]
    [SerializeField] [Min(0.1f)] private float samplesPer100Units = 8f;
    [SerializeField] [Min(2)] private int autoSampleMin = 8;
    [SerializeField] [Min(2)] private int autoSampleMax = 80;

    [Header("Gizmo")]
    [SerializeField] private bool drawSampleDots = true;
    [SerializeField] [Min(0.001f)] private float gizmoDotRadius = 6f;

    [Header("Debug Point Spawn")]
    [SerializeField] private RectTransform debugPointParent;
    [SerializeField] private GameObject debugPointPrefab;
    [SerializeField] private bool clearExistingBeforeSpawn = true;
    [SerializeField] private string debugPointNamePrefix = "PathPoint_";
    [SerializeField] private Vector2 fallbackDotSize = new Vector2(16f, 16f);

    [Header("Runtime Light Dots")]
    [SerializeField] private bool spawnDotsAtRuntime = true;
    [SerializeField] private bool animateAvailablePath = true;
    [SerializeField] [Min(0.01f)] private float animateStepSeconds = 0.06f;
    [SerializeField] [Min(0f)] private float animateLoopDelaySeconds = 0.15f;
    [SerializeField] private Sprite dotLightOnSprite;
    [SerializeField] private Sprite dotLightOffSprite;
    [SerializeField] private Color dotLightOnColor = Color.white;
    [SerializeField] private Color dotLightOffColor = new Color(1f, 1f, 1f, 0.25f);

    private const int CurveResolution = 40;

    private enum LinkLightMode
    {
        Off,
        On,
        Animate
    }

    private sealed class RuntimeDot
    {
        public int LinkIndex;
        public int OrderIndex;
        public Image Image;
    }

    private readonly List<RuntimeDot> _runtimeDots = new List<RuntimeDot>();
    private bool _runtimeBuilt;
    private int _animatedLinkIndex = -1;
    private int _animatedDotCursor;
    private float _animatedTimer;
    private bool _animateWaitingDelay;

    private void OnEnable()
    {
        _runtimeBuilt = false;
        _animatedLinkIndex = -1;
        _animatedDotCursor = 0;
        _animatedTimer = 0f;
        _animateWaitingDelay = false;
    }

    private void OnDisable()
    {
        _runtimeBuilt = false;
        _runtimeDots.Clear();
    }

    private void Update()
    {
        if (!Application.isPlaying) return;
        if (!spawnDotsAtRuntime) return;

        EnsureRuntimeDotsBuilt();
        UpdateRuntimeLightDots();
    }

    private void OnValidate()
    {
        EnsureLinkCurveSize();
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

    [ContextMenu("Spawn Debug Points")]
    public void SpawnDebugPoints()
    {
        RectTransform parent = debugPointParent != null ? debugPointParent : (transform as RectTransform);
        if (parent == null)
        {
            Debug.LogWarning("[SortLevelPathDebug] Parent untuk spawn point tidak valid.");
            return;
        }

        if (clearExistingBeforeSpawn)
            ClearDebugPoints();

        int pointIndex = 0;
        for (int i = 0; i < GetLinkCount(); i++)
        {
            if (!TryGetLink(i, out var start, out var end, out var settings))
                continue;

            int sample = ResolveSampleCount(start, end, settings);
            for (int s = 0; s < sample; s++)
            {
                if (i > 0 && s == 0) continue; // Hindari duplikasi titik sambungan link.
                float t = s / (float)(sample - 1);
                Vector3 world = EvaluateBezier(start.position, end.position, settings, t);
                CreatePointInstance(parent, world, pointIndex++);
            }
        }
    }

    [ContextMenu("Clear Debug Points")]
    public void ClearDebugPoints()
    {
        RectTransform parent = debugPointParent != null ? debugPointParent : (transform as RectTransform);
        if (parent == null) return;

        var toRemove = new List<GameObject>();
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            if (child == null) continue;
            if (child.name.StartsWith(debugPointNamePrefix, StringComparison.Ordinal))
                toRemove.Add(child.gameObject);
        }

        for (int i = 0; i < toRemove.Count; i++)
        {
            if (Application.isPlaying)
                Destroy(toRemove[i]);
            else
                DestroyImmediate(toRemove[i]);
        }
        _runtimeDots.Clear();
        _runtimeBuilt = false;
    }

    private void OnDrawGizmos()
    {
        EnsureLinkCurveSize();
        for (int i = 0; i < GetLinkCount(); i++)
        {
            if (!TryGetLink(i, out var start, out var end, out var settings))
                continue;

            Color color = settings.useCustomColor ? settings.color : defaultGizmoColor;
            Gizmos.color = color;

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
                float t = s / (float)(sample - 1);
                Vector3 point = EvaluateBezier(start.position, end.position, settings, t);
                Gizmos.DrawSphere(point, radius);
            }
        }
    }

    private int GetLinkCount()
    {
        return Mathf.Max(0, levelNodes.Count - 1);
    }

    private void EnsureLinkCurveSize()
    {
        int linkCount = GetLinkCount();
        while (linkCurves.Count < linkCount)
            linkCurves.Add(new LinkCurveSettings());
        if (linkCurves.Count > linkCount)
            linkCurves.RemoveRange(linkCount, linkCurves.Count - linkCount);
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
        settings = index < linkCurves.Count ? linkCurves[index] : null;
        if (settings == null)
            settings = new LinkCurveSettings();
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
        _runtimeBuilt = false;
        _runtimeDots.Clear();

        RectTransform parent = debugPointParent != null ? debugPointParent : (transform as RectTransform);
        if (parent == null) return;

        if (clearExistingBeforeSpawn)
            ClearDebugPoints();

        int pointIndex = 0;
        for (int i = 0; i < GetLinkCount(); i++)
        {
            if (!TryGetLink(i, out var start, out var end, out var settings))
                continue;

            int sample = ResolveSampleCount(start, end, settings);
            int order = 0;
            for (int s = 0; s < sample; s++)
            {
                if (i > 0 && s == 0) continue;
                float t = s / (float)(sample - 1);
                Vector3 world = EvaluateBezier(start.position, end.position, settings, t);
                var go = InstantiatePointObject(parent, world, pointIndex++);
                if (go == null) continue;
                Image img = go.GetComponent<Image>();
                if (img == null) img = go.GetComponentInChildren<Image>();
                _runtimeDots.Add(new RuntimeDot
                {
                    LinkIndex = i,
                    OrderIndex = order++,
                    Image = img
                });
            }
        }
        _runtimeBuilt = true;
    }

    private void UpdateRuntimeLightDots()
    {
        var manager = SortLevelSelectManager.Instance;
        if (manager == null) return;

        int linkCount = GetLinkCount();
        if (linkCount <= 0 || _runtimeDots.Count == 0)
            return;

        LinkLightMode[] modes = new LinkLightMode[linkCount];
        int animatedLink = -1;

        for (int i = 0; i < linkCount; i++)
        {
            int globalStart = manager.GetGlobalLevelIndexForSlot(i);
            int globalEnd = manager.GetGlobalLevelIndexForSlot(i + 1);
            if (globalStart < 0 || globalEnd < 0)
            {
                modes[i] = LinkLightMode.Off;
                continue;
            }

            LevelSlotState startState = manager.GetSlotState(globalStart);
            LevelSlotState endState = manager.GetSlotState(globalEnd);
            if (endState == LevelSlotState.Completed)
            {
                modes[i] = LinkLightMode.On;
                continue;
            }

            if (animateAvailablePath && endState == LevelSlotState.Available && startState == LevelSlotState.Completed)
            {
                modes[i] = LinkLightMode.Animate;
                animatedLink = i;
                continue;
            }

            modes[i] = LinkLightMode.Off;
        }

        if (_animatedLinkIndex != animatedLink)
        {
            _animatedLinkIndex = animatedLink;
            _animatedDotCursor = 0;
            _animatedTimer = 0f;
            _animateWaitingDelay = false;
        }

        if (_animatedLinkIndex >= 0)
            StepAnimatedCursor();

        ApplyDotModes(modes);
    }

    private void StepAnimatedCursor()
    {
        _animatedTimer += Time.unscaledDeltaTime;
        float wait = _animateWaitingDelay ? animateLoopDelaySeconds : animateStepSeconds;
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
        if (_animatedDotCursor > dotCount)
            _animateWaitingDelay = true;
    }

    private int CountDotsInLink(int linkIndex)
    {
        int n = 0;
        for (int i = 0; i < _runtimeDots.Count; i++)
            if (_runtimeDots[i].LinkIndex == linkIndex)
                n++;
        return n;
    }

    private void ApplyDotModes(LinkLightMode[] modes)
    {
        for (int i = 0; i < _runtimeDots.Count; i++)
        {
            RuntimeDot dot = _runtimeDots[i];
            if (dot == null || dot.Image == null) continue;
            if (dot.LinkIndex < 0 || dot.LinkIndex >= modes.Length)
            {
                SetDotLight(dot.Image, false);
                continue;
            }

            switch (modes[dot.LinkIndex])
            {
                case LinkLightMode.On:
                    SetDotLight(dot.Image, true);
                    break;
                case LinkLightMode.Animate:
                    SetDotLight(dot.Image, dot.OrderIndex < _animatedDotCursor);
                    break;
                default:
                    SetDotLight(dot.Image, false);
                    break;
            }
        }
    }

    private void SetDotLight(Image img, bool on)
    {
        if (img == null) return;
        if (on)
        {
            if (dotLightOnSprite != null) img.sprite = dotLightOnSprite;
            img.color = dotLightOnColor;
        }
        else
        {
            if (dotLightOffSprite != null) img.sprite = dotLightOffSprite;
            img.color = dotLightOffColor;
        }
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

    private Vector3 EvaluateBezier(Vector3 startWorld, Vector3 endWorld, LinkCurveSettings settings, float t)
    {
        RectTransform space = referenceSpace != null ? referenceSpace : (transform as RectTransform);
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

    private void CreatePointInstance(RectTransform parent, Vector3 worldPosition, int index)
    {
        GameObject pointObj = InstantiatePointObject(parent, worldPosition, index);
        if (pointObj == null) return;
    }

    private GameObject InstantiatePointObject(RectTransform parent, Vector3 worldPosition, int index)
    {
        GameObject pointObj;
        if (debugPointPrefab != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                pointObj = PrefabUtility.InstantiatePrefab(debugPointPrefab, parent) as GameObject;
            else
                pointObj = Instantiate(debugPointPrefab, parent);
#else
            pointObj = Instantiate(debugPointPrefab, parent);
#endif
        }
        else
        {
            pointObj = new GameObject("Dot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            pointObj.transform.SetParent(parent, false);
            RectTransform rt = pointObj.GetComponent<RectTransform>();
            if (rt != null)
                rt.sizeDelta = fallbackDotSize;
            Image img = pointObj.GetComponent<Image>();
            if (img != null)
            {
                img.raycastTarget = false;
                if (dotLightOffSprite != null) img.sprite = dotLightOffSprite;
                img.color = dotLightOffColor;
            }
        }
        if (pointObj == null) return null;
        pointObj.name = debugPointNamePrefix + index.ToString("000");
        pointObj.transform.position = worldPosition;
        return pointObj;
    }
}
