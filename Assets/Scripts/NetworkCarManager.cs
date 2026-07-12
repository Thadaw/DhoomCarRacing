using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class NetworkCarManager : MonoBehaviour
{
    public static NetworkCarManager Instance;

    [Header("Car Prefabs (drag from Prefab folder)")]
    public GameObject[] carPrefabs;

    [Header("Spawn Points")]
    public Transform[] spawnPoints;

    [Header("Network")]
    public string networkCarPrefabName = "NetworkCar";

    [Header("Spawn Alignment")]
    public float groundRaycastHeight = 5f;
    public float groundRaycastDistance = 20f;
    public float groundSnapOffset = 0.1f;

    private Dictionary<int, GameObject> allCars = new Dictionary<int, GameObject>();

    // Track whether this client has already spawned its car for the current race scene
    private bool hasSpawnedLocal = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (PhotonNetwork.InRoom)
        {
            FindSpawnPointsInScene();
            SpawnMyNetworkCar();
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Instance = null;
    }

    public static void EnsureExists()
    {
        if (Instance == null)
        {
            var go = new GameObject("NetworkCarManager");
            go.AddComponent<NetworkCarManager>();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        hasSpawnedLocal = false;
        spawnPoints = null;

        if (PhotonNetwork.InRoom)
        {
            FindSpawnPointsInScene();
            SpawnMyNetworkCar();
        }
    }

    public void RegisterCarPrefabs(GameObject[] prefabs)
    {
        if (prefabs == null || prefabs.Length == 0) return;
        if (carPrefabs == null || carPrefabs.Length == 0)
        {
            carPrefabs = prefabs;
            TrySpawnIfReady();
        }
    }

    private void TrySpawnIfReady()
    {
        if (hasSpawnedLocal) return;
        if (!PhotonNetwork.InRoom) return;
        if (carPrefabs == null || carPrefabs.Length == 0) return;
        if (spawnPoints == null || spawnPoints.Length == 0) return;
        SpawnMyNetworkCar();
    }

    private void FindSpawnPointsInScene()
    {
        // Try object named "SpawnPoints" containing children transforms
        var parent = GameObject.Find("SpawnPoints");
        if (parent != null)
        {
            var children = new List<Transform>();
            foreach (Transform t in parent.transform)
                children.Add(t);

            if (children.Count > 0)
            {
                spawnPoints = children.ToArray();
                return;
            }
        }

        // Try objects tagged "SpawnPoint"
        try
        {
            var gos = GameObject.FindGameObjectsWithTag("SpawnPoint");
            if (gos != null && gos.Length > 0)
            {
                var pts = new Transform[gos.Length];
                for (int i = 0; i < gos.Length; i++) pts[i] = gos[i].transform;
                spawnPoints = pts;
                return;
            }
        }
        catch { }

        // Fallback: generate race grid around CarSpawner
        var spawners = FindObjectsByType<CarSpawner>(FindObjectsSortMode.None);
        if (spawners != null && spawners.Length > 0)
        {
            CreateRaceGrid(spawners[0].transform);
        }
    }

    private void CreateRaceGrid(Transform center)
    {
        int count = 4;
        spawnPoints = new Transform[count];

        // All cars at the exact CarSpawner position (which is on the road)
        // with a tiny forward stagger so they don't visually overlap
        float spacing = 0.8f;

        for (int i = 0; i < count; i++)
        {
            GameObject go = new GameObject("SpawnPoint_" + (i + 1));
            go.transform.SetParent(center);

            float forward = (i - 1.5f) * spacing;

            Vector3 pos = center.position + center.forward * forward;

            // Snap to ground
            RaycastHit hit;
            if (Physics.Raycast(pos + Vector3.up * 5f, Vector3.down, out hit, 20f))
            {
                pos = hit.point;
            }

            go.transform.position = pos;
            go.transform.rotation = center.rotation;

            spawnPoints[i] = go.transform;
        }
    }

    void SpawnMyNetworkCar()
    {
        if (hasSpawnedLocal) return;

        if (carPrefabs == null || carPrefabs.Length == 0)
        {
            Debug.LogWarning("NetworkCarManager: No car prefabs assigned.");
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("NetworkCarManager: No spawn points assigned.");
            return;
        }

        int carId = 0;
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("CarId", out object carIdObj)
            && carIdObj is int propCarId)
        {
            carId = propCarId;
        }
        else if (GameSession.Instance != null)
        {
            carId = GameSession.Instance.SelectedCarId;
        }

        carId = Mathf.Clamp(carId, 0, carPrefabs.Length - 1);

        int spawnIndex = PhotonNetwork.LocalPlayer.ActorNumber % spawnPoints.Length;
        Transform spawnPt = spawnPoints[spawnIndex];

        PhotonNetwork.Instantiate(
            networkCarPrefabName,
            spawnPt.position,
            spawnPt.rotation,
            0,
            new object[] { carId }
        );

        hasSpawnedLocal = true;
    }

    // Public helper for other spawners to instantiate a networked car
    public GameObject SpawnNetworkCar(Vector3 pos, Quaternion rot, object[] instantiationData = null)
    {
        if (!PhotonNetwork.IsConnected)
        {
            Debug.LogWarning("NetworkCarManager: Photon not connected, cannot spawn networked car.");
            return null;
        }

        GameObject go = PhotonNetwork.Instantiate(networkCarPrefabName, pos, rot, 0, instantiationData);
        return go;
    }

    public void RegisterCar(int viewId, GameObject car)
    {
        if (!allCars.ContainsKey(viewId))
        {
            allCars.Add(viewId, car);
        }
    }

    public GameObject GetCar(int viewId)
    {
        if (allCars.TryGetValue(viewId, out GameObject car))
            return car;
        return null;
    }
}
