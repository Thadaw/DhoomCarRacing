using UnityEngine;
using Photon.Pun;
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

        return spawnedCar;
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
        // Front wheels have higher Z (forward), rear wheels have lower Z
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

        // Find wheel mesh transforms by name
        Transform frontLeftMesh = FindWheelMesh(root, "Wheel_L");
        Transform frontRightMesh = FindWheelMesh(root, "Wheel_R");
        Transform rearLeftMesh = FindWheelMesh(root, "Wheel_Back_L");
        Transform rearRightMesh = FindWheelMesh(root, "Wheel_Back_R");

        // Wire wheel transforms (only if null)
        if (cc.frontLeftTransform == null) cc.frontLeftTransform = frontLeftMesh;
        if (cc.frontRightTransform == null) cc.frontRightTransform = frontRightMesh;
        if (cc.rearLeftTransform == null) cc.rearLeftTransform = rearLeftMesh;
        if (cc.rearRightTransform == null) cc.rearRightTransform = rearRightMesh;

        Debug.Log("CarSpawner: Wired " + root.name + " - wheels: " +
            (cc.frontLeftWheel != null) + ", " +
            (cc.frontRightWheel != null) + ", " +
            (cc.rearLeftWheel != null) + ", " +
            (cc.rearRightWheel != null));
    }

    private Transform FindWheelMesh(Transform root, string name)
    {
        Transform t = root.Find(name);
        if (t != null) return t;
        // Fallback: search all children
        foreach (Transform child in root.GetComponentsInChildren<Transform>())
        {
            if (child.name == name) return child;
        }
        return null;
    }
}