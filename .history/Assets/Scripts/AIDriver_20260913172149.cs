using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class AIDriver : MonoBehaviour
{
    [Header("AI Racing")]
    public float waypointReachDist = 12f;
    public float stuckTimeout = 2f;
    public float reverseTime = 1.5f;
    public float turnTime = 2f;
    public float roadWidth = 6f;

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

    private enum State { Wait, Drive, Reverse, Turn }
    private State state = State.Wait;
    private float stateTimer = 0f;
    private Vector3 lastPos;
    private float stuckTime = 0f;
    private int stuckCount = 0;
    private float turnDir = 1f;

    private List<RaceCheckpoint> sortedCheckpoints;
    private int totalCheckpoints = 0;
    private int nextCP = 0;

    private List<Vector3> roadWaypoints = new List<Vector3>();
    private int roadWaypointIndex = 0;

    private float aiMaxSpeed;
    private float aiMotorForce;
    private float aiBrakeForce;
    private int startFrames = 0;

    private const float CHECKPOINT_SUBDIVIDE_STEP = 15f;
    private const int MIN_CHECKPOINT_WAYPOINTS = 3;

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
        aiMaxSpeed += variation;

        lastPos = transform.position;
        CacheCheckpoints();
        DetectRoadPath();
        nextCP = 0;

        if (tracker != null)
        {
            nextCP = tracker.nextCheckpointIndex;
        }

        FindNearestWaypoint();
        Debug.Log($"AI '{aiName}': checkpoints={totalCheckpoints}, roadWaypoints={roadWaypoints.Count}, maxSpeed={aiMaxSpeed:F0}, skill={skillLevel}");
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

    private void DetectRoadPath()
    {
        roadWaypoints.Clear();
        BuildCheckpointsAsRoadPath();

        if (roadWaypoints.Count >= MIN_CHECKPOINT_WAYPOINTS)
        {
            Debug.Log($"AIDriver: Using checkpoint-based road path ({roadWaypoints.Count} points)");
            return;
        }

        Debug.LogWarning("AIDriver: Not enough checkpoints, falling back to scene geometry.");

        if (TryDetectFromRoadWaypointsParent()) return;
        if (TryDetectFromRoadParts()) return;
        if (TryDetectFromTrackModel()) return;

        Debug.LogWarning("AIDriver: No usable road path found!");
    }

    private void BuildCheckpointsAsRoadPath()
    {
        roadWaypoints.Clear();
        if (sortedCheckpoints == null || sortedCheckpoints.Count < 2) return;

        int n = sortedCheckpoints.Count;
        for (int i = 0; i < n; i++)
        {
            Vector3 current = sortedCheckpoints[i].transform.position;
            Vector3 next = sortedCheckpoints[(i + 1) % n].transform.position;

            roadWaypoints.Add(current);

            int subdivideCount = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(current, next) / CHECKPOINT_SUBDIVIDE_STEP));
            for (int s = 1; s < subdivideCount; s++)
            {
                float t = (float)s / subdivideCount;
                roadWaypoints.Add(Vector3.Lerp(current, next, t));
            }
        }

        Debug.Log($"AIDriver: Built {roadWaypoints.Count} road waypoints from {n} checkpoints");
    }

    private bool TryDetectFromRoadWaypointsParent()
    {
        GameObject wpParent = GameObject.Find("RoadWaypoints");
        if (wpParent == null) return false;

        List<KeyValuePair<int, Transform>> indexed = new List<KeyValuePair<int, Transform>>();
        foreach (Transform child in wpParent.transform)
        {
            if (!child.gameObject.activeInHierarchy) continue;
            int index = 0;
            Match match = Regex.Match(child.name, @"\d+");
            if (match.Success) index = int.Parse(match.Value);
            indexed.Add(new KeyValuePair<int, Transform>(index, child));
        }
        indexed.Sort((a, b) => a.Key.CompareTo(b.Key));

        roadWaypoints.Clear();
        foreach (var pair in indexed)
            roadWaypoints.Add(pair.Value.position);

        Debug.Log("AIDriver: Found " + roadWaypoints.Count + " waypoints from RoadWaypoints parent");
        return roadWaypoints.Count > 0;
    }

    private bool TryDetectFromRoadParts()
    {
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        List<GameObject> roadParts = new List<GameObject>();

        foreach (GameObject obj in allObjects)
        {
            if (obj.name.StartsWith("Road_Part"))
                roadParts.Add(obj);
        }

        if (roadParts.Count == 0) return false;

        List<KeyValuePair<int, GameObject>> indexed = new List<KeyValuePair<int, GameObject>>();
        foreach (GameObject part in roadParts)
        {
            int index = 0;
            Match match = Regex.Match(part.name, @"\d+");
            if (match.Success) index = int.Parse(match.Value);
            indexed.Add(new KeyValuePair<int, GameObject>(index, part));
        }
        indexed.Sort((a, b) => a.Key.CompareTo(b.Key));

        roadWaypoints.Clear();
        foreach (var pair in indexed)
        {
            Renderer renderer = pair.Value.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                Bounds bounds = renderer.bounds;
                roadWaypoints.Add(new Vector3(bounds.center.x, bounds.center.y + 1f, bounds.center.z));
            }
            else
            {
                roadWaypoints.Add(pair.Value.transform.position + Vector3.up * 1f);
            }
        }

        Debug.Log("AIDriver: Found " + roadWaypoints.Count + " road waypoints from Road_Part objects");
        return roadWaypoints.Count > 0;
    }

    private bool TryDetectFromTrackModel()
    {
        string[] trackNames = { "Track", "RealisticRaceTrack", "Track (1)", "RaceTrack", "RaceTrackExport", "map3", "Map3" };
        GameObject trackModel = null;

        foreach (string name in trackNames)
        {
            trackModel = GameObject.Find(name);
            if (trackModel != null) break;
        }

        if (trackModel == null) return false;

        string[] excludePatterns = {
            "WALL", "GRASS", "BANNER", "TENT", "SIGN", "TOWER", "TRIBUENE",
            "LAMP", "PIT", "FLAG", "BRIDGE", "KERB", "BUILDING", "OFFTRACK",
            "AD_", "EMIRATES", "ROLEX", "SHELL", "UBS", "PAVILLION",
            "COMMENTATOR", "FOOD", "RUNOFF", "PERSON", "SMALL_METAL",
            "SMALL01", "SMALL03", "TIRES", "HIGH_CONCRETE", "03_EMIRATES",
            "08_ROLEX", "09_", "17_ROLEX"
        };

        List<KeyValuePair<int, Transform>> indexed = new List<KeyValuePair<int, Transform>>();
        foreach (Transform child in trackModel.transform)
        {
            if (child.GetComponentInChildren<Renderer>() == null) continue;

            string childName = child.name.ToUpper();
            bool excluded = false;
            foreach (string pattern in excludePatterns)
            {
                if (childName.Contains(pattern))
                {
                    excluded = true;
                    break;
                }
            }
            if (excluded) continue;

            int index = 0;
            Match match = Regex.Match(child.name, @"\d+");
            if (match.Success) index = int.Parse(match.Value);
            indexed.Add(new KeyValuePair<int, Transform>(index, child));
        }
        indexed.Sort((a, b) => a.Key.CompareTo(b.Key));

        roadWaypoints.Clear();
        foreach (var pair in indexed)
        {
            Renderer renderer = pair.Value.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                Bounds bounds = renderer.bounds;
                roadWaypoints.Add(new Vector3(bounds.center.x, bounds.center.y + 1f, bounds.center.z));
            }
            else
            {
                roadWaypoints.Add(pair.Value.transform.position + Vector3.up * 1f);
            }
        }

        if (roadWaypoints.Count > 0)
            Debug.Log("AIDriver: Found " + roadWaypoints.Count + " road waypoints from Track model children");

        return roadWaypoints.Count > 0;
    }

    private void FindNearestWaypoint()
    {
        if (roadWaypoints.Count == 0) return;

        Vector3 fwd = transform.forward; fwd.y = 0; fwd.Normalize();
        float bestForwardScore = float.MinValue;
        int bestForwardIndex = -1;
        float bestDist = float.MaxValue;
        int bestDistIndex = 0;

        for (int i = 0; i < roadWaypoints.Count; i++)
        {
            Vector3 toWP = roadWaypoints[i] - transform.position;
            toWP.y = 0;
            float dist = toWP.magnitude;
            if (dist < 0.1f) continue;

            if (dist < bestDist)
            {
                bestDist = dist;
                bestDistIndex = i;
            }

            float dot = Vector3.Dot(fwd, toWP.normalized);
            float score = dot * 100f - dist;

            if (score > bestForwardScore)
            {
                bestForwardScore = score;
                bestForwardIndex = i;
            }
        }

        if (bestForwardIndex >= 0 && bestForwardScore > 0f)
        {
            roadWaypointIndex = bestForwardIndex;
        }
        else
        {
            roadWaypointIndex = bestDistIndex;
            Debug.Log($"AI: No forward waypoint found, using nearest at index {bestDistIndex}");
        }

        for (int i = 0; i < 3; i++)
        {
            Vector3 toWP = roadWaypoints[roadWaypointIndex % roadWaypoints.Count] - transform.position;
            toWP.y = 0;
            if (toWP.sqrMagnitude < waypointReachDist * waypointReachDist && Vector3.Dot(fwd, toWP.normalized) < 0f)
            {
                roadWaypointIndex = (roadWaypointIndex + 1) % roadWaypoints.Count;
            }
            else break;
        }
    }

    private Vector3 GetRecoveryTarget()
    {
        if (roadWaypoints.Count > 0)
        {
            float bestDist = float.MaxValue;
            Vector3 bestPos = roadWaypoints[0];
            Vector3 fwd = transform.forward; fwd.y = 0; fwd.Normalize();

            for (int i = 0; i < roadWaypoints.Count; i++)
            {
                Vector3 toWP = roadWaypoints[i] - transform.position;
                toWP.y = 0;
                float d = toWP.magnitude;
                float dot = Vector3.Dot(fwd, toWP.normalized);
                float adjustedDist = d * (1f - dot * 0.3f);

                if (adjustedDist < bestDist)
                {
                    bestDist = adjustedDist;
                    bestPos = roadWaypoints[i];
                }
            }
            return bestPos;
        }

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
            FindNearestWaypoint();

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            Debug.Log($"AI '{tracker?.aiName}': READY - starting drive from waypoint {roadWaypointIndex}, forward={transform.forward}");
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

        if (tracker != null && tracker.aiName != "" && nextCP != tracker.nextCheckpointIndex)
        {
            nextCP = tracker.nextCheckpointIndex;
        }

        switch (state)
        {
            case State.Drive: DoDrive(); break;
            case State.Reverse: DoReverse(); break;
            case State.Turn: DoTurn(); break;
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

    private Vector3 GetSteerPoint()
    {
        if (roadWaypoints.Count > 0)
        {
            return GetRoadSteerPoint();
        }
        return GetCheckpointSteerPoint();
    }

    private Vector3 GetRoadSteerPoint()
    {
        Vector3 currentPos = transform.position;
        float speedKmh = rb.linearVelocity.magnitude * 3.6f;
        float reachDist = Mathf.Lerp(waypointReachDist, waypointReachDist * 1.3f, speedKmh / 120f);

        Vector3 currentWP = roadWaypoints[roadWaypointIndex % roadWaypoints.Count];

        for (int i = 0; i < 3; i++)
        {
            float dist = Vector3.Distance(currentPos, currentWP);
            if (dist < reachDist)
            {
                int nextIndex = (roadWaypointIndex + 1) % roadWaypoints.Count;
                if (nextIndex <= roadWaypointIndex && roadWaypoints.Count > 10)
                {
                    break;
                }
                roadWaypointIndex = nextIndex;
                currentWP = roadWaypoints[roadWaypointIndex % roadWaypoints.Count];
            }
            else break;
        }

        float lookAheadDistance = Mathf.Lerp(8f, 20f, speedKmh / 120f);
        Vector3 steerPoint = currentWP;
        Vector3 prevPoint = currentWP;
        float accumulated = 0f;

        for (int i = 1; i <= roadWaypoints.Count && accumulated < lookAheadDistance; i++)
        {
            int wpIdx = (roadWaypointIndex + i) % roadWaypoints.Count;
            Vector3 nextPoint = roadWaypoints[wpIdx];
            accumulated += Vector3.Distance(prevPoint, nextPoint);
            steerPoint = nextPoint;
            prevPoint = nextPoint;
        }

        RaceCheckpoint cp = GetCurrentTarget();
        if (cp != null)
        {
            float distToCheckpoint = Vector3.Distance(currentPos, cp.transform.position);
            if (distToCheckpoint < 20f)
            {
                float blend = Mathf.Clamp01(1f - distToCheckpoint / 20f) * 0.35f;
                steerPoint = Vector3.Lerp(steerPoint, cp.transform.position, blend);
            }
        }

        return steerPoint;
    }

    private Vector3 GetCheckpointSteerPoint()
    {
        RaceCheckpoint target = GetCurrentTarget();
        if (target == null)
        {
            if (roadWaypoints.Count > 0)
                return roadWaypoints[roadWaypointIndex % roadWaypoints.Count];
            return transform.position + transform.forward * 50f;
        }
        return target.transform.position;
    }

    private void DoDrive()
    {
        Vector3 steerPoint = GetSteerPoint();

        Vector3 dirToTarget = steerPoint - transform.position;
        dirToTarget.y = 0f;

        if (dirToTarget.sqrMagnitude < 0.01f)
        {
            if (roadWaypoints.Count > 0)
            {
                roadWaypointIndex = (roadWaypointIndex + 1) % roadWaypoints.Count;
                steerPoint = roadWaypoints[roadWaypointIndex % roadWaypoints.Count];
                dirToTarget = steerPoint - transform.position;
                dirToTarget.y = 0f;
            }
            else
            {
                steerPoint = transform.position + transform.forward * 30f;
                dirToTarget = steerPoint - transform.position;
                dirToTarget.y = 0f;
            }
        }

        if (dirToTarget.sqrMagnitude < 0.01f)
        {
            car.SetInput(0.5f, 0f, 0f);
            return;
        }

        Vector3 fwd = transform.forward; fwd.y = 0; fwd.Normalize();
        Vector3 dirNorm = dirToTarget.normalized;
        float cross = Vector3.Cross(fwd, dirNorm).y;
        float angle = Vector3.Angle(fwd, dirNorm);

        float steerSmooth = Mathf.Lerp(8f, 4f, rb.linearVelocity.magnitude * 3.6f / 120f);
        steer = Mathf.Lerp(steer, Mathf.Clamp(cross, -1f, 1f), Time.fixedDeltaTime * steerSmooth);

        float speedKmh = rb.linearVelocity.magnitude * 3.6f;
        float throttle = 1f;
        float brake = 0f;

        if (startFrames > 0)
        {
            startFrames--;
            car.SetInput(1f, 0f, steer);
            lastPos = transform.position;
            return;
        }

        if (angle > 8f)
            throttle = Mathf.Lerp(1f, 0.35f, Mathf.Clamp01((angle - 8f) / 35f));

        if (angle > 30f && speedKmh > 25f)
        {
            float brakeAmount = Mathf.Lerp(0.4f, 0.7f, brakeSkill);
            brake = brakeAmount;
            throttle = 0.05f;
        }

        if (angle > 65f)
        {
            brake = Mathf.Lerp(0.6f, 0.85f, brakeSkill);
            throttle = 0f;
        }

        if (speedKmh > aiMaxSpeed)
        {
            throttle = 0f;
            brake = 0.15f;
        }

        if (tracker != null && tracker.raceCompleted)
        {
            throttle = 0f;
            brake = 0.5f;
        }

        car.SetInput(throttle, brake, steer);

        float moved = Vector3.Distance(transform.position, lastPos);
        float stuckThreshold = speedKmh > 10f ? 0.5f : 0.15f;
        if (moved < stuckThreshold)
        {
            stuckTime += Time.fixedDeltaTime;
            if (stuckTime > stuckTimeout)
            {
                stuckCount++;
                stuckTime = 0f;

                if (stuckCount >= 5)
                {
                    roadWaypointIndex = (roadWaypointIndex + 3) % Mathf.Max(1, roadWaypoints.Count);
                    nextCP = (nextCP + 1) % Mathf.Max(1, totalCheckpoints);
                    FindNearestWaypoint();

                    Vector3 pushDir = -transform.forward + transform.right * turnDir;
                    rb.linearVelocity = pushDir.normalized * 8f;

                    state = State.Drive;
                    stuckCount = 0;
                    Debug.Log($"AI '{tracker?.aiName}': FORCE UNSTUCK - skipped waypoint, pushed sideways");
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
        }
        else
        {
            stuckTime = Mathf.Max(0f, stuckTime - Time.fixedDeltaTime * 0.5f);
        }
        lastPos = transform.position;
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

        float reverseSteer = 0f;
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
            FindNearestWaypoint();
            Debug.Log($"AI '{tracker?.aiName}': facing road (angle={angle:F0}) -> drive");
            return;
        }

        if (stateTimer <= 0f)
        {
            state = State.Drive;
            stuckTime = 0f;
            stuckCount = 0;
            FindNearestWaypoint();
            Debug.Log($"AI '{tracker?.aiName}': turn timeout -> drive");
        }
        lastPos = transform.position;
    }
}
