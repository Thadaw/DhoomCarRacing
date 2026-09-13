using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AIDriver : MonoBehaviour
{
    [Header("AI Racing")]
    public float stuckTimeout = 2f;
    public float reverseTime = 1.5f;
    public float turnTime = 2f;
    public float roadWidth = 6f;

    [Header("Path Following")]
    [Tooltip("Distance in meters between generated points on the smooth path. Smaller = smoother but more memory/CPU.")]
    public float pathSampleSpacing = 4f;
    public float minLookAhead = 6f;
    public float maxLookAhead = 18f;
    public float steerResponsiveness = 8f;

    [Header("Cornering / Speed")]
    [Tooltip("Max lateral acceleration (m/s^2) the AI allows in a corner before it decides to brake.")]
    public float maxLateralAccel = 9f;
    [Tooltip("How far ahead (in seconds of travel) the AI scans for upcoming curves to brake in advance.")]
    public float brakeLookAheadTime = 1.2f;

    [Header("Opponent Avoidance")]
    public float avoidCheckDistance = 14f;
    public float avoidCheckRadius = 2.2f;
    public float avoidSteerStrength = 0.6f;
    public LayerMask carLayerMask = ~0;

    [Header("AI Difficulty")]
    [Range(0.5f, 1f)]
    public float skillLevel = 0.75f;
    public float maxSpeedVariation = 15f;
    public float brakeSkill = 0.7f;

    private PhotonCarController car;
    private Rigidbody rb;
    private PlayerLapTracker tracker;
    private float steer = 0f;
    private bool ready = false;
    private float readyTimer = 0f;

    private enum State { Wait, Drive, Reverse, Turn, Finished }
    private State state = State.Wait;
    private float stateTimer = 0f;
    private Vector3 lastPos;
    private float stuckTime = 0f;
    private int stuckCount = 0;
    private float turnDir = 1f;

    private List<RaceCheckpoint> sortedCheckpoints;
    private int totalCheckpoints = 0;
    private int nextCP = 0;

    // Dense, smooth path generated from checkpoints via Catmull-Rom spline.
    private List<Vector3> path = new List<Vector3>();
    private int pathIndex = 0;

    private float aiMaxSpeed;
    private float aiMotorForce;
    private float aiBrakeForce;
    private int startFrames = 0;

    private const int MIN_CHECKPOINTS_FOR_PATH = 3;

    public void Initialize(string aiName)
    {
        car = GetComponent<PhotonCarController>();
        rb = GetComponent<Rigidbody>();
        tracker = GetComponent<PlayerLapTracker>();

        if (car == null)
        {
            Debug.LogError($"AIDriver on '{gameObject.name}': No PhotonCarController found!");
            return;
        }

        car.useExternalInput = true;

        if (tracker != null)
        {
            tracker.aiName = aiName;
            tracker.totalLaps = GameSession.Instance != null ? GameSession.Instance.TotalLaps : 1;
        }

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        aiMaxSpeed = car.maxSpeed * Mathf.Lerp(0.75f, 0.95f, skillLevel);
        aiMotorForce = car.motorForce * Mathf.Lerp(0.8f, 1.0f, skillLevel);
        aiBrakeForce = car.brakeForce;

        float variation = Random.Range(-maxSpeedVariation, maxSpeedVariation);
        aiMaxSpeed = Mathf.Clamp(aiMaxSpeed + variation, 40f, car.maxSpeed);

        lastPos = transform.position;
        CacheCheckpoints();
        BuildSmoothPath();
        nextCP = tracker != null ? tracker.nextCheckpointIndex : 0;

        FindNearestPathIndex();
        Debug.Log($"AI '{aiName}': checkpoints={totalCheckpoints}, pathPoints={path.Count}, maxSpeed={aiMaxSpeed:F0}, skill={skillLevel}");
    }

    private void CacheCheckpoints()
    {
        RaceCheckpoint[] all = FindObjectsByType<RaceCheckpoint>(FindObjectsSortMode.None);
        sortedCheckpoints = new List<RaceCheckpoint>();

        foreach (RaceCheckpoint cp in all)
        {
            if (!cp.isFinishLine)
                sortedCheckpoints.Add(cp);
        }

        sortedCheckpoints.Sort((a, b) => a.checkpointIndex.CompareTo(b.checkpointIndex));
        totalCheckpoints = sortedCheckpoints.Count;
    }

    // Builds a smooth, continuous path around the track using Catmull-Rom spline
    // interpolation between checkpoints (treated as a closed loop). This makes the
    // AI follow the actual curve of the road instead of straight lines between
    // sparse points, which is what caused corner-cutting before.
    private void BuildSmoothPath()
    {
        path.Clear();

        if (sortedCheckpoints == null || sortedCheckpoints.Count < MIN_CHECKPOINTS_FOR_PATH)
        {
            Debug.LogWarning("AIDriver: Not enough checkpoints to build a smooth path (" +
                (sortedCheckpoints != null ? sortedCheckpoints.Count : 0) + " found, need at least " + MIN_CHECKPOINTS_FOR_PATH + ").");
            return;
        }

        int n = sortedCheckpoints.Count;
        List<Vector3> control = new List<Vector3>(n);
        for (int i = 0; i < n; i++)
            control.Add(sortedCheckpoints[i].transform.position);

        for (int i = 0; i < n; i++)
        {
            Vector3 p0 = control[(i - 1 + n) % n];
            Vector3 p1 = control[i];
            Vector3 p2 = control[(i + 1) % n];
            Vector3 p3 = control[(i + 2) % n];

            float segmentLength = Vector3.Distance(p1, p2);
            int steps = Mathf.Max(2, Mathf.CeilToInt(segmentLength / Mathf.Max(0.5f, pathSampleSpacing)));

            for (int s = 0; s < steps; s++)
            {
                float t = (float)s / steps;
                path.Add(CatmullRom(p0, p1, p2, p3, t));
            }
        }

        Debug.Log($"AIDriver: Built smooth path with {path.Count} points from {n} checkpoints");
    }

    private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;

        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }

    private void FindNearestPathIndex()
    {
        if (path.Count == 0) return;

        float bestDist = float.MaxValue;
        int bestIdx = 0;

        for (int i = 0; i < path.Count; i++)
        {
            float d = Vector3.Distance(transform.position, path[i]);
            if (d < bestDist)
            {
                bestDist = d;
                bestIdx = i;
            }
        }

        pathIndex = bestIdx;
    }

    // Advances pathIndex to the closest point within a forward-looking window, so
    // the anchor tracks the car's real progress along the path without ever
    // jumping backward or wrapping incorrectly.
    private void UpdatePathIndex()
    {
        if (path.Count == 0) return;

        int windowSize = Mathf.Min(20, path.Count - 1);
        float bestDist = Vector3.Distance(transform.position, path[pathIndex]);
        int bestIdx = pathIndex;

        for (int i = 1; i <= windowSize; i++)
        {
            int idx = (pathIndex + i) % path.Count;
            float d = Vector3.Distance(transform.position, path[idx]);
            if (d < bestDist)
            {
                bestDist = d;
                bestIdx = idx;
            }
        }

        pathIndex = bestIdx;
    }

    // Pure-pursuit target point: walks forward along the path accumulating actual
    // distance (not a fixed number of points) until it reaches the look-ahead
    // distance for the AI's current speed.
    private Vector3 GetPursuitPoint()
    {
        if (path.Count == 0)
        {
            RaceCheckpoint cp = GetCurrentTarget();
            if (cp != null) return cp.transform.position;
            return transform.position + transform.forward * 30f;
        }

        UpdatePathIndex();

        float speedKmh = rb.linearVelocity.magnitude * 3.6f;
        float lookAhead = Mathf.Lerp(minLookAhead, maxLookAhead, Mathf.Clamp01(speedKmh / 140f));

        Vector3 point = path[pathIndex];
        Vector3 prev = point;
        float accumulated = 0f;
        int idx = pathIndex;
        int steps = 0;

        while (accumulated < lookAhead && steps < path.Count)
        {
            idx = (idx + 1) % path.Count;
            Vector3 next = path[idx];
            accumulated += Vector3.Distance(prev, next);
            point = next;
            prev = next;
            steps++;
        }

        return point;
    }

    // Estimates the tightest curve radius coming up within the braking horizon so
    // the AI can start braking BEFORE the corner instead of reacting once it's
    // already turning sharply.
    private float GetTargetCorneringSpeed()
    {
        if (path.Count < 9) return aiMaxSpeed;

        float speedMs = rb.linearVelocity.magnitude;
        float horizonDist = Mathf.Max(20f, speedMs * brakeLookAheadTime * 2f);
        float minRadius = float.MaxValue;

        int sampleStep = 3; // sample every few path points to reduce noise
        float accumulated = 0f;
        int i = 0;

        while (accumulated < horizonDist && i < path.Count)
        {
            int i0 = (pathIndex + i) % path.Count;
            int i1 = (pathIndex + i + sampleStep) % path.Count;
            int i2 = (pathIndex + i + sampleStep * 2) % path.Count;

            Vector3 p0 = path[i0];
            Vector3 p1 = path[i1];
            Vector3 p2 = path[i2];

            float radius = EstimateCircumradius(p0, p1, p2);
            if (radius < minRadius) minRadius = radius;

            accumulated += Vector3.Distance(p0, p1);
            i += sampleStep;
        }

        if (minRadius >= 500f) return aiMaxSpeed; // effectively straight ahead

        float safeSpeedMs = Mathf.Sqrt(Mathf.Max(1f, maxLateralAccel * minRadius));
        float safeSpeedKmh = safeSpeedMs * 3.6f;

        // brakeSkill lets sharper AIs carry a bit more corner speed
        safeSpeedKmh *= Mathf.Lerp(0.85f, 1.05f, brakeSkill);

        return Mathf.Clamp(safeSpeedKmh, 20f, aiMaxSpeed);
    }

    private static float EstimateCircumradius(Vector3 a, Vector3 b, Vector3 c)
    {
        a.y = 0f; b.y = 0f; c.y = 0f;

        float sideA = Vector3.Distance(b, c);
        float sideB = Vector3.Distance(a, c);
        float sideC = Vector3.Distance(a, b);

        float s = (sideA + sideB + sideC) * 0.5f;
        float areaSq = s * (s - sideA) * (s - sideB) * (s - sideC);
        if (areaSq <= 0.0001f) return 9999f; // collinear / straight

        float area = Mathf.Sqrt(areaSq);
        if (area <= 0.0001f) return 9999f;

        return (sideA * sideB * sideC) / (4f * area);
    }

    private Vector3 GetRecoveryTarget()
    {
        if (path.Count > 0)
            return path[pathIndex];

        RaceCheckpoint cp = GetCurrentTarget();
        if (cp != null) return cp.transform.position;

        return transform.position + transform.forward * 30f;
    }

    private void Start()
    {
        if (car == null)
        {
            car = GetComponent<PhotonCarController>();
            rb = GetComponent<Rigidbody>();
            tracker = GetComponent<PlayerLapTracker>();
        }

        if (car == null)
        {
            Debug.LogWarning($"AIDriver on '{gameObject.name}': PhotonCarController not found yet, will retry...");
            StartCoroutine(DelayedInit());
        }
    }

    private IEnumerator DelayedInit()
    {
        yield return new WaitForSeconds(1f);

        if (car != null) yield break;

        car = GetComponent<PhotonCarController>();
        rb = GetComponent<Rigidbody>();
        tracker = GetComponent<PlayerLapTracker>();

        if (car != null)
        {
            string aiName = gameObject.name;
            Initialize(aiName);
        }
        else
        {
            Debug.LogError($"AIDriver on '{gameObject.name}': Failed to find PhotonCarController after delay!");
        }
    }

    private void Update()
    {
        if (ready) return;
        readyTimer += Time.deltaTime;
        if ((RaceManager.Instance != null && RaceManager.Instance.raceStarted) || readyTimer > 4f)
        {
            ready = true;
            state = State.Drive;
            stuckTime = 0f;
            stuckCount = 0;
            startFrames = 30;
            lastPos = transform.position;
            FindNearestPathIndex();

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            Debug.Log($"AI '{tracker?.aiName}': READY - starting drive from path index {pathIndex}, forward={transform.forward}");
        }
    }

    private void FixedUpdate()
    {
        if (car == null) return;
        if (!ready)
        {
            car.SetInput(0f, 1f, 0f);
            return;
        }

        if (tracker != null && tracker.raceCompleted && state != State.Finished)
        {
            state = State.Finished;
            Debug.Log($"AI '{tracker?.aiName}': race completed -> slowing to a stop");
        }

        if (tracker != null && tracker.aiName != "" && nextCP != tracker.nextCheckpointIndex)
        {
            nextCP = tracker.nextCheckpointIndex;
        }

        switch (state)
        {
            case State.Drive: DoDrive(); break;
            case State.Reverse: DoReverse(); break;
            case State.Turn: DoTurn(); break;
            case State.Finished: DoFinished(); break;
        }
    }

    private RaceCheckpoint GetCurrentTarget()
    {
        if (sortedCheckpoints == null || totalCheckpoints == 0) return null;

        for (int i = 0; i < sortedCheckpoints.Count; i++)
        {
            if (sortedCheckpoints[i].checkpointIndex == nextCP)
                return sortedCheckpoints[i];
        }
        return null;
    }

    private void ApplyOpponentAvoidance(ref float steerValue, ref float throttleValue)
    {
        Vector3 checkCenter = transform.position + transform.forward * (avoidCheckDistance * 0.5f) + Vector3.up * 0.5f;
        Collider[] hits = Physics.OverlapSphere(checkCenter, avoidCheckDistance * 0.5f, carLayerMask);

        float closestFwdDist = float.MaxValue;
        float avoidLateral = 0f;
        bool blocked = false;

        for (int i = 0; i < hits.Length; i++)
        {
            PhotonCarController otherCar = hits[i].GetComponentInParent<PhotonCarController>();
            if (otherCar == null || otherCar.transform == transform) continue;

            Vector3 toOther = otherCar.transform.position - transform.position;
            float fwdDist = Vector3.Dot(toOther, transform.forward);
            float lateral = Vector3.Dot(toOther, transform.right);

            if (fwdDist > 0.5f && fwdDist < avoidCheckDistance && Mathf.Abs(lateral) < avoidCheckRadius * 2f)
            {
                blocked = true;
                if (fwdDist < closestFwdDist)
                {
                    closestFwdDist = fwdDist;
                    avoidLateral = lateral;
                }
            }
        }

        if (blocked)
        {
            float avoidDir = avoidLateral >= 0f ? -1f : 1f;
            float proximity = Mathf.Clamp01(1f - closestFwdDist / avoidCheckDistance);
            steerValue = Mathf.Clamp(steerValue + avoidDir * avoidSteerStrength * proximity, -1f, 1f);
            throttleValue *= Mathf.Lerp(1f, 0.55f, proximity);
        }
    }

    private void DoDrive()
    {
        Vector3 pursuitPoint = GetPursuitPoint();

        Vector3 dirToTarget = pursuitPoint - transform.position;
        dirToTarget.y = 0f;

        if (dirToTarget.sqrMagnitude < 0.01f)
        {
            car.SetInput(0.5f, 0f, steer);
            lastPos = transform.position;
            return;
        }

        Vector3 fwd = transform.forward; fwd.y = 0; fwd.Normalize();
        Vector3 dirNorm = dirToTarget.normalized;
        float cross = Vector3.Cross(fwd, dirNorm).y;
        float angleToTarget = Vector3.Angle(fwd, dirNorm);

        float speedKmh = rb.linearVelocity.magnitude * 3.6f;
        float steerSmooth = Mathf.Lerp(steerResponsiveness, steerResponsiveness * 0.5f, Mathf.Clamp01(speedKmh / 120f));
        steer = Mathf.Lerp(steer, Mathf.Clamp(cross, -1f, 1f), Time.fixedDeltaTime * steerSmooth);

        if (startFrames > 0)
        {
            startFrames--;
            car.SetInput(1f, 0f, steer);
            lastPos = transform.position;
            return;
        }

        float targetSpeed = GetTargetCorneringSpeed();
        float throttle;
        float brake = 0f;

        if (speedKmh < targetSpeed - 3f)
        {
            throttle = 1f;
        }
        else if (speedKmh > targetSpeed + 2f)
        {
            throttle = 0f;
            float overshoot = Mathf.Clamp01((speedKmh - targetSpeed) / 40f);
            brake = Mathf.Lerp(0.15f, 0.9f, overshoot) * Mathf.Lerp(0.6f, 1f, brakeSkill);
        }
        else
        {
            throttle = 0.5f;
        }

        // Safety net: if the immediate pursuit point still implies a sharp turn
        // (e.g. right after recovering from being stuck), back off throttle/brake
        // even if the longer-range curvature scan didn't flag it.
        if (angleToTarget > 40f)
        {
            throttle = Mathf.Min(throttle, 0.15f);
            brake = Mathf.Max(brake, Mathf.Lerp(0.3f, 0.6f, brakeSkill));
        }

        if (speedKmh > aiMaxSpeed)
        {
            throttle = 0f;
            brake = Mathf.Max(brake, 0.15f);
        }

        ApplyOpponentAvoidance(ref steer, ref throttle);

        car.SetInput(throttle, brake, steer);

        float moved = Vector3.Distance(transform.position, lastPos);
        float stuckThreshold = speedKmh > 10f ? 0.5f : 0.15f;
        if (moved < stuckThreshold)
        {
            stuckTime += Time.fixedDeltaTime;
            if (stuckTime > stuckTimeout)
            {
                HandleStuck();
            }
        }
        else
        {
            stuckTime = Mathf.Max(0f, stuckTime - Time.fixedDeltaTime * 0.5f);
        }

        lastPos = transform.position;
    }

    private void HandleStuck()
    {
        stuckCount++;
        stuckTime = 0f;

        if (stuckCount >= 5)
        {
            if (path.Count > 0)
                pathIndex = (pathIndex + 15) % path.Count;

            Vector3 pushDir = -transform.forward + transform.right * turnDir;
            rb.linearVelocity = pushDir.normalized * 8f;

            state = State.Drive;
            stuckCount = 0;
            Debug.Log($"AI '{tracker?.aiName}': FORCE UNSTUCK - skipped ahead on path, pushed sideways");
        }
        else if (stuckCount >= 3)
        {
            state = State.Reverse;
            stateTimer = reverseTime * 2f;
            turnDir = GetTurnDirTowardRoad();
            Debug.Log($"AI '{tracker?.aiName}': STUCK #{stuckCount} -> LONG reverse");
        }
        else
        {
            state = State.Reverse;
            stateTimer = reverseTime;
            turnDir = GetTurnDirTowardRoad();
            Debug.Log($"AI '{tracker?.aiName}': STUCK #{stuckCount} -> reverse");
        }
    }

    private float GetTurnDirTowardRoad()
    {
        Vector3 recoveryTarget = GetRecoveryTarget();
        Vector3 dirToTarget = recoveryTarget - transform.position;
        dirToTarget.y = 0f;

        Vector3 fwd = transform.forward; fwd.y = 0; fwd.Normalize();
        float cross = Vector3.Cross(fwd, dirToTarget.normalized).y;

        if (Mathf.Abs(cross) < 0.15f)
        {
            cross = Vector3.Cross(fwd, transform.right).y > 0 ? 1f : -1f;
        }

        return cross > 0 ? 1f : -1f;
    }

    private void DoReverse()
    {
        Vector3 recoveryTarget = GetRecoveryTarget();
        Vector3 dirToTarget = recoveryTarget - transform.position;
        dirToTarget.y = 0f;

        Vector3 fwd = transform.forward; fwd.y = 0; fwd.Normalize();

        float reverseSteer;
        if (dirToTarget.sqrMagnitude > 0.1f)
        {
            Vector3 dirNorm = dirToTarget.normalized;
            reverseSteer = -Vector3.Cross(fwd, dirNorm).y;
        }
        else
        {
            reverseSteer = turnDir;
        }

        steer = Mathf.Lerp(steer, Mathf.Clamp(reverseSteer * turnDir, -1f, 1f), Time.fixedDeltaTime * 8f);

        float reverseForce = stuckCount >= 3 ? -1f : -0.8f;
        car.SetInput(reverseForce, 0f, steer);
        stateTimer -= Time.fixedDeltaTime;

        float moved = Vector3.Distance(transform.position, lastPos);
        float speedKmh = rb.linearVelocity.magnitude * 3.6f;

        if (stateTimer <= 0f || moved > 6f || speedKmh > 15f)
        {
            state = State.Turn;
            stateTimer = turnTime * (stuckCount >= 3 ? 1.5f : 1f);
            Debug.Log($"AI '{tracker?.aiName}': turning toward road (moved={moved:F1}, speed={speedKmh:F0})...");
        }
        lastPos = transform.position;
    }

    private void DoTurn()
    {
        Vector3 recoveryTarget = GetRecoveryTarget();
        Vector3 dirToTarget = recoveryTarget - transform.position;
        dirToTarget.y = 0f;

        Vector3 fwd = transform.forward; fwd.y = 0; fwd.Normalize();

        float cross = 0f;
        float angle = 90f;
        if (dirToTarget.sqrMagnitude > 0.1f)
        {
            Vector3 dirNorm = dirToTarget.normalized;
            cross = Vector3.Cross(fwd, dirNorm).y;
            angle = Vector3.Angle(fwd, dirNorm);
        }

        float turnAggression = stuckCount >= 3 ? 1.5f : 1.2f;
        steer = Mathf.Lerp(steer, Mathf.Clamp(cross * turnAggression, -1f, 1f), Time.fixedDeltaTime * 8f);

        float throttle = angle > 15f ? 0.2f : 0.6f;
        car.SetInput(throttle, 0f, steer);

        stateTimer -= Time.fixedDeltaTime;

        if (angle < 35f)
        {
            state = State.Drive;
            stuckTime = 0f;
            stuckCount = 0;
            FindNearestPathIndex();
            Debug.Log($"AI '{tracker?.aiName}': facing road (angle={angle:F0}) -> drive");
            return;
        }

        if (stateTimer <= 0f)
        {
            state = State.Drive;
            stuckTime = 0f;
            stuckCount = 0;
            FindNearestPathIndex();
            Debug.Log($"AI '{tracker?.aiName}': turn timeout -> drive");
        }
        lastPos = transform.position;
    }

    // Smoothly brakes the AI car to a stop once the race is complete, instead of
    // continuing to chase path points forever.
    private void DoFinished()
    {
        float speedKmh = rb.linearVelocity.magnitude * 3.6f;

        if (speedKmh > 3f)
        {
            car.SetInput(0f, 0.6f, 0f);
        }
        else
        {
            car.SetInput(0f, 0.05f, 0f);
        }
    }
}