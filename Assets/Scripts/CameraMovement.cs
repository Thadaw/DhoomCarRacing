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
    [Tooltip("how much the camera banks (rolls) while drifting , negative flips the direction")]
    [SerializeField] private float driftRollAngle = 6f;
    [SerializeField] private float driftRollLerpSpeed = 5f;
    [Tooltip("extra field of view while drifting , 0 = off")]
    [SerializeField] private float driftFovKick = 8f;
    [SerializeField] private float driftFovLerpSpeed = 5f;

    private Vector3 currentVelocity;
    private float currentRoll;
    private WheelsManager carWheels;
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

        ApplyDriftRoll();
        ApplyDriftFov();
    }

    // drift amount from whichever car controller stack this car uses
    private float GetDriftAmount()
    {
        if (photonCar != null) return photonCar.driftAmount;
        if (carWheels != null) return carWheels.driftAmount;
        return 0f;
    }

    // fov swells while sliding and settles back as the car grips up
    private void ApplyDriftFov()
    {
        if (cam == null || driftFovKick <= 0f) return;
        if (photonCar == null && carWheels == null) return;   // no drift system on this car

        float targetFov = baseFov + driftFovKick * GetDriftAmount();
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, driftFovLerpSpeed * Time.deltaTime);
    }

    // banks the camera into the drift , scaled by the car's drift amount and yaw rate
    private void ApplyDriftRoll()
    {
        if (carWheels == null && photonCar == null && playerCarTransform != null)
        {
            carWheels = playerCarTransform.GetComponent<WheelsManager>();
            photonCar = playerCarTransform.GetComponent<PhotonCarController>();
        }

        // SetTarget is often called without the rb , fetch it so yaw rate / speed effects still work
        if (playerCarRb == null && playerCarTransform != null)
            playerCarRb = playerCarTransform.GetComponent<Rigidbody>();

        float targetRoll = 0f;

        if (driftRollAngle != 0f && (carWheels != null || photonCar != null))
        {
            float yawRate = playerCarRb != null
                ? Vector3.Dot(playerCarRb.angularVelocity, playerCarTransform.up)
                : 0f;

            // full roll when rotating at 1 rad/s or more , only while actually drifting
            targetRoll = Mathf.Clamp(yawRate, -1f, 1f) * driftRollAngle * GetDriftAmount();
        }

        currentRoll = Mathf.Lerp(currentRoll, targetRoll, driftRollLerpSpeed * Time.deltaTime);

        if (Mathf.Abs(currentRoll) > 0.01f)
            transform.rotation = Quaternion.AngleAxis(currentRoll, transform.forward) * transform.rotation;
    }

    public void SetTarget(Transform target, Rigidbody rb = null)
    {
        playerCarTransform = target;
        carWheels = playerCarTransform != null ? playerCarTransform.GetComponent<WheelsManager>() : null;
        photonCar = playerCarTransform != null ? playerCarTransform.GetComponent<PhotonCarController>() : null;
        currentRoll = 0f;

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