using UnityEngine;
using Photon.Pun;

public class CarSpawner : MonoBehaviour
{
    [SerializeField] private GameObject[] carsPrefabs;

    private GameObject spawnedCar;

    void Start()
    {
        // In multiplayer, register prefabs with NetworkCarManager instead of spawning
        if (PhotonNetwork.InRoom)
        {
            NetworkCarManager.EnsureExists();
            if (NetworkCarManager.Instance != null)
            {
                NetworkCarManager.Instance.RegisterCarPrefabs(carsPrefabs);
            }
            return;
        }

        SpawnCar();
    }

    public GameObject[] GetCarPrefabs()
    {
        return carsPrefabs;
    }

    public void SpawnCar()
    {
        if (carsPrefabs == null || carsPrefabs.Length == 0)
        {
            Debug.LogWarning("CarSpawner: No car prefabs assigned.");
            return;
        }

        int currentCarIndex = PlayerPrefs.GetInt("CarIndexValue", 0);
        currentCarIndex = Mathf.Clamp(currentCarIndex, 0, carsPrefabs.Length - 1);

        // Destroy previously spawned car (IMPORTANT FIX)
        if (spawnedCar != null)
        {
            Destroy(spawnedCar);
            spawnedCar = null;
        }

        GameObject selectedPrefab = carsPrefabs[currentCarIndex];

        spawnedCar = Instantiate(
            selectedPrefab,
            transform.position,
            transform.rotation
        );

        // In single player, mark this car as the local player's car
        CarController cc = spawnedCar.GetComponent<CarController>();
        if (cc != null)
            cc.isLocalPlayerCar = true;

        // Add lap tracker for results UI (normally added by NetworkCar in multiplayer)
        if (!spawnedCar.TryGetComponent<PlayerLapTracker>(out _))
            spawnedCar.AddComponent<PlayerLapTracker>();

        AssignCameraTarget(spawnedCar);
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