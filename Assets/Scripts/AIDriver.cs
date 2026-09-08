using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class AIDriver : MonoBehaviour
{
    [Header("AI")]
    public float waypointReachDist = 12f;
    public float stuckTimeout = 2f;
    public float reverseTime = 1.5f;
    public float turnTime = 2f;

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

    public void Initialize(string aiName)
    {
        car = GetComponent<PhotonCarController>();
        rb = GetComponent<Rigidbody>();
        tracker = GetComponent<PlayerLapTracker>();

        if (car != null) car.useExternalInput = true;
        if (tracker != null) tracker.aiName = aiName;
        if (rb != null) { rb.isKinematic = false; rb.useGravity = true; }

        lastPos = transform.position;
        CacheCheckpoints();
        DetectRoadPath();
        nextCP = 0;
        FindNearestWaypoint();
        Debug.Log($"AI '{aiName}': checkpoints={totalCheckpoints}, roadWaypoints={roadWaypoints.Count}, pos={transform.position}");
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

        if (TryDetectFromRoadWaypointsParent()) return;
        if (TryDetectFromRoadParts()) return;
        if (TryDetectFromTrackModel()) return;
        if (TryDetectFromSceneObjects()) return;

        Debug.Log("AIDriver: No road objects found, using checkpoints only");
    }

    private bool TryDetectFromRoadWaypointsParent()
    {
        GameObject wpParent = GameObject.Find("RoadWaypoints");
        if (wpParent == null) return false;

        List<KeyValuePair<int, Transform>> indexed = new List<KeyValuePair<int, Transform>>();
        foreach (Transform child in wpParent.transform)
        {
            int index = 0;
            Match match = Regex.Match(child.name, @"\d+");
            if (match.Success) index = int.Parse(match.Value);
            indexed.Add(new KeyValuePair<int, Transform>(index, child));
        }
        indexed.Sort((a, b) => a.Key.CompareTo(b.Key));

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
        string[] trackNames = { "Track", "RealisticRaceTrack", "Track (1)", "RaceTrack", "RaceTrackExport" };
        GameObject trackModel = null;

        foreach (string name in trackNames)
        {
            trackModel = GameObject.Find(name);
            if (trackModel != null) break;
        }

        if (trackModel == null) return false;

        List<KeyValuePair<int, Transform>> indexed = new List<KeyValuePair<int, Transform>>();
        foreach (Transform child in trackModel.transform)
        {
            if (child.GetComponentInChildren<Renderer>() == null) continue;
            if (child.name.Contains("WALL") || child.name.Contains("GRASS") || child.name.Contains("BANNER")
                || child.name.Contains("TENT") || child.name.Contains("SIGN") || child.name.Contains("TOWER")
                || child.name.Contains("TRIBUENE") || child.name.Contains("LAMP") || child.name.Contains("PIT")
                || child.name.Contains("FLAG") || child.name.Contains("BRIDGE") || child.name.Contains("KERB"))
                continue;

            int index = 0;
            Match match = Regex.Match(child.name, @"\d+");
            if (match.Success) index = int.Parse(match.Value);
            indexed.Add(new KeyValuePair<int, Transform>(index, child));
        }
        indexed.Sort((a, b) => a.Key.CompareTo(b.Key));

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

    private bool TryDetectFromSceneObjects()
    {
        RaceCheckpoint[] cps = FindObjectsByType<RaceCheckpoint>(FindObjectsSortMode.None);
        if (cps.Length == 0) return false;

        List<KeyValuePair<int, Vector3>> indexed = new List<KeyValuePair<int, Vector3>>();
        foreach (RaceCheckpoint cp in cps)
        {
            indexed.Add(new KeyValuePair<int, Vector3>(cp.checkpointIndex, cp.transform.position));
        }
        indexed.Sort((a, b) => a.Key.CompareTo(b.Key));

        foreach (var pair in indexed)
            roadWaypoints.Add(pair.Value);

        Debug.Log("AIDriver: Using " + roadWaypoints.Count + " checkpoint positions as road waypoints");
        return roadWaypoints.Count > 0;
    }

    private void FindNearestWaypoint()
    {
        if (roadWaypoints.Count == 0) return;

        float bestDist = float.MaxValue;
        int bestIndex = 0;

        for (int i = 0; i < roadWaypoints.Count; i++)
        {
            float d = Vector3.Distance(transform.position, roadWaypoints[i]);
            if (d < bestDist)
            {
                bestDist = d;
                bestIndex = i;
            }
        }

        roadWaypointIndex = bestIndex;
    }

    private Vector3 GetRecoveryTarget()
    {
        if (roadWaypoints.Count > 0)
        {
            float bestDist = float.MaxValue;
            Vector3 bestPos = roadWaypoints[0];

            for (int i = 0; i < roadWaypoints.Count; i++)
            {
                float d = Vector3.Distance(transform.position, roadWaypoints[i]);
                if (d < bestDist)
                {
                    bestDist = d;
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
            StartCoroutine(DelayedInit());
    }

    private IEnumerator DelayedInit()
    {
        yield return new WaitForSeconds(0.5f);
        Initialize("AI");
    }

    private void Update()
    {
        if (ready) return;
        readyTimer += Time.deltaTime;
        if ((RaceManager.Instance != null && RaceManager.Instance.raceStarted) || readyTimer > 4f)
        {
            ready = true;
            state = State.Drive;
        }
    }

    private void FixedUpdate()
    {
        if (car == null) return;
        if (!ready) { car.SetInput(0f, 1f, 0f); return; }

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

    private void AdvanceCheckpoint()
    {
        nextCP = (nextCP + 1) % totalCheckpoints;
        Debug.Log($"AI '{tracker?.aiName}': CP -> {nextCP}");
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
        Vector3 currentWP = roadWaypoints[roadWaypointIndex % roadWaypoints.Count];
        float distToWP = Vector3.Distance(currentPos, currentWP);

        if (distToWP < waypointReachDist)
        {
            roadWaypointIndex = (roadWaypointIndex + 1) % roadWaypoints.Count;
            currentWP = roadWaypoints[roadWaypointIndex % roadWaypoints.Count];
        }

        int nextWPIndex = (roadWaypointIndex + 1) % roadWaypoints.Count;
        Vector3 nextWP = roadWaypoints[nextWPIndex];

        float lookAheadDist = Mathf.Clamp(rb.linearVelocity.magnitude * 3.6f / 30f, 0.2f, 1.0f);
        Vector3 steerPoint = Vector3.Lerp(currentWP, nextWP, lookAheadDist * 0.5f);

        RaceCheckpoint cp = GetCurrentTarget();
        if (cp != null)
        {
            float distToCheckpoint = Vector3.Distance(currentPos, cp.transform.position);
            if (distToCheckpoint < 25f)
            {
                steerPoint = Vector3.Lerp(steerPoint, cp.transform.position, 0.3f);
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

        steer = Mathf.Lerp(steer, Mathf.Clamp(cross, -1f, 1f), Time.fixedDeltaTime * 5f);

        float throttle = 1f;
        float brake = 0f;

        if (angle > 15f)
            throttle = Mathf.Lerp(1f, 0.4f, Mathf.Clamp01((angle - 15f) / 50f));

        if (angle > 45f && rb.linearVelocity.magnitude * 3.6f > 25f)
        {
            brake = 0.5f;
            throttle = 0.1f;
        }

        car.SetInput(throttle, brake, steer);

        float moved = Vector3.Distance(transform.position, lastPos);
        if (moved < 0.3f)
        {
            stuckTime += Time.fixedDeltaTime;
            if (stuckTime > stuckTimeout)
            {
                stuckCount++;
                state = State.Reverse;
                stateTimer = reverseTime;
                turnDir = GetTurnDirTowardRoad();
                stuckTime = 0f;
                Debug.Log($"AI '{tracker?.aiName}': STUCK #{stuckCount} -> reverse");
            }
        }
        else
        {
            stuckTime = 0f;
        }
        lastPos = transform.position;

        RaceCheckpoint cpTarget = GetCurrentTarget();
        if (cpTarget != null)
        {
            float distToCP = Vector3.Distance(transform.position, cpTarget.transform.position);
            if (distToCP < 15f)
            {
                AdvanceCheckpoint();
            }
        }
    }

    private float GetTurnDirTowardRoad()
    {
        Vector3 recoveryTarget = GetRecoveryTarget();
        Vector3 dirToTarget = recoveryTarget - transform.position;
        dirToTarget.y = 0f;

        Vector3 fwd = transform.forward; fwd.y = 0; fwd.Normalize();
        float cross = Vector3.Cross(fwd, dirToTarget.normalized).y;
        return cross > 0 ? 1f : -1f;
    }

    private void DoReverse()
    {
        Vector3 recoveryTarget = GetRecoveryTarget();
        Vector3 dirToTarget = recoveryTarget - transform.position;
        dirToTarget.y = 0f;

        Vector3 fwd = transform.forward; fwd.y = 0; fwd.Normalize();
        dirToTarget.Normalize();

        float reverseSteer = -Vector3.Cross(fwd, dirToTarget).y;
        steer = Mathf.Lerp(steer, Mathf.Clamp(reverseSteer, -1f, 1f), Time.fixedDeltaTime * 5f);

        car.SetInput(-0.7f, 0f, steer);
        stateTimer -= Time.fixedDeltaTime;

        float moved = Vector3.Distance(transform.position, lastPos);

        if (stateTimer <= 0f || moved > 3f)
        {
            state = State.Turn;
            stateTimer = turnTime;
            Debug.Log($"AI '{tracker?.aiName}': turning toward road...");
        }
        lastPos = transform.position;
    }

    private void DoTurn()
    {
        Vector3 recoveryTarget = GetRecoveryTarget();
        Vector3 dirToTarget = recoveryTarget - transform.position;
        dirToTarget.y = 0f;

        Vector3 fwd = transform.forward; fwd.y = 0; fwd.Normalize();
        dirToTarget.Normalize();

        float cross = Vector3.Cross(fwd, dirToTarget).y;
        float angle = Vector3.Angle(fwd, dirToTarget);

        steer = Mathf.Lerp(steer, Mathf.Clamp(cross, -1f, 1f), Time.fixedDeltaTime * 5f);

        float throttle = angle > 20f ? 0.3f : 0.7f;
        car.SetInput(throttle, 0f, steer);

        stateTimer -= Time.fixedDeltaTime;

        if (angle < 30f)
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
