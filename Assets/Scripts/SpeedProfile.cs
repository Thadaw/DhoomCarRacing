using UnityEngine;
using System.Collections.Generic;

public class SpeedProfile : MonoBehaviour
{
    public static SpeedProfile Instance { get; private set; }

    [Header("Speed Settings")]
    public float maxLateralAccel = 8f;
    public float minCornerSpeed = 15f;
    public float brakingLookAhead = 80f;
    public float cornerEntryBlend = 0.7f;

    public List<float> maxSpeeds { get; private set; } = new List<float>();
    private bool built = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void Build(float aiMaxSpeed)
    {
        if (built) return;
        if (TrackData.Instance == null || TrackData.Instance.points.Count == 0)
        {
            Debug.LogError("SpeedProfile: TrackData not available");
            return;
        }

        var pts = TrackData.Instance.points;
        int n = pts.Count;

        maxSpeeds.Clear();
        maxSpeeds.Capacity = n;

        for (int i = 0; i < n; i++)
        {
            float absCurv = Mathf.Abs(pts[i].curvature);

            if (absCurv < 0.005f)
            {
                maxSpeeds.Add(aiMaxSpeed);
                continue;
            }

            float radius = 1f / absCurv;
            float safeSpeedMs = Mathf.Sqrt(Mathf.Max(1f, maxLateralAccel * radius));
            float safeSpeedKmh = safeSpeedMs * 3.6f;
            safeSpeedKmh *= cornerEntryBlend;
            maxSpeeds.Add(Mathf.Clamp(safeSpeedKmh, minCornerSpeed, aiMaxSpeed));
        }

        for (int i = 0; i < n; i++)
        {
            if (maxSpeeds[i] >= aiMaxSpeed * 0.95f) continue;

            float cornerSpeed = maxSpeeds[i];
            float decelDist = 0f;

            int j = i;
            while (decelDist < brakingLookAhead)
            {
                int prev = (j - 1 + n) % n;
                decelDist += Vector3.Distance(pts[j].center, pts[prev].center);
                j = prev;

                if (maxSpeeds[j] > cornerSpeed)
                {
                    float blendT = Mathf.Clamp01(decelDist / brakingLookAhead);
                    float targetSpeed = Mathf.Lerp(cornerSpeed, maxSpeeds[j], blendT);
                    maxSpeeds[j] = Mathf.Min(maxSpeeds[j], targetSpeed);
                }
            }
        }

        built = true;
        Debug.Log($"SpeedProfile: Built {maxSpeeds.Count} speed entries, minSpeed={minCornerSpeed:F0}, maxSpeed={aiMaxSpeed:F0}");
    }

    public float GetMaxSpeed(int trackIndex)
    {
        if (maxSpeeds.Count == 0) return 200f;
        return maxSpeeds[trackIndex % maxSpeeds.Count];
    }
}
