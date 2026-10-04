using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class AITrackGenerator : MonoBehaviour
{
    [Header("Track Settings")]
    public SimpleTrackGenerator.TrackShape trackShape = SimpleTrackGenerator.TrackShape.Oval;
    public float trackRadius = 120f;
    public float straightLength = 150f;
    public int resolution = 160;
    public float cornerRadius = 40f;
    public float roadWidth = 28f;

    [Header("AI Path Settings")]
    public int waypointCount = 60;
    public float leftLateralOffset = -5f;
    public float centerLateralOffset = 0f;
    public float rightLateralOffset = 5f;
    public float defaultSpeedKmh = 145f;
    public float cornerSpeedKmh = 55f;
    public float straightSpeedKmh = 170f;
    [Tooltip("Lateral acceleration the AI may use in corners (m/s^2). Higher = faster cornering, but too high makes them slide off.")]
    public float maxLateralAcceleration = 11.5f;

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
        // AI races are always a single lap: cross the finish line once to end the game.
        if (GameSession.Instance != null)
            GameSession.Instance.TotalLaps = 1;

        DisableExistingMap();
        GenerateNewTrack();
        RepositionCarSpawner();
        GenerateCheckpoints();
        GenerateFinishLineVisual();
        GeneratePerCarWaypoints();
        EnsureRaceManagerUI();
        // The regular RaceManager countdown (3 - 2 - 1 - GO) is left untouched now.
        // It used to be force-killed after 3 frames, which skipped the countdown and
        // released the cars immediately; cars stay locked until it reaches 0.
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
        // Dashed white centre line down the middle of the AI track.
        generator.drawCenterLine = true;
        generator.centerLineColor = Color.white;
        // Solid track-side barrier: tall enough that no car can climb or bounce over it.
        generator.borderHeight = 1.4f;
        generator.borderWidth = 1.2f;
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

                // Corner speed from the actual turn radius instead of a blind lerp:
                //   R = arcLength / turnAngle,  v = sqrt(maxLatAccel * R)
                // Straights (huge R) simply get this car's top speed. This lets the AI
                // be fast on the straights while still braking for tight corners.
                int prev = (i - step + pathPoints.Count) % pathPoints.Count;
                Vector3 toPrev = pathPoints[prev] - pathPoints[i];
                Vector3 toNext = pathPoints[nextIdx] - pathPoints[i];
                toPrev.y = 0f;
                toNext.y = 0f;

                float radiusSpeedKmh = straightSpeedKmh;
                float angleRad = Vector3.Angle(toPrev, toNext) * Mathf.Deg2Rad;
                float arcLength = toPrev.magnitude + toNext.magnitude;
                if (angleRad > 0.0005f && arcLength > 0.01f)
                {
                    float radius = arcLength / angleRad;
                    radiusSpeedKmh = Mathf.Sqrt(maxLateralAcceleration * radius) * 3.6f;
                }

                float targetSpeed = Mathf.Min(speeds[car], radiusSpeedKmh);
                wp.targetSpeedKmh = Mathf.Clamp(targetSpeed, cornerSpeedKmh, speeds[car]);

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
            // 4m deep (was 2m) so a fast car can never tunnel past at high speed.
            col.size = new Vector3(roadWidth + 4f, 4f, 4f);

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
        finishCol.size = new Vector3(roadWidth + 4f, 4f, 4f);

        RaceCheckpoint finishCP = finishObj.AddComponent<RaceCheckpoint>();
        finishCP.checkpointIndex = 0;
        finishCP.isFinishLine = true;

        Debug.Log($"AITrackGenerator: Created {checkpointCount} checkpoints + finish line");
    }

    // Puts a visible checkered strip across the road at the start/finish point so
    // the finish line is unambiguous — it is exactly where the race begins and ends.
    private void GenerateFinishLineVisual()
    {
        SimpleTrackGenerator gen = FindFirstObjectByType<SimpleTrackGenerator>();
        if (gen == null) return;

        List<Vector3> points = gen.GetPathPoints();
        if (points.Count < 2) return;

        Vector3 p0 = points[0];
        Vector3 dir = points[1] - points[0];
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        dir.Normalize();

        GameObject finish = new GameObject("FinishLineVisual");
        finish.transform.position = new Vector3(p0.x, 0.17f, p0.z);
        finish.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

        MeshFilter mf = finish.AddComponent<MeshFilter>();
        MeshRenderer mr = finish.AddComponent<MeshRenderer>();

        float width = roadWidth;
        float depth = 3f;
        float cellSize = 1.5f;              // metres per black/white square
        float repeat = cellSize * 2f;       // texture repeat = 2 squares

        Mesh mesh = new Mesh();
        mesh.name = "FinishLineVisual";
        mesh.vertices = new Vector3[]
        {
            new Vector3(-width * 0.5f, 0f, -depth * 0.5f),
            new Vector3( width * 0.5f, 0f, -depth * 0.5f),
            new Vector3( width * 0.5f, 0f,  depth * 0.5f),
            new Vector3(-width * 0.5f, 0f,  depth * 0.5f)
        };
        mesh.triangles = new int[] { 0, 2, 1, 0, 3, 2 };
        float uv = 1f / repeat;
        mesh.uv = new Vector2[]
        {
            new Vector2(-width * 0.5f * uv, -depth * 0.5f * uv),
            new Vector2( width * 0.5f * uv, -depth * 0.5f * uv),
            new Vector2( width * 0.5f * uv,  depth * 0.5f * uv),
            new Vector2(-width * 0.5f * uv,  depth * 0.5f * uv)
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mf.mesh = mesh;

        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Repeat
        };
        tex.SetPixels(new Color[]
        {
            Color.white, Color.black,
            Color.black, Color.white
        });
        tex.Apply();

        Material mat = new Material(Shader.Find("Standard"))
        {
            mainTexture = tex
        };
        mat.SetFloat("_Glossiness", 0.2f);
        mr.material = mat;

        Debug.Log($"AITrackGenerator: Finish point at {p0} — checkered strip {width}m wide");
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
