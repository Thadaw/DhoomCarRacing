using UnityEngine;

public class AIDriver : MonoBehaviour
{
    [Header("AI Identity")]
    public string aiName = "";
    public int aiIndex = 0;

    [Header("Waypoint Path")]
    public Transform[] waypoints;
    public int currentWaypointIndex = 0;
    public float waypointReachDistance = 10f;
    [Tooltip("seconds of path lookahead — the aim point sits this far ahead of the car along the racing line")]
    public float lookAheadSeconds = 0.6f;
    [Tooltip("clamp on that lookahead in metres (short = twitchy , long = wide lines)")]
    public float minLookahead = 8f;
    public float maxLookahead = 45f;
    public float lostWaypointDistance = 70f;

    [Header("Speed")]
    public float maxSpeedKmh = 85f;
    public float slowSpeedKmh = 35f;
    public float brakeResponse = 25f;

    [Header("Corner Braking")]
    public int speedLookahead = 8;
    public float speedLookaheadDistance = 80f;

    [Header("Steering")]
    public float steerSensitivity = 4f;
    public float fullSteerAngle = 45f;

    [Header("Sensors")]
    public float frontSensorLength = 12f;
    public float sideSensorLength = 8f;
    public float sensorHeight = 0.6f;
    public float sensorSideOffset = 0.75f;
    public float avoidanceSteerStrength = 0.7f;
    public float brakeObstacleDistance = 6f;
    public LayerMask sensorLayers = ~0;

    [Header("Road Barrier")]
    public float wallAvoidDistance = 8f;
    public float boundaryRecoverSteer = 0.8f;

    [Header("Stuck Recovery")]
    public float stuckSpeedKmh = 3f;
    public float stuckTime = 2f;
    public float recoveryTime = 2f;
    public float recoveryThrottle = -0.7f;
    public int maxRecoveryAttempts = 3;

    private PhotonCarController car;
    private Rigidbody rb;
    private PlayerLapTracker tracker;

    private float[] waypointSpeeds;
    private float currentSteer = 0f;
    private bool obstacleAhead = false;
    private float frontObstacleDistance = float.MaxValue;
    private float avoidanceSteer = 0f;
    private float boundarySteer = 0f;

    private float stuckTimer = 0f;
    private float recoveryTimer = 0f;
    private bool recovering = false;
    private int recoveryAttempts = 0;
    private float normalDriveTimer = 0f;
    private bool finishStopLogged = false;

    // Checkpoint / finish triggers must never be seen as obstacles.
    private const QueryTriggerInteraction RayTriggers = QueryTriggerInteraction.Ignore;

    public void Initialize(string name)
    {
        aiName = name;
        car = GetComponent<PhotonCarController>();
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = GetComponentInChildren<Rigidbody>();
        tracker = GetComponent<PlayerLapTracker>();
        if (tracker == null) tracker = GetComponentInChildren<PlayerLapTracker>();

        if (tracker != null)
        {
            tracker.aiName = aiName;
            tracker.totalLaps = GameSession.Instance != null ? GameSession.Instance.TotalLaps : 1;
        }

        if (car != null)
        {
            car.useExternalInput = true;
        }

        CacheWaypointSpeeds();

        // Find nearest waypoint to start
        if (waypoints != null && waypoints.Length > 0)
        {
            float bestDist = float.MaxValue;
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] == null) continue;
                float d = Vector3.Distance(transform.position, waypoints[i].position);
                if (d < bestDist)
                {
                    bestDist = d;
                    currentWaypointIndex = i;
                }
            }
        }

        Debug.Log($"AIDriver '{aiName}': initialized, waypoints={waypoints?.Length ?? 0}, start index={currentWaypointIndex}");
    }

    private void CacheWaypointSpeeds()
    {
        if (waypoints == null)
        {
            waypointSpeeds = null;
            return;
        }

        waypointSpeeds = new float[waypoints.Length];
        for (int i = 0; i < waypoints.Length; i++)
        {
            waypointSpeeds[i] = maxSpeedKmh;
            if (waypoints[i] == null) continue;
            AIWaypoint wp = waypoints[i].GetComponent<AIWaypoint>();
            if (wp != null)
                waypointSpeeds[i] = wp.targetSpeedKmh;
        }
    }

    void FixedUpdate()
    {
        if (car == null || waypoints == null || waypoints.Length == 0) return;

        if (RaceManager.Instance != null && !RaceManager.Instance.raceStarted)
        {
            car.SetInput(0f, 0.8f, 0f);
            recovering = false;
            stuckTimer = 0f;
            recoveryTimer = 0f;
            return;
        }

        // The race is over for THIS car only once it crosses the finish line itself.
        // The player finishing first must not park the AI — otherwise the AI cars never
        // complete the race and their times stay DNF. A finished car still brakes to a
        // halt below, and stuck-recovery never runs on it.
        bool raceOverForThisCar = tracker != null
            ? tracker.raceCompleted
            : (RaceManager.Instance != null && RaceManager.Instance.raceFinished);
        if (raceOverForThisCar)
        {
            HandleFinished();
            return;
        }

        HandleWaypointProgress();
        HandleSensors();
        HandleSteering();
        UpdateStuckDetection();
        HandleSpeed();
    }

    // Kill the throttle and hold the brakes: the car slides to a stop in a straight
    // line and then stays parked instead of driving more laps.
    private void HandleFinished()
    {
        if (!finishStopLogged)
        {
            finishStopLogged = true;
            Debug.Log($"AIDriver '{aiName}': race finished — braking to a stop");
        }

        car.SetInput(0f, 1f, 0f);
    }

    void HandleWaypointProgress()
    {
        int count = waypoints.Length;

        // Drive past waypoints that are close enough or already behind us,
        // so a slightly missed waypoint can never stall the lap.
        for (int guard = 0; guard < count; guard++)
        {
            Transform target = waypoints[currentWaypointIndex];
            if (target == null)
            {
                currentWaypointIndex = (currentWaypointIndex + 1) % count;
                continue;
            }

            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;
            float dist = toTarget.magnitude;

            bool reached = dist <= waypointReachDistance;
            bool passed = dist <= waypointReachDistance * 3f &&
                          Vector3.Dot(transform.forward, toTarget) < -0.2f;

            if (reached || passed)
            {
                currentWaypointIndex = (currentWaypointIndex + 1) % count;
                continue;
            }

            break;
        }

        // Recovery: if the active waypoint is absurdly far away we got lost
        // (spun out / knocked backwards) — snap to the closest waypoint.
        Transform active = waypoints[currentWaypointIndex];
        if (active != null)
        {
            Vector3 toActive = active.position - transform.position;
            toActive.y = 0f;

            if (toActive.magnitude > lostWaypointDistance)
            {
                int best = currentWaypointIndex;
                float bestDist = float.MaxValue;
                for (int i = 0; i < count; i++)
                {
                    if (waypoints[i] == null) continue;
                    Vector3 d = waypoints[i].position - transform.position;
                    d.y = 0f;
                    float dist = d.magnitude;
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        best = i;
                    }
                }
                currentWaypointIndex = best;
                Debug.Log($"AIDriver '{aiName}': lost — re-synced to waypoint {best} ({bestDist:F1}m)");
            }
        }
    }

    // Aim point = a spot on the racing line a fixed TIME ahead of the car , so the
    // lookahead grows with speed (slow in corners , long on straights) . The old
    // version lerped toward the NEXT waypoint , which aims across the chord and cuts
    // every corner — this one always lands ON the path , so the car tracks the exact
    // line it was given .
    Vector3 GetAimPoint()
    {
        int count = waypoints.Length;
        Transform target = waypoints[currentWaypointIndex];
        if (target == null)
            return transform.position + transform.forward * 10f;

        float speedMs = rb != null ? rb.linearVelocity.magnitude : 0f;
        float lookahead = Mathf.Clamp(speedMs * lookAheadSeconds, minLookahead, maxLookahead);

        Vector3 prev = transform.position;
        float remaining = lookahead;

        // Walk forward along the waypoint chain until we have covered the lookahead,
        // then return the exact point on that polyline.
        for (int i = 0; i < count; i++)
        {
            int idx = (currentWaypointIndex + i) % count;
            if (waypoints[idx] == null) continue;

            Vector3 p = waypoints[idx].position;
            float seg = Vector3.Distance(prev, p);

            if (seg >= remaining && seg > 0.0001f)
            {
                float t = remaining / seg;
                return Vector3.Lerp(prev, p, t);
            }

            remaining -= seg;
            prev = p;
        }

        return prev;   // whole loop is closer than the lookahead — aim at the last point
    }

    void HandleSensors()
    {
        obstacleAhead = false;
        frontObstacleDistance = float.MaxValue;
        avoidanceSteer = 0f;

        // Flatten the cast directions so a bouncing car never raycasts into the ground.
        Vector3 fwd = transform.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.001f) fwd = transform.forward;
        else fwd.Normalize();

        Vector3 right = transform.right;
        right.y = 0f;
        if (right.sqrMagnitude < 0.001f) right = transform.right;
        else right.Normalize();

        Vector3 origin = transform.position + Vector3.up * sensorHeight;
        Vector3 leftOrigin = origin - right * sensorSideOffset;
        Vector3 rightOrigin = origin + right * sensorSideOffset;

        bool centerHit = Physics.Raycast(origin, fwd, out RaycastHit hitCenter, frontSensorLength, sensorLayers, RayTriggers);
        bool leftHit = Physics.Raycast(leftOrigin, fwd, out RaycastHit hitLeft, frontSensorLength, sensorLayers, RayTriggers);
        bool rightHit = Physics.Raycast(rightOrigin, fwd, out RaycastHit hitRight, frontSensorLength, sensorLayers, RayTriggers);

        if (centerHit)
        {
            obstacleAhead = true;
            frontObstacleDistance = Mathf.Min(frontObstacleDistance, hitCenter.distance);
        }
        if (leftHit)
        {
            obstacleAhead = true;
            frontObstacleDistance = Mathf.Min(frontObstacleDistance, hitLeft.distance);
            avoidanceSteer += avoidanceSteerStrength;
        }
        if (rightHit)
        {
            obstacleAhead = true;
            frontObstacleDistance = Mathf.Min(frontObstacleDistance, hitRight.distance);
            avoidanceSteer -= avoidanceSteerStrength;
        }

        // Lateral clearance (barriers / cars alongside), weighted by distance.
        float leftClear = sideSensorLength;
        float rightClear = sideSensorLength;

        if (Physics.Raycast(origin, -right, out RaycastHit hitSideLeft, sideSensorLength, sensorLayers, RayTriggers))
            leftClear = hitSideLeft.distance;
        if (Physics.Raycast(origin, right, out RaycastHit hitSideRight, sideSensorLength, sensorLayers, RayTriggers))
            rightClear = hitSideRight.distance;

        boundarySteer = PushFromWall(leftClear) - PushFromWall(rightClear);

        avoidanceSteer = Mathf.Clamp(avoidanceSteer, -1f, 1f);

        // Road ahead fully blocked and both sides clear: pick the side with more
        // room and pull out, instead of parking behind the car in front.
        if (obstacleAhead &&
            Mathf.Abs(avoidanceSteer) < 0.01f &&
            Mathf.Abs(boundarySteer) < 0.01f)
        {
            if (rightClear - leftClear > 1f)
                avoidanceSteer = 0.5f;
            else if (leftClear - rightClear > 1f)
                avoidanceSteer = -0.5f;
            else
                avoidanceSteer = (aiIndex % 2 == 0) ? 0.5f : -0.5f;
        }
    }

    // Returns a positive steer (turn right) when a wall is on the given side.
    private float PushFromWall(float clearanceOnOneSide)
    {
        if (clearanceOnOneSide >= wallAvoidDistance) return 0f;
        float weight = 1f - (clearanceOnOneSide / Mathf.Max(wallAvoidDistance, 0.01f));
        return Mathf.Clamp01(weight) * boundaryRecoverSteer;
    }

    void HandleSteering()
    {
        Vector3 aim = GetAimPoint();
        Vector3 toAim = aim - transform.position;
        toAim.y = 0f;

        float pathSteer = 0f;
        if (toAim.sqrMagnitude > 0.01f)
        {
            // Positive angle = target on the right = positive steer turns right.
            float angle = Vector3.SignedAngle(transform.forward, toAim, Vector3.up);
            pathSteer = Mathf.Clamp(angle / Mathf.Max(fullSteerAngle, 1f), -1f, 1f);
        }

        float steerInput = Mathf.Clamp(pathSteer + avoidanceSteer + boundarySteer, -1f, 1f);
        currentSteer = Mathf.Lerp(currentSteer, steerInput, Time.fixedDeltaTime * steerSensitivity);
        currentSteer = Mathf.Clamp(currentSteer, -1f, 1f);
    }

    void UpdateStuckDetection()
    {
        float speedKmh = rb != null ? rb.linearVelocity.magnitude * 3.6f : 0f;

        if (recovering)
        {
            recoveryTimer += Time.fixedDeltaTime;
            if (recoveryTimer >= recoveryTime)
            {
                recovering = false;
                stuckTimer = 0f;
                recoveryAttempts++;
                Debug.Log($"AIDriver '{aiName}': recovery attempt {recoveryAttempts} done (speed {speedKmh:F1} km/h)");

                // If it keeps failing, stop nudging around and put the car back on
                // its racing line facing the right way — it can never stay stuck.
                if (recoveryAttempts >= maxRecoveryAttempts)
                    SnapToRacingLine();
            }
            return;
        }

        if (speedKmh < stuckSpeedKmh)
        {
            stuckTimer += Time.fixedDeltaTime;
            if (stuckTimer >= stuckTime)
            {
                recovering = true;
                recoveryTimer = 0f;
                stuckTimer = 0f;
                LogStuckState(speedKmh);
            }
        }
        else
        {
            stuckTimer = 0f;

            // Driving normally for a while = the earlier trouble is behind us.
            normalDriveTimer += Time.fixedDeltaTime;
            if (normalDriveTimer >= 5f)
            {
                recoveryAttempts = 0;
                normalDriveTimer = 0f;
            }
        }
    }

    // Diagnostics so a log tells us exactly why the car stopped moving.
    private void LogStuckState(float speedKmh)
    {
        string obstacle = frontObstacleDistance < float.MaxValue
            ? frontObstacleDistance.ToString("F1") + "m"
            : "none";

        Debug.Log($"AIDriver '{aiName}': stuck — pos={transform.position}, speed={speedKmh:F1}km/h, " +
                  $"wp={currentWaypointIndex}/{waypoints.Length}, frontObstacle={obstacle}, " +
                  $"steer={currentSteer:F2}, boundary={boundarySteer:F2}, avoidance={avoidanceSteer:F2}, " +
                  $"rb={(rb != null)}, car={(car != null)} — reversing to recover");
    }

    // Last-resort unstick: place the car back on its racing line, facing forward.
    private void SnapToRacingLine()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        int best = 0;
        float bestDist = float.MaxValue;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;
            float d = Vector3.Distance(transform.position, waypoints[i].position);
            if (d < bestDist)
            {
                bestDist = d;
                best = i;
            }
        }

        Transform wp = waypoints[best];
        Transform next = waypoints[(best + 1) % waypoints.Length];

        Vector3 fwd = next != null ? next.position - wp.position : transform.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.001f) fwd = transform.forward;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        transform.position = wp.position;
        transform.rotation = Quaternion.LookRotation(fwd.normalized, Vector3.up);

        currentWaypointIndex = best;
        currentSteer = 0f;
        recovering = false;
        stuckTimer = 0f;
        recoveryAttempts = 0;
        normalDriveTimer = 0f;

        Debug.Log($"AIDriver '{aiName}': stuck too long — snapped back to racing line at waypoint {best} ({bestDist:F1}m)");
    }

    float GetTargetSpeed()
    {
        int count = waypoints.Length;
        float best = GetWaypointSpeed(currentWaypointIndex);

        // Brake early: respect the slowest of the upcoming corners.
        for (int i = 1; i <= speedLookahead; i++)
        {
            int idx = (currentWaypointIndex + i) % count;
            if (waypoints[idx] == null) continue;
            if (Vector3.Distance(transform.position, waypoints[idx].position) > speedLookaheadDistance) continue;
            best = Mathf.Min(best, GetWaypointSpeed(idx));
        }

        return Mathf.Min(best, maxSpeedKmh);
    }

    float GetWaypointSpeed(int index)
    {
        if (waypointSpeeds == null || index < 0 || index >= waypointSpeeds.Length)
            return maxSpeedKmh;
        return waypointSpeeds[index];
    }

    void HandleSpeed()
    {
        float speedKmh = rb != null ? rb.linearVelocity.magnitude * 3.6f : 0f;

        if (recovering)
        {
            // Back away from whatever is blocking us. Steer ONLY away from
            // obstacles/walls — never toward the waypoint, which could keep the
            // nose pressed against the barrier while reversing.
            float recoverySteer = Mathf.Clamp(avoidanceSteer + boundarySteer, -1f, 1f);
            car.SetInput(recoveryThrottle, 0f, recoverySteer);
            return;
        }

        float targetSpeed = GetTargetSpeed();

        if (obstacleAhead && frontObstacleDistance < float.MaxValue)
        {
            float urgency = 1f - Mathf.Clamp01(frontObstacleDistance / frontSensorLength);
            targetSpeed = Mathf.Min(targetSpeed, Mathf.Lerp(targetSpeed, slowSpeedKmh, urgency));
        }

        float throttle = 0f;
        float brake = 0f;

        if (speedKmh < targetSpeed - 1f)
        {
            throttle = 1f;
        }
        else if (speedKmh > targetSpeed + 1f)
        {
            brake = Mathf.Clamp01((speedKmh - targetSpeed) / Mathf.Max(brakeResponse, 1f));
            brake = Mathf.Max(brake, 0.25f);
        }
        else
        {
            throttle = 0.35f;
        }

        if (obstacleAhead && frontObstacleDistance < brakeObstacleDistance && speedKmh > slowSpeedKmh)
        {
            throttle = 0f;
            brake = Mathf.Max(brake, 0.85f);
        }

        car.SetInput(throttle, brake, currentSteer);
    }

    public void ResetForNewRace()
    {
        currentWaypointIndex = 0;
        currentSteer = 0f;
        obstacleAhead = false;
        frontObstacleDistance = float.MaxValue;
        avoidanceSteer = 0f;
        boundarySteer = 0f;
        stuckTimer = 0f;
        recoveryTimer = 0f;
        recovering = false;
        recoveryAttempts = 0;
        normalDriveTimer = 0f;
        finishStopLogged = false;
        CacheWaypointSpeeds();
    }
}
