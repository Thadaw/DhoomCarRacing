using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class AITrackGenerator : MonoBehaviour
{
    [Header("Track Settings")]
    public SimpleTrackGenerator.TrackShape trackShape = SimpleTrackGenerator.TrackShape.Oval;
    public float trackRadius = 120f;
    public float straightLength = 150f;
    public int resolution = 160;
    public float cornerRadius = 40f;
    public float roadWidth = 20f;

    [Header("AI Path Settings")]
    public int waypointCount = 30;
    public float leftLateralOffset = -3f;
    public float centerLateralOffset = 0f;
    public float rightLateralOffset = 3f;
    public float defaultSpeedKmh = 85f;
    public float cornerSpeedKmh = 40f;
    public float straightSpeedKmh = 100f;

    public static List<Transform>[] PerCarWaypoints { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "aioponent") return;
        if (GameSession.Instance == null || GameSession.Instance.CurrentMode != GameSession.GameMode.AI) return;

        GameObject go = new GameObject("AITrackGenerator");
        go.AddComponent<AITrackGenerator>();
    }

    private void Awake()
    {
        DisableExistingMap();
        GenerateNewTrack();
        RepositionCarSpawner();
        GenerateCheckpoints();
        GeneratePerCarWaypoints();
        EnsureRaceManagerUI();
        StartCoroutine(ForceStartRaceNextFrame());
    }

    private IEnumerator ForceStartRaceNextFrame()
    {
        yield return null;
        yield return null;
        yield return null;

        RaceManager rm = RaceManager.Instance;
        if (rm != null && !rm.raceStarted)
        {
            rm.StopAllCoroutines();
            rm.raceStarted = true;
            rm.raceStartTime = Time.time;
            Debug.Log("AITrackGenerator: Race force-started");
        }
    }

    private void DisableExistingMap()
    {
        string[] mapNames = { "map3", "Map3", "RealisticRaceTrack", "RealisticRaceTrack(Clone)" };
        foreach (string name in mapNames)
        {
            GameObject mapObj = GameObject.Find(name);
            if (mapObj != null) mapObj.SetActive(false);
        }

        GameObject[] allObjs = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in allObjs)
        {
            if (obj.transform.parent == null &&
                (obj.name.StartsWith("Road_Part") || obj.name.Contains("RaceTrack") || obj.name.Contains("Tent") || obj.name.Contains("Water")))
            {
                obj.SetActive(false);
            }
        }

        RaceCheckpoint[] allCheckpoints = FindObjectsByType<RaceCheckpoint>(FindObjectsSortMode.None);
        foreach (RaceCheckpoint cp in allCheckpoints)
            cp.gameObject.SetActive(false);
    }

    private void GenerateNewTrack()
    {
        GameObject genObj = new GameObject("GeneratedTrack");
        genObj.transform.position = Vector3.zero;

        SimpleTrackGenerator generator = genObj.AddComponent<SimpleTrackGenerator>();
        generator.trackShape = trackShape;
        generator.trackRadius = trackRadius;
        generator.straightLength = straightLength;
        generator.resolution = resolution;
        generator.roadWidth = roadWidth;
        generator.cornerRadius = cornerRadius;
        generator.generateOnStart = false;
        generator.borderHeight = 0.3f;
        generator.borderWidth = 1f;
        generator.Generate();

        // Disable checkpoints that SimpleTrackGenerator creates
        RaceCheckpoint[] genCheckpoints = FindObjectsByType<RaceCheckpoint>(FindObjectsSortMode.None);
        foreach (RaceCheckpoint cp in genCheckpoints)
            cp.gameObject.SetActive(false);

        Debug.Log($"AITrackGenerator: Generated {trackShape} track");
    }

    private void RepositionCarSpawner()
    {
        CarSpawner spawner = FindFirstObjectByType<CarSpawner>();
        if (spawner == null) return;

        SimpleTrackGenerator gen = FindFirstObjectByType<SimpleTrackGenerator>();
        if (gen == null) return;

        List<Vector3> points = gen.GetPathPoints();
        if (points.Count < 2) return;

        Vector3 startPoint = points[0];
        Vector3 secondPoint = points[1];
        Vector3 forward = (secondPoint - startPoint).normalized;
        forward.y = 0;
        forward.Normalize();

        spawner.transform.position = new Vector3(startPoint.x, 0.5f, startPoint.z);
        spawner.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);

        Debug.Log($"AITrackGenerator: CarSpawner repositioned to {spawner.transform.position}");
    }

    private void GeneratePerCarWaypoints()
    {
        SimpleTrackGenerator gen = FindFirstObjectByType<SimpleTrackGenerator>();
        if (gen == null) return;

        List<Vector3> pathPoints = gen.GetPathPoints();
        if (pathPoints.Count < 2) return;

        float[] lateralOffsets = { leftLateralOffset, centerLateralOffset, rightLateralOffset };
        float[] speeds = { defaultSpeedKmh - 10f, defaultSpeedKmh, defaultSpeedKmh + 10f };
        string[] carNames = { "AI Player 1", "AI Player 2", "AI Player 3" };

        PerCarWaypoints = new List<Transform>[3];

        for (int car = 0; car < 3; car++)
        {
            GameObject wpParent = new GameObject($"Waypoints_{carNames[car]}");
            PerCarWaypoints[car] = new List<Transform>();

            float offset = lateralOffsets[car];
            int step = Mathf.Max(1, pathPoints.Count / waypointCount);

            for (int i = 0; i < pathPoints.Count; i += step)
            {
                Vector3 pos = pathPoints[i];

                // Calculate lateral offset direction (perpendicular to track)
                int nextI = (i + 1) % pathPoints.Count;
                Vector3 forward = (pathPoints[nextI] - pathPoints[i]).normalized;
                Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

                pos += right * offset;
                pos.y = 0.5f;

                GameObject wpObj = new GameObject($"WP_{car}_{PerCarWaypoints[car].Count}");
                wpObj.transform.SetParent(wpParent.transform);
                wpObj.transform.position = pos;

                // Face toward next waypoint
                int nextIdx = (i + step) % pathPoints.Count;
                Vector3 nextPos = pathPoints[nextIdx] + right * offset;
                Vector3 dir = (nextPos - pos);
                dir.y = 0;
                if (dir.sqrMagnitude > 0.01f)
                    wpObj.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);

                AIWaypoint wp = wpObj.AddComponent<AIWaypoint>();

                // Speed based on curvature
                int prev = (i - step + pathPoints.Count) % pathPoints.Count;
                Vector3 toPrev = (pathPoints[prev] - pathPoints[i]).normalized;
                Vector3 toNext = (pathPoints[nextIdx] - pathPoints[i]).normalized;
                toPrev.y = 0; toNext.y = 0;
                float angle = Vector3.Angle(toPrev, toNext);
                float curvature = angle / 180f;
                wp.targetSpeedKmh = Mathf.Lerp(speeds[car], cornerSpeedKmh, curvature);

                PerCarWaypoints[car].Add(wpObj.transform);
            }

            Debug.Log($"AITrackGenerator: {carNames[car]} - {PerCarWaypoints[car].Count} waypoints, lateral offset={offset}m, speed={speeds[car]}km/h");
        }
    }

    private void GenerateCheckpoints()
    {
        SimpleTrackGenerator gen = FindFirstObjectByType<SimpleTrackGenerator>();
        if (gen == null) return;

        List<Vector3> pathPoints = gen.GetPathPoints();
        if (pathPoints.Count < 2) return;

        int checkpointCount = 10;
        int step = Mathf.Max(1, pathPoints.Count / (checkpointCount + 1));

        GameObject cpParent = new GameObject("Checkpoints");

        for (int i = 0; i < checkpointCount; i++)
        {
            int idx = ((i + 1) * step) % pathPoints.Count;
            Vector3 pos = pathPoints[idx];
            pos.y = 1.5f;

            int nextIdx = (idx + step) % pathPoints.Count;
            Vector3 forward = (pathPoints[nextIdx] - pathPoints[idx]).normalized;
            forward.y = 0;

            GameObject cpObj = new GameObject($"CP_{i}");
            cpObj.transform.SetParent(cpParent.transform);
            cpObj.transform.position = pos;
            if (forward.sqrMagnitude > 0.01f)
                cpObj.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);

            BoxCollider col = cpObj.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(roadWidth + 4f, 4f, 2f);

            RaceCheckpoint cp = cpObj.AddComponent<RaceCheckpoint>();
            cp.checkpointIndex = i;
            cp.isFinishLine = false;
        }

        // Finish line at start
        Vector3 finishPos = pathPoints[0];
        finishPos.y = 1.5f;
        Vector3 finishForward = (pathPoints[1] - pathPoints[0]).normalized;
        finishForward.y = 0;

        GameObject finishObj = new GameObject("FinishLine");
        finishObj.transform.SetParent(cpParent.transform);
        finishObj.transform.position = finishPos;
        if (finishForward.sqrMagnitude > 0.01f)
            finishObj.transform.rotation = Quaternion.LookRotation(finishForward, Vector3.up);

        BoxCollider finishCol = finishObj.AddComponent<BoxCollider>();
        finishCol.isTrigger = true;
        finishCol.size = new Vector3(roadWidth + 4f, 4f, 2f);

        RaceCheckpoint finishCP = finishObj.AddComponent<RaceCheckpoint>();
        finishCP.checkpointIndex = 0;
        finishCP.isFinishLine = true;

        Debug.Log($"AITrackGenerator: Created {checkpointCount} checkpoints + finish line");
    }

    private void EnsureRaceManagerUI()
    {
        RaceManager rm = FindFirstObjectByType<RaceManager>();
        if (rm == null)
        {
            GameObject rmObj = new GameObject("RaceManager");
            rm = rmObj.AddComponent<RaceManager>();
        }

        if (rm.countdownText == null)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGO = new GameObject("Canvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }

            GameObject countdownGO = new GameObject("CountdownText");
            countdownGO.transform.SetParent(canvas.transform, false);

            RectTransform rt = countdownGO.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(400, 200);

            TMPro.TextMeshProUGUI tmp = countdownGO.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.text = "";
            tmp.fontSize = 120;
            tmp.color = Color.white;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;

            rm.countdownText = tmp;

            CanvasGroup cg = countdownGO.AddComponent<CanvasGroup>();
            rm.countdownCanvasGroup = cg;
        }
    }
}
