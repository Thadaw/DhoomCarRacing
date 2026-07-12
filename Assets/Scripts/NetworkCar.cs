using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;

public class NetworkCar : MonoBehaviourPun, IPunObservable
{
    private PhotonCarController carController;
    private Rigidbody carRigidbody;
    private Vector3 networkPosition;
    private Quaternion networkRotation;
    private Transform carModelTransform;
    private bool hasReceivedFirstUpdate;

    void Awake()
    {
        // Destroy PhotonTransformView so FindObservables() can never re-add it.
        var ptv = GetComponent<PhotonTransformView>();
        if (ptv != null)
            DestroyImmediate(ptv);

        var pv = GetComponent<PhotonView>();
        if (pv != null)
        {
            pv.Synchronization = ViewSynchronization.UnreliableOnChange;
            pv.ObservedComponents.Clear();
            pv.ObservedComponents.Add(this);
        }

        Debug.Log($"NetworkCar.Awake: IsMine={photonView.IsMine}, carId from InstantiationData={GetCarId()}");
    }

    private int GetCarId()
    {
        if (photonView.InstantiationData != null && photonView.InstantiationData.Length > 0)
            return (int)photonView.InstantiationData[0];
        return -1;
    }

    void Start()
    {

        if (NetworkCarManager.Instance == null)
        {
            Debug.LogError("NetworkCarManager.Instance is null!");
            return;
        }

        if (photonView.InstantiationData == null || photonView.InstantiationData.Length < 1)
        {
            Debug.LogError("NetworkCar: No instantiation data (carId) provided.");
            return;
        }

        int carId = (int)photonView.InstantiationData[0];
        GameObject[] prefabs = NetworkCarManager.Instance.carPrefabs;

        if (prefabs == null || carId < 0 || carId >= prefabs.Length)
        {
            Debug.LogError("NetworkCar: Invalid carId " + carId);
            return;
        }

        GameObject carModel = Instantiate(prefabs[carId], transform.position, transform.rotation, transform);
        carModelTransform = carModel.transform;

        carController = carModel.GetComponent<PhotonCarController>();
        if (carController != null)
        {
            carController.photonViewRef = photonView;
            carController.isLocalPlayerCar = photonView.IsMine;
        }

        carRigidbody = carModel.GetComponent<Rigidbody>();
        if (!photonView.IsMine && carRigidbody != null)
        {
            carRigidbody.isKinematic = true;
        }

        if (photonView.IsMine)
        {
            CameraMovement cam = FindFirstObjectByType<CameraMovement>();
            if (cam == null && Camera.main != null)
            {
                cam = Camera.main.gameObject.AddComponent<CameraMovement>();
            }
            if (cam != null)
            {
                cam.SetTarget(carModel.transform, carRigidbody);
            }

            FollowCar follow = FindFirstObjectByType<FollowCar>();
            if (follow != null)
            {
                follow.carTransform = carModel.transform;
            }

            UIManager ui = FindFirstObjectByType<UIManager>();
            if (ui != null)
            {
                ui.BindCarController(carController);
            }
        }

        if (!carModel.TryGetComponent<PlayerLapTracker>(out _))
        {
            carModel.AddComponent<PlayerLapTracker>();
        }

        NetworkCarManager.Instance.RegisterCar(photonView.ViewID, carModel);
    }

    void Update()
    {
        if (!photonView.IsMine && hasReceivedFirstUpdate && carModelTransform != null)
        {
            carModelTransform.position = Vector3.Lerp(carModelTransform.position, networkPosition, Time.deltaTime * 10f);
            carModelTransform.rotation = Quaternion.Slerp(carModelTransform.rotation, networkRotation, Time.deltaTime * 10f);

            if (carController != null)
                carController.UpdateWheelVisuals();
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            Transform target = carModelTransform != null ? carModelTransform : transform;
            stream.SendNext(target.position);
            stream.SendNext(target.rotation);
        }
        else
        {
            networkPosition = (Vector3)stream.ReceiveNext();
            networkRotation = (Quaternion)stream.ReceiveNext();
            if (!hasReceivedFirstUpdate)
                Debug.Log($"NetworkCar FIRST READ: pos={networkPosition}, rot={networkRotation}");
            hasReceivedFirstUpdate = true;
        }
    }
}
