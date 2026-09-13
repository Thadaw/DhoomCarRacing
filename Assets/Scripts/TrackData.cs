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

        List<Vector3> centerline = BuildCenterline(controlPoints);
        ComputeBoundaryAndCurvature(centerline);
        built = true;

        Debug.Log($"TrackData: Built {points.Count} track points, total length={totalLength:F0}m");
    }

    private List<Vector3> GatherControlPoints()
    {
        // Find all Road_Part objects and sort by index
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        List<KeyValuePair<int, GameObject>> roadParts = new List<KeyValuePair<int, GameObject>>();
        foreach (GameObject obj in allObjects)
        {
            if (!obj.name.StartsWith("Road_Part")) continue;
            Match m = Regex.Match(obj.name, @"\d+");
            int idx = m.Success ? int.Parse(m.Value) : 0;
            roadParts.Add(new KeyValuePair<int, GameObject>(idx, obj));
        }
        roadParts.Sort((a, b) => a.Key.CompareTo(b.Key));

        if (roadParts.Count < 3)
        {
            Debug.LogWarning($"TrackData: Only {roadParts.Count} Road_Parts found, falling back to checkpoints");
            return GatherCheckpoints();
        }

        // Sample multiple points along each Road_Part's bounds to get dense path
        // We can't access mesh vertices (isReadable=false), so use bounds extents
        List<Vector3> allPts = new List<Vector3>();
        int pointsPerPart = 10;

        foreach (var rp in roadParts)
        {
            Renderer r = rp.Value.GetComponentInChildren<Renderer>();
            if (r == null) continue;

            Bounds b = r.bounds;
            Vector3 size = b.size;

            // Determine the road direction axis (longest extent)
            int forwardAxis = size.x >= size.z ? 0 : 2;

            float minCoord = forwardAxis == 0 ? b.min.x : b.min.z;
            float maxCoord = forwardAxis == 0 ? b.max.x : b.max.z;

            for (int s = 0; s < pointsPerPart; s++)
            {
                float t = (float)s / (pointsPerPart - 1);
                float coord = Mathf.Lerp(minCoord, maxCoord, t);

                // Cross-section center: use bounds center for the other axes
                float x = forwardAxis == 0 ? coord : b.center.x;
                float z = forwardAxis == 2 ? coord : b.center.z;
                float y = b.center.y;

                allPts.Add(new Vector3(x, y, z));
            }
        }

        // Remove duplicate / very close points
        List<Vector3> cleaned = new List<Vector3>();
        foreach (Vector3 p in allPts)
        {
            if (cleaned.Count == 0 || Vector3.Distance(p, cleaned[cleaned.Count - 1]) > 0.5f)
                cleaned.Add(p);
        }

        Debug.Log($"TrackData: Sampled {cleaned.Count} control points from {roadParts.Count} Road_Parts (bounds-based)");
        return cleaned;
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

        return centerline;
    }

    private void ComputeBoundaryAndCurvature(List<Vector3> centerline)
    {
        int n = centerline.Count;
        if (n < 3) return;

        float[] accumDist = new float[n];
        Vector3[] tangents = new Vector3[n];
        Vector3[] rights = new Vector3[n];

        for (int i = 0; i < n; i++)
        {
            Vector3 prev = centerline[(i - 1 + n) % n];
            Vector3 next = centerline[(i + 1) % n];
            Vector3 t = (next - prev).normalized;
            t.y = 0;
            tangents[i] = t.sqrMagnitude > 0.001f ? t : Vector3.forward;
            rights[i] = Vector3.Cross(Vector3.up, tangents[i]).normalized;
        }

        for (int i = 1; i < n; i++)
            accumDist[i] = accumDist[i - 1] + Vector3.Distance(centerline[i - 1], centerline[i]);

        totalLength = accumDist[n - 1] + Vector3.Distance(centerline[n - 1], centerline[0]);

        float[] curvatures = new float[n];
        for (int i = 0; i < n; i++)
        {
            int i0 = (i - 1 + n) % n;
            int i1 = i;
            int i2 = (i + 1) % n;

            Vector3 v1 = (centerline[i1] - centerline[i0]).normalized;
            Vector3 v2 = (centerline[i2] - centerline[i1]).normalized;

            float angle = Vector3.Angle(v1, v2);
            float cross = Vector3.Cross(v1, v2).y;
            curvatures[i] = (cross > 0 ? angle : -angle) * Mathf.Deg2Rad;
        }

        // Estimate road width from Road_Part mesh bounds
        float estimatedHalfWidth = roadHalfWidth;
        GameObject[] allRoadParts = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        float widthSum = 0f;
        int widthCount = 0;
        foreach (GameObject obj in allRoadParts)
        {
            if (!obj.name.StartsWith("Road_Part")) continue;
            Renderer r = obj.GetComponentInChildren<Renderer>();
            if (r == null) continue;
            Bounds b = r.bounds;
            // The width of the road piece is the smaller dimension of its bounds
            float w = Mathf.Min(b.size.x, b.size.z);
            if (w > 2f && w < 30f)
            {
                widthSum += w;
                widthCount++;
            }
        }
        if (widthCount > 0)
        {
            estimatedHalfWidth = (widthSum / widthCount) * 0.5f;
            Debug.Log($"TrackData: Estimated road half-width = {estimatedHalfWidth:F1}m from {widthCount} Road_Parts");
        }

        points.Clear();
        for (int i = 0; i < n; i++)
        {
            // Simple perpendicular offset from centerline — more reliable than raycasts
            // that can hit terrain/buildings instead of road edges
            Vector3 leftPt = centerline[i] - rights[i] * estimatedHalfWidth;
            Vector3 rightPt = centerline[i] + rights[i] * estimatedHalfWidth;

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
