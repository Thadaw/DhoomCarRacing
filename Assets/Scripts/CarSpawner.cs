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
        float[] aiSkills = { 0.65f, 0.75f, 0.85f };

        int playerCarIndex = PlayerPrefs.GetInt("CarIndexValue", 0);
        List<int> availableIndices = new List<int>();
        for (int i = 0; i < carsPrefabs.Length; i++)
        {
            if (i == playerCarIndex) continue;
            if (carsPrefabs[i] != null)
                availableIndices.Add(i);
        }

        if (availableIndices.Count == 0)
        {
            Debug.LogWarning("CarSpawner: No AI car prefabs available.");
            return;
        }

        int aiCount = Mathf.Min(3, availableIndices.Count);

        Vector3 roadDir = transform.forward;
        roadDir.y = 0f;
        roadDir.Normalize();

        Vector3 rightDir = transform.right;
        rightDir.y = 0f;
        rightDir.Normalize();

        Vector3 playerPos = spawnedCar.transform.position;

        Vector3[] lateralOffsets = new Vector3[]
        {
            -rightDir * 8f,
            rightDir * 8f,
            Vector3.zero
        };

        float[] forwardOffsets = { -4f, 0f, 8f };

        for (int i = 0; i < aiCount; i++)
        {
            int prefabIndex = availableIndices[i];

            Vector3 spawnPos = playerPos + lateralOffsets[i] + roadDir * forwardOffsets[i];
            spawnPos.y = playerPos.y;

            Quaternion spawnRot = Quaternion.LookRotation(roadDir, Vector3.up);

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

            if (!aiCar.TryGetComponent<CarSound>(out _))
                aiCar.AddComponent<CarSound>();

            if (!aiCar.TryGetComponent<AudioSource>(out _))
                aiCar.AddComponent<AudioSource>();

            AIDriver aiDriver = aiCar.AddComponent<AIDriver>();
            aiDriver.skillLevel = aiSkills[i];

            StartCoroutine(InitializeAIDelayed(aiCar, aiNames[i]));

            Debug.Log($"CarSpawner: Spawned {aiNames[i]} at {spawnPos} using prefab[{prefabIndex}] (skill={aiSkills[i]})");
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
            // Reset state to ensure clean start for each race
            driver.ResetForNewRace();
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