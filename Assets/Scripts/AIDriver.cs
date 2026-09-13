using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AIDriver : MonoBehaviour
{
    [Header("AI Difficulty")]
    [Range(0.5f, 1f)]
    public float skillLevel = 0.75f;
    public float maxSpeedVariation = 15f;
    public float brakeSkill = 0.7f;

    [Header("Path Following")]
    public float minLookAhead = 8f;
    public float maxLookAhead = 25f;
    public float steerResponsiveness = 14f;

    [Header("Cornering")]
    public float maxLateralAccel = 8f;

    [Header("Opponent Avoidance")]
    public float avoidCheckDistance = 14f;
    public float avoidCheckRadius = 2.2f;
    public float avoidSteerStrength = 0.6f;
    public LayerMask carLayerMask = ~0;

    [Header("Boundary Correction")]
    public float boundaryPushStrength = 1.5f;

    [Header("Stuck Recovery")]
    public float stuckTimeout = 3f;
    public float reverseTime = 1.5f;
    public float turnTime = 2f;

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

    private int lineIndex = 0;

    private float aiMaxSpeed;
    private float aiMotorForce;
    private float aiBrakeForce;
    private int startFrames = 0;

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
        nextCP = tracker != null ? tracker.nextCheckpointIndex : 0;

        EnsureTrackSystems();

        lineIndex = RacingLine.Instance != null ? RacingLine.Instance.FindNearestIndex(transform.position) : 0;

        Debug.Log($"AI '{aiName}': checkpoints={totalCheckpoints}, linePoints={RacingLine.Instance?.line.Count ?? 0}, maxSpeed={aiMaxSpeed:F0}, skill={skillLevel}");
    }

    private void EnsureTrackSystems()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (sceneName != "aioponent")
            return;

        if (TrackData.Instance == null)
        {
            GameObject tdObj = new GameObject("TrackData");
            tdObj.AddComponent<TrackData>();
        }

        if (RacingLine.Instance == null)
        {
            GameObject rlObj = new GameObject("RacingLine");
            rlObj.AddComponent<RacingLine>();
        }

        if (SpeedProfile.Instance == null)
        {
            GameObject spObj = new GameObject("SpeedProfile");
            spObj.AddComponent<SpeedProfile>();
        }

        if (TrackData.Instance.points.Count == 0)
            TrackData.Instance.Build();

        if (RacingLine.Instance.line.Count == 0)
            RacingLine.Instance.Build();

        if (SpeedProfile.Instance.maxSpeeds.Count == 0)
            SpeedProfile.Instance.Build(aiMaxSpeed);
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
            stuckTime = 0f;
            stuckCount = 0;
            startFrames = 30;
            lastPos = transform.position;

            if (RacingLine.Instance != null && RacingLine.Instance.line.Count > 0)
            {
                lineIndex = RacingLine.Instance.FindNearestIndex(transform.position);
                // Snap position to the racing line so the car starts on the road
                Vector3 linePos = RacingLine.Instance.line[lineIndex].position;
                linePos.y = transform.position.y;
                transform.position = linePos;
                SnapToLineDirection();
            }

            if (rb != null)
            {
                rb.linearVelocity = rb.linearVelocity * 0.3f;
                rb.angularVelocity = Vector3.zero;
            }

            state = State.Drive;

            Debug.Log($"AI '{tracker?.aiName}': READY - starting from line index {lineIndex}, forward={transform.forward}");
        }
    }

    public void ResetForNewRace()
    {
        ready = false;
        readyTimer = 0f;
        state = State.Wait;
        stateTimer = 0f;
        stuckTime = 0f;
        stuckCount = 0;
        startFrames = 0;
        steer = 0f;
        lineIndex = 0;
        nextCP = 0;
        lastPos = transform.position;
    }

    private void FixedUpdate()
    {
        if (car == null) return;

        if (!ready)
        {
            PrepareDuringCountdown();
            return;
        }

        if (tracker != null && tracker.raceCompleted)
        {
            state = State.Finished;
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

    private void PrepareDuringCountdown()
    {
        if (countdownPrepared > 1f)
        {
            if (RacingLine.Instance != null && RacingLine.Instance.line.Count > 0)
            {
                Vector3 target = RacingLine.Instance.line[lineIndex].position;
                Vector3 dirToTarget = target - transform.position;
                dirToTarget.y = 0f;

                if (dirToTarget.sqrMagnitude > 0.01f)
                {
                    Vector3 fwd = transform.forward; fwd.y = 0; fwd.Normalize();
                    Vector3 dirNorm = dirToTarget.normalized;
                    float cross = Vector3.Cross(fwd, dirNorm).y;
                    float angle = Vector3.Angle(fwd, dirNorm);

                    float steerPrep = Mathf.Clamp(cross, -1f, 1f);
                    steerPrep *= Mathf.Lerp(1f, 0.3f, angle / 90f);
                    car.SetInput(0.1f, 0.8f, steerPrep);
                }
                else
                {
                    car.SetInput(0.1f, 0.8f, 0f);
                }
            }
            else
            {
                car.SetInput(0.1f, 0.8f, 0f);
            }
        }
        else
        {
            car.SetInput(0f, 0.9f, 0f);
        }
        countdownPrepared += Time.fixedDeltaTime;
    }

    private float countdownPrepared = 0f;

    private void SnapToLineDirection()
    {
        if (RacingLine.Instance == null || RacingLine.Instance.line.Count == 0) return;

        int lookAhead = RacingLine.Instance.GetForwardIndex(lineIndex, 20f);
        Vector3 target = RacingLine.Instance.line[lookAhead].position;
        Vector3 dir = target - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.01f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
        transform.rotation = targetRot;

        Debug.Log($"AI '{tracker?.aiName}': Snapped to racing line direction at index {lineIndex}");
    }

    private void DoDrive()
    {
        if (RacingLine.Instance == null || RacingLine.Instance.line.Count == 0)
        {
            car.SetInput(0.5f, 0f, 0f);
            return;
        }

        UpdateLineIndex();

        float speedKmh = rb.linearVelocity.magnitude * 3.6f;
        float lookAheadDist = Mathf.Lerp(minLookAhead, maxLookAhead, Mathf.Clamp01(speedKmh / 130f));

        int targetIdx = RacingLine.Instance.GetForwardIndex(lineIndex, lookAheadDist);
        Vector3 pursuitPoint = RacingLine.Instance.line[targetIdx].position;

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

        float steerSmooth = Mathf.Lerp(steerResponsiveness, steerResponsiveness * 0.6f, Mathf.Clamp01(speedKmh / 140f));
        steer = Mathf.Lerp(steer, Mathf.Clamp(cross, -1f, 1f), Time.fixedDeltaTime * steerSmooth * 1.8f);

        float boundaryCorrection = ComputeBoundaryCorrection();
        steer = Mathf.Clamp(steer + boundaryCorrection, -1f, 1f);

        if (startFrames > 0)
        {
            startFrames--;
            float rampT = 1f - (startFrames / 30f);
            float rampedThrottle = Mathf.Lerp(0.7f, 1f, rampT);
            float startBrake = angleToTarget > 45f ? Mathf.Lerp(0.3f, 0.6f, brakeSkill) : 0f;
            if (startFrames > 20) rampedThrottle = Mathf.Max(rampedThrottle, 0.8f);
            car.SetInput(rampedThrottle, startBrake, steer);
            lastPos = transform.position;
            return;
        }

        float targetSpeed = GetTargetSpeed(targetIdx);
        float throttle;
        float brake = 0f;

        if (speedKmh < targetSpeed - 8f)
        {
            throttle = 1f;
        }
        else if (speedKmh > targetSpeed - 5f)
        {
            throttle = 0f;
            float overshoot = Mathf.Clamp01((speedKmh - targetSpeed) / 25f);
            brake = Mathf.Lerp(0.3f, 1f, overshoot) * Mathf.Lerp(0.8f, 1f, brakeSkill);
        }
        else
        {
            throttle = 0.3f;
        }

        if (angleToTarget > 25f)
        {
            throttle = Mathf.Min(throttle, 0.05f);
            brake = Mathf.Max(brake, Mathf.Lerp(0.5f, 0.8f, brakeSkill));
        }

        float curvatureAhead = GetCurvatureAhead(lineIndex, 50f);
        if (curvatureAhead > 0.8f)
        {
            if (speedKmh > targetSpeed * 0.7f)
            {
                throttle = Mathf.Min(throttle, 0.1f);
                brake = Mathf.Max(brake, 0.4f * Mathf.Lerp(0.7f, 1f, brakeSkill));
            }
        }
        else if (curvatureAhead > 0.4f)
        {
            if (speedKmh > targetSpeed * 0.85f)
                throttle = Mathf.Min(throttle, 0.2f);
        }

        if (speedKmh > aiMaxSpeed)
        {
            throttle = 0f;
            brake = Mathf.Max(brake, 0.15f);
        }

        ApplyOpponentAvoidance(ref steer, ref throttle);

        car.SetInput(throttle, brake, steer);

        float moved = Vector3.Distance(transform.position, lastPos);
        // More lenient at race start - give car time to get rolling
        float stuckThreshold = (startFrames > 0) ? 0.3f : (speedKmh > 10f ? 0.5f : 0.15f);
        float timeout = (startFrames > 0) ? 5f : stuckTimeout;
        if (moved < stuckThreshold)
        {
            stuckTime += Time.fixedDeltaTime;
            if (stuckTime > timeout)
                HandleStuck();
        }
        else
        {
            stuckTime = Mathf.Max(0f, stuckTime - Time.fixedDeltaTime * 0.5f);
        }

        lastPos = transform.position;
    }

    private void UpdateLineIndex()
    {
        if (RacingLine.Instance == null || RacingLine.Instance.line.Count == 0) return;

        int n = RacingLine.Instance.line.Count;
        int bestIdx = lineIndex;
        float bestDist = Vector3.Distance(transform.position, RacingLine.Instance.line[lineIndex].position);

        int windowSize = Mathf.Min(30, n);
        for (int i = 1; i <= windowSize; i++)
        {
            int idx = (lineIndex + i) % n;
            float d = Vector3.Distance(transform.position, RacingLine.Instance.line[idx].position);
            if (d < bestDist)
            {
                bestDist = d;
                bestIdx = idx;
            }
        }

        lineIndex = bestIdx;
    }

    private float GetTargetSpeed(int targetIdx)
    {
        if (SpeedProfile.Instance == null) return aiMaxSpeed;
        return SpeedProfile.Instance.GetMaxSpeed(targetIdx);
    }

    private float GetCurvatureAhead(int fromIdx, float distance)
    {
        if (TrackData.Instance == null || TrackData.Instance.points.Count == 0) return 0f;

        int n = TrackData.Instance.points.Count;
        float maxCurv = 0f;
        float accum = 0f;
        int idx = fromIdx;

        while (accum < distance)
        {
            int next = (idx + 1) % n;
            accum += Vector3.Distance(TrackData.Instance.points[idx].center, TrackData.Instance.points[next].center);
            float absC = Mathf.Abs(TrackData.Instance.points[next].curvature);
            if (absC > maxCurv) maxCurv = absC;
            idx = next;
        }

        return maxCurv;
    }

    private float ComputeBoundaryCorrection()
    {
        if (TrackData.Instance == null || TrackData.Instance.points.Count == 0) return 0f;

        int trackIdx = TrackData.Instance.FindNearestIndex(transform.position);
        var pt = TrackData.Instance.points[trackIdx];

        float leftDist = Vector3.Distance(transform.position, pt.left);
        float rightDist = Vector3.Distance(transform.position, pt.right);
        float roadWidth = leftDist + rightDist;

        if (roadWidth < 0.1f) return 0f;

        float normalizedPos = (rightDist - leftDist) / roadWidth;
        float centerOffset = -normalizedPos;

        float absOffset = Mathf.Abs(centerOffset);
        if (absOffset < 0.3f) return 0f;

        float correction = Mathf.Sign(centerOffset) * Mathf.Lerp(0f, boundaryPushStrength, (absOffset - 0.3f) / 0.7f);
        return Mathf.Clamp(correction, -1f, 1f);
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

    private void HandleStuck()
    {
        stuckCount++;
        stuckTime = 0f;

        state = State.Reverse;
        stateTimer = stuckCount >= 3 ? reverseTime * 2f : reverseTime;
        turnDir = GetTurnDirTowardLine();

        Debug.Log($"AI '{tracker?.aiName}': STUCK #{stuckCount} -> reverse");
    }

    private float GetTurnDirTowardLine()
    {
        if (RacingLine.Instance == null || RacingLine.Instance.line.Count == 0) return 1f;

        // Look ahead on the racing line for a meaningful direction
        int forwardIdx = RacingLine.Instance.GetForwardIndex(lineIndex, 30f);
        Vector3 target = RacingLine.Instance.line[forwardIdx].position;
        Vector3 dirToTarget = target - transform.position;
        dirToTarget.y = 0f;

        Vector3 fwd = transform.forward; fwd.y = 0; fwd.Normalize();
        float cross = Vector3.Cross(fwd, dirToTarget.normalized).y;

        if (Mathf.Abs(cross) < 0.15f)
            cross = Vector3.Cross(fwd, transform.right).y > 0 ? 1f : -1f;

        return cross > 0 ? 1f : -1f;
    }

    private void DoReverse()
    {
        UpdateLineIndex();

        Vector3 recoveryTarget = GetRecoveryTarget();
        Vector3 dirToTarget = recoveryTarget - transform.position;
        dirToTarget.y = 0f;

        Vector3 fwd = transform.forward; fwd.y = 0; fwd.Normalize();

        float reverseSteer;
        if (dirToTarget.sqrMagnitude > 0.1f)
            reverseSteer = -Vector3.Cross(fwd, dirToTarget.normalized).y;
        else
            reverseSteer = turnDir;

        steer = Mathf.Lerp(steer, Mathf.Clamp(reverseSteer * turnDir, -1f, 1f), Time.fixedDeltaTime * 8f);

        float reverseForce = stuckCount >= 3 ? -1f : -0.8f;
        car.SetInput(reverseForce, 0f, steer);
        stateTimer -= Time.fixedDeltaTime;

        float moved = Vector3.Distance(transform.position, lastPos);

        if (stateTimer <= 0f || moved > 8f)
        {
            state = State.Turn;
            stateTimer = turnTime * (stuckCount >= 3 ? 1.5f : 1f);
        }
        lastPos = transform.position;
    }

    private void DoTurn()
    {
        // Re-find nearest racing line point after reversing
        UpdateLineIndex();

        Vector3 recoveryTarget = GetRecoveryTarget();
        Vector3 dirToTarget = recoveryTarget - transform.position;
        dirToTarget.y = 0f;

        Vector3 fwd = transform.forward; fwd.y = 0; fwd.Normalize();

        float cross = 0f;
        float angle = 90f;
        if (dirToTarget.sqrMagnitude > 0.1f)
        {
            cross = Vector3.Cross(fwd, dirToTarget.normalized).y;
            angle = Vector3.Angle(fwd, dirToTarget);
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
            UpdateLineIndex();
            Debug.Log($"AI '{tracker?.aiName}': facing road (angle={angle:F0}) -> drive");
            return;
        }

        if (stateTimer <= 0f)
        {
            if (angle > 30f && stuckCount < 5)
            {
                state = State.Reverse;
                stateTimer = reverseTime * 1.5f;
                turnDir = GetTurnDirTowardLine();
                stuckCount++;
                Debug.Log($"AI '{tracker?.aiName}': turn timeout angle={angle:F0} -> reverse again (stuck #{stuckCount})");
            }
            else
            {
                state = State.Drive;
                stuckTime = 0f;
                stuckCount = 0;
                UpdateLineIndex();
                Debug.Log($"AI '{tracker?.aiName}': turn timeout -> drive");
            }
        }
        lastPos = transform.position;
    }

    private void DoFinished()
    {
        float speedKmh = rb.linearVelocity.magnitude * 3.6f;
        if (speedKmh > 3f)
            car.SetInput(0f, 0.6f, 0f);
        else
            car.SetInput(0f, 0.05f, 0f);
    }

    private Vector3 GetRecoveryTarget()
    {
        if (RacingLine.Instance != null && RacingLine.Instance.line.Count > 0)
        {
            // Look AHEAD on the racing line, not at the nearest point
            // The nearest point may be right under the car, giving meaningless angles
            int forwardIdx = RacingLine.Instance.GetForwardIndex(lineIndex, 50f);
            return RacingLine.Instance.line[forwardIdx].position;
        }

        if (sortedCheckpoints != null && sortedCheckpoints.Count > 0)
        {
            for (int i = 0; i < sortedCheckpoints.Count; i++)
            {
                if (sortedCheckpoints[i].checkpointIndex == nextCP)
                    return sortedCheckpoints[i].transform.position;
            }
            return sortedCheckpoints[0].transform.position;
        }

        return transform.position + transform.forward * 30f;
    }
}
