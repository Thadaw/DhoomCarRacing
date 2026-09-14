using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class TrackData : MonoBehaviour
{
    public static TrackData Instance { get; private set; }

    [Header("Sampling")]
    public float sampleSpacing = 2f;
    public float roadHalfWidth = 6f;
    public float boundaryRayHeight = 1f;
    public float boundaryRayDist = 15f;
    public LayerMask roadLayerMask = ~0;

    [Header("Debug")]
    public bool drawGizmos = true;
    public Color centerColor = Color.white;
    public Color leftColor = Color.cyan;
    public Color rightColor = Color.magenta;

    public struct TrackPoint
    {
        public Vector3 center;
        public Vector3 left;
        public Vector3 right;
        public Vector3 tangent;
        public Vector3 rightVector;
        public float curvature;
        public float distanceAlongTrack;
    }

    public List<TrackPoint> points = new List<TrackPoint>();
    public float totalLength { get; private set; }

    private bool built = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Build();
    }

    public void Build()
    {
        if (built) return;
        points.Clear();

        List<Vector3> controlPoints = GatherControlPoints();
        if (controlPoints.Count < 3)
        {
            Debug.LogError("TrackData: Not enough control points (" + controlPoints.Count + ")");
            return;
        }

        // Determine road width from generator if available
        if (SimpleTrackGenerator.LastGeneratedPath != null && SimpleTrackGenerator.LastGeneratedPath.Count > 0)
        {
            DetectRoadWidthFromScene();
        }

        // If we got exact path points from the generator, use them directly
        // (no interpolation needed — they ARE the road centerline)
        // If we reconstructed from Road_Parts or checkpoints, smooth via Catmull-Rom
        bool isDirectPath = (SimpleTrackGenerator.LastGeneratedPath != null
            && SimpleTrackGenerator.LastGeneratedPath.Count > 0
            && controlPoints.Count == SimpleTrackGenerator.LastGeneratedPath.Count);

        List<Vector3> centerline;
        if (isDirectPath)
        {
            centerline = new List<Vector3>(controlPoints);
        }
        else
        {
            centerline = BuildCenterline(controlPoints);
        }

        ComputeBoundaryAndCurvature(centerline);
        built = true;

        Debug.Log($"TrackData: Built {points.Count} track points, total length={totalLength:F0}m, roadHalfWidth={roadHalfWidth:F1}m");
    }

    private void DetectRoadWidthFromScene()
    {
        // Try to read road width from SimpleTrackGenerator if available
        SimpleTrackGenerator gen = FindFirstObjectByType<SimpleTrackGenerator>();
        if (gen != null)
        {
            roadHalfWidth = gen.roadWidth * 0.5f;
            return;
        }

        // Fallback: detect from Road_Part bounds
        GameObject[] allRoadParts = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        float widthSum = 0f;
        int widthCount = 0;
        foreach (GameObject obj in allRoadParts)
        {
            if (!obj.name.StartsWith("Road_Part")) continue;
            Renderer r = obj.GetComponentInChildren<Renderer>();
            if (r == null) continue;
            Bounds b = r.bounds;
            float w = Mathf.Max(b.size.x, b.size.z);
            if (w > 2f && w < 50f)
            {
                widthSum += w;
                widthCount++;
            }
        }
        if (widthCount > 0)
        {
            roadHalfWidth = (widthSum / widthCount) * 0.5f;
        }
    }

    private List<Vector3> GatherControlPoints()
    {
        // Priority 1: Direct path from SimpleTrackGenerator (most accurate)
        if (SimpleTrackGenerator.LastGeneratedPath != null && SimpleTrackGenerator.LastGeneratedPath.Count >= 3)
        {
            Debug.Log($"TrackData: Using {SimpleTrackGenerator.LastGeneratedPath.Count} direct path points from SimpleTrackGenerator");
            return new List<Vector3>(SimpleTrackGenerator.LastGeneratedPath);
        }

        // Priority 2: Road_Part transform positions (midpoints of road segments)
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        List<KeyValuePair<int, GameObject>> roadParts = new List<KeyValuePair<int, GameObject>>();
        foreach (GameObject obj in allObjects)
        {
            if (!obj.name.StartsWith("Road_Part")) continue;
            Match m = System.Text.RegularExpressions.Regex.Match(obj.name, @"\d+");
            int idx = m.Success ? int.Parse(m.Value) : 0;
            roadParts.Add(new KeyValuePair<int, GameObject>(idx, obj));
        }
        roadParts.Sort((a, b) => a.Key.CompareTo(b.Key));

        if (roadParts.Count >= 3)
        {
            List<Vector3> pts = new List<Vector3>();
            foreach (var rp in roadParts)
            {
                Vector3 pos = rp.Value.transform.position;
                pos.y = 0f;
                pts.Add(pos);
            }
            Debug.Log($"TrackData: Using {pts.Count} Road_Part transform positions");
            return pts;
        }

        // Priority 3: Checkpoints
        Debug.LogWarning("TrackData: Falling back to checkpoints");
        return GatherCheckpoints();
    }

    private List<Vector3> GatherCheckpoints()
    {
        RaceCheckpoint[] allCPs = FindObjectsByType<RaceCheckpoint>(FindObjectsSortMode.None);
        List<RaceCheckpoint> sorted = new List<RaceCheckpoint>();
        foreach (RaceCheckpoint cp in allCPs)
        {
            if (!cp.isFinishLine) sorted.Add(cp);
        }
        sorted.Sort((a, b) => a.checkpointIndex.CompareTo(b.checkpointIndex));

        List<Vector3> cpPts = new List<Vector3>();
        foreach (RaceCheckpoint cp in sorted)
            cpPts.Add(cp.transform.position);

        return cpPts;
    }

    private List<Vector3> BuildCenterline(List<Vector3> controlPoints)
    {
        List<Vector3> centerline = new List<Vector3>();
        int n = controlPoints.Count;

        for (int i = 0; i < n; i++)
        {
            Vector3 p0 = controlPoints[(i - 1 + n) % n];
            Vector3 p1 = controlPoints[i];
            Vector3 p2 = controlPoints[(i + 1) % n];
            Vector3 p3 = controlPoints[(i + 2) % n];

            float segLen = Vector3.Distance(p1, p2);
            int steps = Mathf.Max(2, Mathf.CeilToInt(segLen / Mathf.Max(0.5f, sampleSpacing)));

            for (int s = 0; s < steps; s++)
            {
                float t = (float)s / steps;
                centerline.Add(CatmullRomCentripetal(p0, p1, p2, p3, t));
            }
        }

        // Ensure the centerline closes back to the start for a perfect loop
        if (centerline.Count > 1)
        {
            float closureDist = Vector3.Distance(centerline[centerline.Count - 1], centerline[0]);
            if (closureDist > 0.1f)
            {
                centerline.Add(centerline[0]);
            }
        }

        return centerline;
    }

    private void ComputeBoundaryAndCurvature(List<Vector3> centerline)
    {
        int n = centerline.Count;
        if (n < 3) return;

        // Ensure loop closure: if first and last points are > 0.1m apart, close it
        if (Vector3.Distance(centerline[0], centerline[n - 1]) > 0.1f)
        {
            centerline.Add(centerline[0]);
            n = centerline.Count;
        }

        float[] accumDist = new float[n];
        Vector3[] tangents = new Vector3[n];
        Vector3[] rights = new Vector3[n];

        // Central-difference tangent: (next - prev) / 2
        // For a closed loop, this is naturally correct because of modulo indexing
        for (int i = 0; i < n; i++)
        {
            Vector3 prev = centerline[(i - 1 + n) % n];
            Vector3 next = centerline[(i + 1) % n];
            Vector3 t = (next - prev);
            t.y = 0f;

            if (t.sqrMagnitude < 0.0001f)
            {
                // Degenerate: use forward difference as fallback
                int fwd = (i + 1) % n;
                t = (centerline[fwd] - centerline[i]);
                t.y = 0f;
            }

            tangents[i] = t.sqrMagnitude > 0.0001f ? t.normalized : Vector3.forward;
            rights[i] = Vector3.Cross(Vector3.up, tangents[i]).normalized;
        }

        // Accumulate distance along track
        for (int i = 1; i < n; i++)
            accumDist[i] = accumDist[i - 1] + Vector3.Distance(centerline[i - 1], centerline[i]);

        totalLength = accumDist[n - 1] + Vector3.Distance(centerline[n - 1], centerline[0]);

        // Curvature: signed angle between consecutive direction vectors
        float[] curvatures = new float[n];
        for (int i = 0; i < n; i++)
        {
            int prev = (i - 1 + n) % n;
            int next = (i + 1) % n;

            Vector3 v1 = (centerline[i] - centerline[prev]).normalized;
            Vector3 v2 = (centerline[next] - centerline[i]).normalized;

            float angle = Vector3.Angle(v1, v2);
            float cross = Vector3.Cross(v1, v2).y;
            curvatures[i] = (cross > 0 ? angle : -angle) * Mathf.Deg2Rad;
        }

        points.Clear();
        for (int i = 0; i < n; i++)
        {
            // Simple perpendicular offset from centerline — more reliable than raycasts
            // that can hit terrain/buildings instead of road edges
            Vector3 leftPt = centerline[i] - rights[i] * roadHalfWidth;
            Vector3 rightPt = centerline[i] + rights[i] * roadHalfWidth;

            leftPt.y = centerline[i].y;
            rightPt.y = centerline[i].y;

            points.Add(new TrackPoint
            {
                center = centerline[i],
                left = leftPt,
                right = rightPt,
                tangent = tangents[i],
                rightVector = rights[i],
                curvature = curvatures[i],
                distanceAlongTrack = accumDist[i]
            });
        }
    }

    public int FindNearestIndex(Vector3 pos)
    {
        float bestDist = float.MaxValue;
        int bestIdx = 0;
        for (int i = 0; i < points.Count; i++)
        {
            float d = Vector3.Distance(pos, points[i].center);
            if (d < bestDist) { bestDist = d; bestIdx = i; }
        }
        return bestIdx;
    }

    public int FindForwardIndex(int fromIdx, float distance)
    {
        int idx = fromIdx;
        float accum = 0f;
        int n = points.Count;
        for (int i = 0; i < n; i++)
        {
            int next = (idx + 1) % n;
            accum += Vector3.Distance(points[idx].center, points[next].center);
            idx = next;
            if (accum >= distance) break;
        }
        return idx;
    }

    public float DistanceAlongTrack(Vector3 pos)
    {
        int idx = FindNearestIndex(pos);
        return points[idx].distanceAlongTrack;
    }

    private static Vector3 CatmullRomCentripetal(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        const float alpha = 0.5f;
        const float minDist = 0.01f;

        float t0 = 0f;
        float t1 = t0 + Mathf.Pow(Mathf.Max(Vector3.Distance(p0, p1), minDist), alpha);
        float t2 = t1 + Mathf.Pow(Mathf.Max(Vector3.Distance(p1, p2), minDist), alpha);
        float t3 = t2 + Mathf.Pow(Mathf.Max(Vector3.Distance(p2, p3), minDist), alpha);

        float u = Mathf.Lerp(t1, t2, t);

        Vector3 A1 = SafeLerp(p0, p1, t0, t1, u);
        Vector3 A2 = SafeLerp(p1, p2, t1, t2, u);
        Vector3 A3 = SafeLerp(p2, p3, t2, t3, u);

        Vector3 B1 = SafeLerp(A1, A2, t0, t2, u);
        Vector3 B2 = SafeLerp(A2, A3, t1, t3, u);

        return SafeLerp(B1, B2, t1, t2, u);
    }

    private static Vector3 SafeLerp(Vector3 a, Vector3 b, float paramA, float paramB, float u)
    {
        float denom = paramB - paramA;
        if (Mathf.Abs(denom) < 0.0001f) return a;
        float frac = Mathf.Clamp01((u - paramA) / denom);
        return Vector3.Lerp(a, b, frac);
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos || points.Count == 0) return;

        for (int i = 0; i < points.Count; i++)
        {
            Gizmos.color = centerColor;
            Gizmos.DrawWireSphere(points[i].center, 0.5f);

            Gizmos.color = leftColor;
            Gizmos.DrawWireSphere(points[i].left, 0.3f);

            Gizmos.color = rightColor;
            Gizmos.DrawWireSphere(points[i].right, 0.3f);

            int next = (i + 1) % points.Count;
            Gizmos.color = centerColor;
            Gizmos.DrawLine(points[i].center, points[next].center);
            Gizmos.color = leftColor;
            Gizmos.DrawLine(points[i].left, points[next].left);
            Gizmos.color = rightColor;
            Gizmos.DrawLine(points[i].right, points[next].right);
        }
    }
}
