using UnityEngine;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;

public class CarSpawner : MonoBehaviour
{
    [SerializeField] private GameObject[] carsPrefabs;

    private GameObject spawnedCar;

    void Awake()
    {
        if (PhotonNetwork.InRoom)
        {
            NetworkCarManager.EnsureExists();
            if (NetworkCarManager.Instance != null)
            {
                NetworkCarManager.Instance.RegisterCarPrefabs(carsPrefabs);
            }
        }
    }

    void Start()
    {
        // In multiplayer, register prefabs with NetworkCarManager instead of spawning
        if (PhotonNetwork.InRoom)
        {
            return;
        }

        SpawnCar();
    }

    public GameObject[] GetCarPrefabs()
    {
        return carsPrefabs;
    }

    public GameObject[] GetValidPrefabs()
    {
        if (carsPrefabs == null) return new GameObject[0];
        List<GameObject> valid = new List<GameObject>();
        for (int i = 0; i < carsPrefabs.Length; i++)
        {
            if (carsPrefabs[i] != null)
                valid.Add(carsPrefabs[i]);
        }
        return valid.ToArray();
    }

    public GameObject SpawnCar()
    {
        if (carsPrefabs == null || carsPrefabs.Length == 0)
        {
            Debug.LogWarning("CarSpawner: No car prefabs assigned.");
            return null;
        }

        int currentCarIndex = PlayerPrefs.GetInt("CarIndexValue", 0);
        currentCarIndex = Mathf.Clamp(currentCarIndex, 0, carsPrefabs.Length - 1);

        GameObject selectedPrefab = carsPrefabs[currentCarIndex];

        if (selectedPrefab == null)
        {
            for (int i = 0; i < carsPrefabs.Length; i++)
            {
                if (carsPrefabs[i] != null)
                {
                    selectedPrefab = carsPrefabs[i];
                    Debug.LogWarning($"CarSpawner: Car at index {currentCarIndex} is null, falling back to index {i}.");
                    break;
                }
            }
        }

        if (selectedPrefab == null)
        {
            Debug.LogWarning("CarSpawner: No valid car prefab found.");
            return null;
        }

        // Destroy any existing cars in scene (scene-placed or previously spawned)
        PhotonCarController[] existingCars = FindObjectsByType<PhotonCarController>(FindObjectsSortMode.None);
        foreach (var existing in existingCars)
        {
            if (existing.gameObject != gameObject)
            {
                Debug.Log("CarSpawner: Destroying scene car " + existing.gameObject.name);
                Destroy(existing.gameObject);
            }
        }
        spawnedCar = null;

        spawnedCar = Instantiate(
            selectedPrefab,
            transform.position,
            transform.rotation
        );

        // In single player, mark this car as the local player's car
        PhotonCarController cc = spawnedCar.GetComponent<PhotonCarController>();
        if (cc != null)
        {
            cc.isLocalPlayerCar = true;
            WirePhotonCarReferences(spawnedCar.transform, cc);
        }

        // Add lap tracker for results UI (normally added by NetworkCar in multiplayer)
        if (!spawnedCar.TryGetComponent<PlayerLapTracker>(out _))
            spawnedCar.AddComponent<PlayerLapTracker>();

        // Add car sounds
        if (!spawnedCar.TryGetComponent<CarSound>(out _))
            spawnedCar.AddComponent<CarSound>();

        AssignCameraTarget(spawnedCar);

        // Spawn AI opponents if in AI mode
        if (GameSession.Instance != null && GameSession.Instance.CurrentMode == GameSession.GameMode.AI)
        {
            SpawnAICars();
        }

        return spawnedCar;
    }

    private void SpawnAICars()
    {
        string[] aiNames = { "AI Player 1", "AI Player 2", "AI Player 3" };
        // Top speeds (km/h) — must line up with AITrackGenerator's per-car speeds
        // (AITrackGenerator.defaultSpeedKmh ± 10 = 135 / 145 / 155).
        float[] aiSpeeds = { 135f, 145f, 155f };

        int playerCarIndex = PlayerPrefs.GetInt("CarIndexValue", 0);
        List<int> availableIndices = new List<int>();
        for (int i = 0; i < carsPrefabs.Length; i++)
        {
            if (i == playerCarIndex) continue;
            if (carsPrefabs[i] != null)
                availableIndices.Add(i);
        }

        if (availableIndices.Count == 0) return;

        int aiCount = Mathf.Min(3, availableIndices.Count);

        Vector3 roadDir = transform.forward;
        roadDir.y = 0f;
        roadDir.Normalize();

        Vector3 rightDir = transform.right;
        rightDir.y = 0f;
        rightDir.Normalize();

        Vector3 playerPos = spawnedCar.transform.position;

        // AI cars in front, player behind
        Vector3[] spawnLateralOffsets = new Vector3[]
        {
            -rightDir * 6f,
            Vector3.zero,
            rightDir * 6f
        };

        // Move player car behind the AI grid
        spawnedCar.transform.position = playerPos - roadDir * 10f;

        Vector3 aiStartPos = playerPos + roadDir * 10f;

        // Grid slots: line each AI car up on waypoint 0 of its own racing line.
        // Those waypoints sit exactly on the start/finish line, 10m apart, so the
        // cars start on their line and cross the SAME line to complete the lap.
        Transform[] gridSlots = new Transform[3];
        if (AITrackGenerator.PerCarWaypoints != null)
        {
            for (int i = 0; i < 3 && i < AITrackGenerator.PerCarWaypoints.Length; i++)
            {
                List<Transform> line = AITrackGenerator.PerCarWaypoints[i];
                if (line != null && line.Count > 0)
                    gridSlots[i] = line[0];
            }
        }

        for (int i = 0; i < aiCount; i++)
        {
            int prefabIndex = availableIndices[i];

            Vector3 spawnPos;
            Quaternion spawnRot;
            if (gridSlots[i] != null)
            {
                // Start exactly on the start/finish line, on this car's racing line.
                spawnPos = gridSlots[i].position;
                spawnRot = gridSlots[i].rotation;
            }
            else
            {
                spawnPos = aiStartPos + spawnLateralOffsets[i];
                spawnPos.y = playerPos.y;
                spawnRot = Quaternion.LookRotation(roadDir, Vector3.up);
            }

            GameObject aiCar = Instantiate(carsPrefabs[prefabIndex], spawnPos, spawnRot);
            aiCar.name = aiNames[i];

            PhotonCarController aiCC = aiCar.GetComponent<PhotonCarController>();
            if (aiCC != null)
            {
                aiCC.isLocalPlayerCar = false;
                aiCC.useExternalInput = true;
                WirePhotonCarReferences(aiCar.transform, aiCC);
            }

            if (!aiCar.TryGetComponent<PlayerLapTracker>(out _))
                aiCar.AddComponent<PlayerLapTracker>();

            // Engine/BGM sound belongs to the real player only — strip any CarSound
            // that came with the prefab so AI cars stay completely silent.
            foreach (CarSound sound in aiCar.GetComponentsInChildren<CarSound>(true))
                Destroy(sound);

            AIDriver aiDriver = aiCar.AddComponent<AIDriver>();
            aiDriver.aiName = aiNames[i];
            aiDriver.aiIndex = i;
            aiDriver.maxSpeedKmh = aiSpeeds[i];

            // Assign individual waypoints for this AI car
            if (AITrackGenerator.PerCarWaypoints != null && i < AITrackGenerator.PerCarWaypoints.Length)
            {
                List<Transform> carWaypoints = AITrackGenerator.PerCarWaypoints[i];
                aiDriver.waypoints = carWaypoints.ToArray();
            }

            StartCoroutine(InitializeAIDelayed(aiCar, aiNames[i]));

            Debug.Log($"CarSpawner: Spawned {aiNames[i]} at {spawnPos} (waypoints={aiDriver.waypoints?.Length ?? 0}, speed={aiSpeeds[i]})");
        }
    }

    private IEnumerator InitializeAIDelayed(GameObject aiCar, string aiName)
    {
        yield return new WaitForSeconds(0.3f);

        if (aiCar == null) yield break;

        AIDriver driver = aiCar.GetComponent<AIDriver>();
        if (driver != null)
        {
            PhotonCarController cc = aiCar.GetComponent<PhotonCarController>();
            if (cc != null)
            {
                WirePhotonCarReferences(aiCar.transform, cc);
            }
            driver.Initialize(aiName);
        }
    }

    private void AssignCameraTarget(GameObject car)
    {
        if (car == null) return;

        CameraMovement camMovement = FindFirstObjectByType<CameraMovement>();
        if (camMovement != null)
        {
            camMovement.SetTarget(car.transform);
            return;
        }

        FollowCar follow = FindFirstObjectByType<FollowCar>();
        if (follow != null)
        {
            follow.carTransform = car.transform;
            return;
        }

        Debug.LogWarning("No camera follow script found.");
    }

    private void WirePhotonCarReferences(Transform root, PhotonCarController cc)
    {
        // Fix carRb
        if (cc.carRb == null)
            cc.carRb = root.GetComponent<Rigidbody>();

        // Fix centerOfMass
        if (cc.centerOfMass == null)
        {
            Transform com = root.Find("CarCenterOfMass");
            if (com == null) com = root.Find("CenterOfMass");
            cc.centerOfMass = com;
        }

        // Find all WheelColliders in children
        WheelCollider[] allWheels = root.GetComponentsInChildren<WheelCollider>();
        if (allWheels.Length < 4)
        {
            Debug.LogWarning("CarSpawner: " + root.name + " has only " + allWheels.Length + " WheelColliders, need 4");
            return;
        }

        // Sort wheels: front-left, front-right, rear-left, rear-right based on Z position
        System.Array.Sort(allWheels, (a, b) =>
        {
            bool aFront = a.transform.localPosition.z > 0;
            bool bFront = b.transform.localPosition.z > 0;
            if (aFront != bFront) return aFront ? -1 : 1;
            return a.transform.localPosition.x.CompareTo(b.transform.localPosition.x);
        });

        // Wire wheel colliders (only if null)
        if (cc.frontLeftWheel == null) cc.frontLeftWheel = allWheels[0];
        if (cc.frontRightWheel == null) cc.frontRightWheel = allWheels[1];
        if (cc.rearLeftWheel == null) cc.rearLeftWheel = allWheels[2];
        if (cc.rearRightWheel == null) cc.rearRightWheel = allWheels[3];

        // Wire wheel transforms using collider transforms as fallback
        // Try to find by common naming conventions, then fall back to collider transforms
        if (cc.frontLeftTransform == null)
            cc.frontLeftTransform = FindWheelMesh(root, "Wheel_L", "FrontLeft", "FL") ?? allWheels[0].transform;
        if (cc.frontRightTransform == null)
            cc.frontRightTransform = FindWheelMesh(root, "Wheel_R", "FrontRight", "FR") ?? allWheels[1].transform;
        if (cc.rearLeftTransform == null)
            cc.rearLeftTransform = FindWheelMesh(root, "Wheel_Back_L", "RearLeft", "RL") ?? allWheels[2].transform;
        if (cc.rearRightTransform == null)
            cc.rearRightTransform = FindWheelMesh(root, "Wheel_Back_R", "RearRight", "RR") ?? allWheels[3].transform;

        Debug.Log("CarSpawner: Wired " + root.name + " - wheels: " +
            (cc.frontLeftWheel != null) + ", " +
            (cc.frontRightWheel != null) + ", " +
            (cc.rearLeftWheel != null) + ", " +
            (cc.rearRightWheel != null));
    }

    private Transform FindWheelMesh(Transform root, string name1, string name2, string name3)
    {
        Transform t = root.Find(name1);
        if (t != null) return t;
        t = root.Find(name2);
        if (t != null) return t;
        t = root.Find(name3);
        if (t != null) return t;
        // Fallback: search all children by partial name match
        foreach (Transform child in root.GetComponentsInChildren<Transform>())
        {
            string n = child.name.ToLower();
            if (n.Contains(name1.ToLower()) || n.Contains(name2.ToLower()) || n.Contains(name3.ToLower()))
                return child;
        }
        return null;
    }
}