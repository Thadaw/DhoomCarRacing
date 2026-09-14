using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AIDriver : MonoBehaviour
{
    [Header("AI Identity")]
    public string aiName = "";
    public int aiIndex = 0;

    [Header("Waypoint Path")]
    public Transform[] waypoints;
    public int currentWaypointIndex = 0;
    public float waypointReachDistance = 8f;

    [Header("Speed")]
    public float maxSpeedKmh = 85f;
    public float slowSpeedKmh = 35f;

    [Header("Steering")]
    public float steerSensitivity = 4f;

    [Header("Sensors")]
    public float frontSensorLength = 10f;
    public float sideSensorLength = 4f;
    public float sensorHeight = 0.6f;
    public float sensorSideOffset = 0.75f;
    public float avoidanceSteerStrength = 0.6f;
    public LayerMask sensorLayers = ~0;

    [Header("Road Boundary")]
    public float roadBoundarySensorWidth = 12f;
    public float roadBoundaryRayLength = 3f;
    public float boundaryRecoverSteer = 0.8f;

    private PhotonCarController car;
    private Rigidbody rb;
    private PlayerLapTracker tracker;
    private float currentSteer = 0f;
    private bool obstacleAhead = false;
    private float avoidanceSteer = 0f;
    private float boundarySteer = 0f;

    public void Initialize(string name)
    {
        aiName = name;
        car = GetComponent<PhotonCarController>();
        rb = GetComponent<Rigidbody>();
        tracker = GetComponent<PlayerLapTracker>();

        if (tracker != null)
        {
            tracker.aiName = aiName;
            tracker.totalLaps = GameSession.Instance != null ? GameSession.Instance.TotalLaps : 1;
        }

        if (car != null)
        {
            car.useExternalInput = true;
        }

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

    void FixedUpdate()
    {
        if (car == null || waypoints == null || waypoints.Length == 0) return;

        if (RaceManager.Instance != null && !RaceManager.Instance.raceStarted)
        {
            car.SetInput(0f, 0.8f, 0f);
            return;
        }

        HandleWaypointProgress();
        HandleSensors();
        HandleRoadBoundary();
        HandleSteering();
        HandleSpeed();
    }

    void HandleWaypointProgress()
    {
        Transform target = waypoints[currentWaypointIndex];
        float distance = Vector3.Distance(transform.position, target.position);

        if (distance < waypointReachDistance)
        {
            currentWaypointIndex++;
            if (currentWaypointIndex >= waypoints.Length)
                currentWaypointIndex = 0;
        }
    }

    void HandleSensors()
    {
        obstacleAhead = false;
        avoidanceSteer = 0f;

        Vector3 origin = transform.position + Vector3.up * sensorHeight;
        Vector3 fwd = transform.forward;
        Vector3 leftOrigin = origin - transform.right * sensorSideOffset;
        Vector3 rightOrigin = origin + transform.right * sensorSideOffset;

        bool centerHit = Physics.Raycast(origin, fwd, out RaycastHit hitCenter, frontSensorLength, sensorLayers);
        bool leftHit = Physics.Raycast(leftOrigin, fwd, out RaycastHit hitLeft, frontSensorLength, sensorLayers);
        bool rightHit = Physics.Raycast(rightOrigin, fwd, out RaycastHit hitRight, frontSensorLength, sensorLayers);
        bool sideLeftHit = Physics.Raycast(origin, -transform.right, out RaycastHit hitSideLeft, sideSensorLength, sensorLayers);
        bool sideRightHit = Physics.Raycast(origin, transform.right, out RaycastHit hitSideRight, sideSensorLength, sensorLayers);

        if (centerHit) obstacleAhead = true;
        if (leftHit) { obstacleAhead = true; avoidanceSteer += avoidanceSteerStrength; }
        if (rightHit) { obstacleAhead = true; avoidanceSteer -= avoidanceSteerStrength; }
        if (sideLeftHit) avoidanceSteer += avoidanceSteerStrength;
        if (sideRightHit) avoidanceSteer -= avoidanceSteerStrength;

        avoidanceSteer = Mathf.Clamp(avoidanceSteer, -1f, 1f);
    }

    void HandleRoadBoundary()
    {
        boundarySteer = 0f;

        Vector3 origin = transform.position + Vector3.up * 0.5f;

        // Cast down-left and down-right to detect road surface
        bool leftOnRoad = Physics.Raycast(origin - transform.right * roadBoundarySensorWidth * 0.5f, Vector3.down, out RaycastHit leftHit, roadBoundaryRayLength, sensorLayers);
        bool rightOnRoad = Physics.Raycast(origin + transform.right * roadBoundarySensorWidth * 0.5f, Vector3.down, out RaycastHit rightHit, roadBoundaryRayLength, sensorLayers);

        if (leftOnRoad && !rightOnRoad)
        {
            // Right side is off road — steer left
            boundarySteer = -boundaryRecoverSteer;
        }
        else if (!leftOnRoad && rightOnRoad)
        {
            // Left side is off road — steer right
            boundarySteer = boundaryRecoverSteer;
        }
        else if (!leftOnRoad && !rightOnRoad)
        {
            // Both sides off road — steer toward nearest waypoint
            Transform target = waypoints[currentWaypointIndex];
            Vector3 localTarget = transform.InverseTransformPoint(target.position);
            boundarySteer = Mathf.Clamp(localTarget.x / Mathf.Max(localTarget.magnitude, 1f), -1f, 1f);
        }
    }

    void HandleSteering()
    {
        Transform target = waypoints[currentWaypointIndex];
        Vector3 localTarget = transform.InverseTransformPoint(target.position);
        float pathSteer = Mathf.Clamp(localTarget.x / localTarget.magnitude, -1f, 1f);
        float steerInput = Mathf.Clamp(pathSteer + avoidanceSteer + boundarySteer, -1f, 1f);
        currentSteer = Mathf.Lerp(currentSteer, steerInput, Time.fixedDeltaTime * steerSensitivity);
        currentSteer = Mathf.Clamp(currentSteer, -1f, 1f);
    }

    void HandleSpeed()
    {
        float speedKmh = rb != null ? rb.linearVelocity.magnitude * 3.6f : 0f;

        float targetSpeed = maxSpeedKmh;
        AIWaypoint wp = waypoints[currentWaypointIndex]?.GetComponent<AIWaypoint>();
        if (wp != null)
            targetSpeed = wp.targetSpeedKmh;

        if (obstacleAhead)
            targetSpeed = Mathf.Min(targetSpeed, slowSpeedKmh);

        float throttle = 0f;
        float brake = 0f;

        if (speedKmh < targetSpeed)
        {
            throttle = 1f;
            brake = 0f;
        }
        else
        {
            throttle = 0f;
            brake = 0.45f;
        }

        if (obstacleAhead && speedKmh > slowSpeedKmh)
        {
            throttle = 0f;
            brake = 0.8f;
        }

        car.SetInput(throttle, brake, currentSteer);
    }

    public void ResetForNewRace()
    {
        currentWaypointIndex = 0;
        currentSteer = 0f;
        obstacleAhead = false;
        avoidanceSteer = 0f;
        boundarySteer = 0f;
    }
}
