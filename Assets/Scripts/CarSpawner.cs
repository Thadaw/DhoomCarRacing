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

        // Destroy previously spawned car (IMPORTANT FIX)
        if (spawnedCar != null)
        {
            Destroy(spawnedCar);
            spawnedCar = null;
        }

        spawnedCar = Instantiate(
            selectedPrefab,
            transform.position,
            transform.rotation
        );

        // In single player, mark this car as the local player's car
        PhotonCarController cc = spawnedCar.GetComponent<PhotonCarController>();
        if (cc != null)
            cc.isLocalPlayerCar = true;

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
}