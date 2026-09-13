using UnityEngine;
using System.Collections.Generic;

public class RacingLine : MonoBehaviour
{
    public static RacingLine Instance { get; private set; }

    [Header("Racing Line Settings")]
    [Range(0f, 1f)]
    public float apexOffset = 0.85f;
    [Range(0f, 1f)]
    public float entryExitOffset = 0.6f;
    public float curvatureLookAhead = 50f;
    public float curvatureLookBehind = 30f;
    public float smoothingPasses = 3;

    public struct RacingLinePoint
    {
        public Vector3 position;
        public float lateralOffset;
    }

    public List<RacingLinePoint> line { get; private set; } = new List<RacingLinePoint>();
    private bool built = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void Build()
    {
        if (built) return;
        if (TrackData.Instance == null || TrackData.Instance.points.Count == 0)
        {
            Debug.LogError("RacingLine: TrackData not available");
            return;
        }

        var trackPts = TrackData.Instance.points;
        int n = trackPts.Count;

        float[] rawOffset = new float[n];

        for (int i = 0; i < n; i++)
        {
            float absCurv = Mathf.Abs(trackPts[i].curvature);

            float maxCurvAhead = 0f;
            float maxCurvBehind = 0f;
            float sumCurvAhead = 0f;
            float sumCurvBehind = 0f;
            int countAhead = 0;
            int countBehind = 0;

            float accumAhead = 0f;
            float accumBehind = 0f;
            int j;

            j = i;
            while (accumAhead < curvatureLookAhead)
            {
                j = (j + 1) % n;
                float segDist = Vector3.Distance(trackPts[j].center, trackPts[(j - 1 + n) % n].center);
                accumAhead += segDist;
                float c = Mathf.Abs(trackPts[j].curvature);
                sumCurvAhead += c;
                countAhead++;
                if (c > maxCurvAhead) maxCurvAhead = c;
            }

            j = i;
            while (accumBehind < curvatureLookBehind)
            {
                j = (j - 1 + n) % n;
                float segDist = Vector3.Distance(trackPts[j].center, trackPts[(j + 1) % n].center);
                accumBehind += segDist;
                float c = Mathf.Abs(trackPts[j].curvature);
                sumCurvBehind += c;
                countBehind++;
                if (c > maxCurvBehind) maxCurvBehind = c;
            }

            float avgAhead = countAhead > 0 ? sumCurvAhead / countAhead : 0f;
            float avgBehind = countBehind > 0 ? sumCurvBehind / countBehind : 0f;

            float offset = 0f;

            if (absCurv > 0.03f)
            {
                bool isLeftTurn = trackPts[i].curvature > 0f;
                offset = isLeftTurn ? -apexOffset : apexOffset;
            }
            else if (maxCurvAhead > 0.05f && avgAhead > avgBehind * 1.2f)
            {
                bool upcomingLeftTurn = FindUpcomingTurnDirection(trackPts, i, curvatureLookAhead);
                offset = upcomingLeftTurn ? entryExitOffset : -entryExitOffset;
            }
            else if (maxCurvBehind > 0.05f && avgBehind > avgAhead * 1.2f)
            {
                bool prevTurnLeft = FindPrevTurnDirection(trackPts, i, curvatureLookBehind);
                offset = prevTurnLeft ? entryExitOffset : -entryExitOffset;
            }

            rawOffset[i] = offset;
        }

        for (int pass = 0; pass < (int)smoothingPasses; pass++)
        {
            float[] smoothed = new float[n];
            for (int i = 0; i < n; i++)
            {
                int prev = (i - 1 + n) % n;
                int next = (i + 1) % n;
                smoothed[i] = (rawOffset[prev] * 0.25f + rawOffset[i] * 0.5f + rawOffset[next] * 0.25f);
            }
            rawOffset = smoothed;
        }

        line.Clear();
        for (int i = 0; i < n; i++)
        {
            float maxLateral = Vector3.Distance(trackPts[i].left, trackPts[i].right) * 0.5f;
            float clampedOffset = Mathf.Clamp(rawOffset[i], -1f, 1f);
            float worldOffset = clampedOffset * maxLateral * 0.9f;

            Vector3 pos = trackPts[i].center + trackPts[i].rightVector * worldOffset;
            pos.y = trackPts[i].center.y;

            line.Add(new RacingLinePoint
            {
                position = pos,
                lateralOffset = clampedOffset
            });
        }

        built = true;
        Debug.Log($"RacingLine: Built {line.Count} racing line points");
    }

    private bool FindUpcomingTurnDirection(List<TrackData.TrackPoint> pts, int fromIdx, float lookAheadDist)
    {
        int n = pts.Count;
        int idx = fromIdx;
        float accum = 0f;

        while (accum < lookAheadDist)
        {
            int next = (idx + 1) % n;
            accum += Vector3.Distance(pts[idx].center, pts[next].center);
            idx = next;
            if (Mathf.Abs(pts[idx].curvature) > 0.05f)
                return pts[idx].curvature > 0f;
        }
        return false;
    }

    private bool FindPrevTurnDirection(List<TrackData.TrackPoint> pts, int fromIdx, float lookBehindDist)
    {
        int n = pts.Count;
        int idx = fromIdx;
        float accum = 0f;

        while (accum < lookBehindDist)
        {
            int prev = (idx - 1 + n) % n;
            accum += Vector3.Distance(pts[idx].center, pts[prev].center);
            idx = prev;
            if (Mathf.Abs(pts[idx].curvature) > 0.05f)
                return pts[idx].curvature > 0f;
        }
        return false;
    }

    public int FindNearestIndex(Vector3 pos)
    {
        if (line.Count == 0) return 0;
        float bestDist = float.MaxValue;
        int bestIdx = 0;
        for (int i = 0; i < line.Count; i++)
        {
            float d = Vector3.Distance(pos, line[i].position);
            if (d < bestDist) { bestDist = d; bestIdx = i; }
        }
        return bestIdx;
    }

    public int GetForwardIndex(int fromIdx, float distance)
    {
        if (line.Count == 0) return 0;
        int idx = fromIdx;
        float accum = 0f;
        int n = line.Count;
        for (int i = 0; i < n; i++)
        {
            int next = (idx + 1) % n;
            accum += Vector3.Distance(line[idx].position, line[next].position);
            idx = next;
            if (accum >= distance) break;
        }
        return idx;
    }
}
