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

    private Vector3 currentVelocity;

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
    }

    public void SetTarget(Transform target, Rigidbody rb = null)
    {
        playerCarTransform = target;

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