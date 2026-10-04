using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform playerCarTransform;
    [SerializeField] private Rigidbody playerCarRb;

    [Header("Camera Position")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 5.5f, -10f);
    [SerializeField] private float positionSmoothTime = 0.1f;

    [Header("Camera Rotation")]
    [SerializeField] private float rotationSmoothSpeed = 8f;
    [SerializeField] private float lookHeight = 1.2f;
    [SerializeField] private float lookAheadDistance = 15f;

    [Header("Speed Zoom")]
    [SerializeField] private float speedDistanceMultiplier = 0.03f;
    [SerializeField] private float maxExtraDistance = 6f;

    [Header("Camera Shake")]
    [SerializeField] private float shakeStartSpeed = 120f;
    [SerializeField] private float maxShakeAmount = 0.08f;

    [Header("Drift Feel")]
    [Tooltip("extra field of view while drifting , 0 = off")]
    [SerializeField] private float driftFovKick = 8f;
    [SerializeField] private float driftFovLerpSpeed = 5f;

    private Vector3 currentVelocity;
    private PhotonCarController photonCar;
    private Camera cam;
    private float baseFov;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam != null) baseFov = cam.fieldOfView;
    }

    private void LateUpdate()
    {
        if (playerCarTransform == null)
            return;

        // The scene-serialized rb points at a PREFAB ASSET (never simulated, so its
        // velocity is always 0) and SetTarget is often called without an rb — that is
        // why speed zoom / shake never fired in single player while multiplayer (which
        // passes the real rb) worked. Bind to the rigidbody of the car we actually
        // follow, whatever mode we are in.
        if (playerCarRb == null || !IsRbPartOfTarget())
        {
            Rigidbody found = playerCarTransform.GetComponent<Rigidbody>();
            if (found == null)
                found = playerCarTransform.GetComponentInParent<Rigidbody>();
            if (found != null)
                playerCarRb = found;
        }

        float speed = 0f;

        if (playerCarRb != null)
        {
            speed = playerCarRb.linearVelocity.magnitude * 3.6f; // km/h
        }

        // Dynamic distance based on speed
        float extraDistance = Mathf.Clamp(
            speed * speedDistanceMultiplier,
            0f,
            maxExtraDistance
        );

        Vector3 dynamicOffset =
            offset +
            new Vector3(0f, 0f, -extraDistance);

        Vector3 targetPosition =
            playerCarTransform.TransformPoint(dynamicOffset);

        // Camera shake at high speed
        if (speed > shakeStartSpeed)
        {
            float shakeStrength = Mathf.Lerp(
                0f,
                maxShakeAmount,
                (speed - shakeStartSpeed) / 100f
            );

            targetPosition += Random.insideUnitSphere * shakeStrength;
        }

        // Smooth follow
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref currentVelocity,
            positionSmoothTime
        );

        // Look ahead of the car
        Vector3 lookPoint =
            playerCarTransform.position +
            playerCarTransform.forward * lookAheadDistance +
            Vector3.up * lookHeight;

        Quaternion targetRotation =
            Quaternion.LookRotation(
                lookPoint - transform.position,
                Vector3.up
            );

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSmoothSpeed * Time.deltaTime
        );

        // NOTE: the camera never banks (rolls) with the drift , rotation only follows
        // the car so multiplayer matches the single player camera while drifting
        ApplyDriftFov();
    }

    // true when the rigidbody belongs to the car we are following (self, parent or child)
    private bool IsRbPartOfTarget()
    {
        Transform rbT = playerCarRb.transform;
        return rbT == playerCarTransform
            || rbT.IsChildOf(playerCarTransform)
            || playerCarTransform.IsChildOf(rbT);
    }

    // drift amount from the car controller (PhotonCarController drives every car)
    private float GetDriftAmount()
    {
        if (photonCar != null) return photonCar.driftAmount;
        return 0f;
    }

    // fov swells while sliding and settles back as the car grips up
    private void ApplyDriftFov()
    {
        if (cam == null || driftFovKick <= 0f) return;

        // fetch the controller lazily so the kick also works when the target
        // was assigned by a scene reference instead of SetTarget
        if (photonCar == null && playerCarTransform != null)
            photonCar = playerCarTransform.GetComponent<PhotonCarController>();

        if (photonCar == null) return;   // no drift system on this car

        float targetFov = baseFov + driftFovKick * GetDriftAmount();
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, driftFovLerpSpeed * Time.deltaTime);
    }

    public void SetTarget(Transform target, Rigidbody rb = null)
    {
        playerCarTransform = target;
        photonCar = playerCarTransform != null ? playerCarTransform.GetComponent<PhotonCarController>() : null;

        if (rb != null)
            playerCarRb = rb;

        if (playerCarTransform == null)
            return;

        transform.position =
            playerCarTransform.TransformPoint(offset);

        Vector3 lookPoint =
            playerCarTransform.position +
            playerCarTransform.forward * lookAheadDistance +
            Vector3.up * lookHeight;

        transform.rotation =
            Quaternion.LookRotation(
                lookPoint - transform.position,
                Vector3.up
            );

        currentVelocity = Vector3.zero;
    }
}