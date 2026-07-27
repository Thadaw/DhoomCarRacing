using UnityEngine;
using Photon.Pun;

public class PhotonCarController : MonoBehaviour
{
    [Header("Networking")]
    public PhotonView photonViewRef;
    public bool isLocalPlayerCar = false;

    [Header("Wheel Colliders")]
    public WheelCollider frontLeftWheel;

    public WheelCollider frontRightWheel;
    public WheelCollider rearLeftWheel;
    public WheelCollider rearRightWheel;

    [Header("Wheel Meshes")]
    public Transform frontLeftTransform;
    public Transform frontRightTransform;
    public Transform rearLeftTransform;
    public Transform rearRightTransform;

    [Header("Car Setup")]
    public Rigidbody carRb;
    public Transform centerOfMass;

    [Header("Engine")]
    public float motorForce = 3500f;
    public float maxSpeed = 220f;

    [Header("Steering")]
    public float maxSteerAngle = 32f;
    public float steeringResponsiveness = 6f;
    public float highSpeedSteeringReduction = 0.45f;

    [Header("Brakes")]
    public float brakeForce = 4000f;

    [Header("Arcade Handling")]
    public float downforce = 80f;
    [Range(0.8f, 1f)]
    public float driftFactor = 0.92f;
    public float antiRollForce = 6000f;

    private float throttleInput;
    private float steeringInput;
    private bool isBraking;

    private void Start()
    {
        if (centerOfMass != null)
            carRb.centerOfMass = centerOfMass.localPosition;

        carRb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void FixedUpdate()
    {
        // Non-local cars should not read input or apply driving forces.
        if (photonViewRef != null)
        {
            if (!photonViewRef.IsMine)
                return;
        }
        else
        {
            if (!isLocalPlayerCar)
                return;
        }

        // Lock car until countdown finishes
        if (RaceManager.Instance != null && !RaceManager.Instance.raceStarted)
        {
            frontLeftWheel.motorTorque = 0f;
            frontRightWheel.motorTorque = 0f;


            frontLeftWheel.brakeTorque = brakeForce;
            frontRightWheel.brakeTorque = brakeForce;
            rearLeftWheel.brakeTorque = brakeForce;
            rearRightWheel.brakeTorque = brakeForce;

            UpdateWheels();
            return;
        }

        GetInputs();

        HandleMotor();
        HandleSteering();
        HandleBrakes();

        ApplyDownforce();
        ApplyDriftControl();
        ApplyAntiRoll();

        UpdateWheels();
    }

    private void GetInputs()
    {
        throttleInput = Input.GetAxis("Vertical");
        steeringInput = Input.GetAxis("Horizontal");
        isBraking = Input.GetKey(KeyCode.Space);
    }

    private void HandleMotor()
    {
        float currentSpeed = CarSpeed();

        if (currentSpeed < maxSpeed)
        {
            frontLeftWheel.motorTorque = throttleInput * motorForce;
            frontRightWheel.motorTorque = throttleInput * motorForce;
        }
        else
        {
            frontLeftWheel.motorTorque = 0f;
            frontRightWheel.motorTorque = 0f;
        }
    }

    private void HandleSteering()
    {
        float speedPercent = Mathf.Clamp01(CarSpeed() / maxSpeed);

        float steerLimit =
            Mathf.Lerp(
                maxSteerAngle,
                maxSteerAngle * highSpeedSteeringReduction,
                speedPercent);

        float targetAngle = steeringInput * steerLimit;

        frontLeftWheel.steerAngle =
            Mathf.Lerp(
                frontLeftWheel.steerAngle,
                targetAngle,
                Time.fixedDeltaTime * steeringResponsiveness);

        frontRightWheel.steerAngle =
            Mathf.Lerp(
                frontRightWheel.steerAngle,
                targetAngle,
                Time.fixedDeltaTime * steeringResponsiveness);
    }

    private void HandleBrakes()
    {
        float currentBrakeForce = isBraking ? brakeForce : 0f;

        frontLeftWheel.brakeTorque = currentBrakeForce;
        frontRightWheel.brakeTorque = currentBrakeForce;
        rearLeftWheel.brakeTorque = currentBrakeForce;
        rearRightWheel.brakeTorque = currentBrakeForce;
    }

    private void ApplyDownforce()
    {
        carRb.AddForce(
            -transform.up * downforce * carRb.linearVelocity.magnitude,
            ForceMode.Force);
    }

    private void ApplyDriftControl()
    {
        Vector3 localVelocity =
            transform.InverseTransformDirection(carRb.linearVelocity);

        localVelocity.x *= driftFactor;

        carRb.linearVelocity =
            transform.TransformDirection(localVelocity);
    }

    private void ApplyAntiRoll()
    {
        ApplyAntiRollAxle(frontLeftWheel, frontRightWheel);
        ApplyAntiRollAxle(rearLeftWheel, rearRightWheel);
    }

    private void ApplyAntiRollAxle(
        WheelCollider leftWheel,
        WheelCollider rightWheel)
    {
        WheelHit hit;

        float travelLeft = 1.0f;
        float travelRight = 1.0f;

        bool groundedLeft = leftWheel.GetGroundHit(out hit);

        if (groundedLeft)
        {
            travelLeft =
                (-leftWheel.transform.InverseTransformPoint(hit.point).y
                - leftWheel.radius)
                / leftWheel.suspensionDistance;
        }

        bool groundedRight = rightWheel.GetGroundHit(out hit);

        if (groundedRight)
        {
            travelRight =
                (-rightWheel.transform.InverseTransformPoint(hit.point).y
                - rightWheel.radius)
                / rightWheel.suspensionDistance;
        }

        float antiRoll = (travelLeft - travelRight) * antiRollForce;

        if (groundedLeft)
        {
            carRb.AddForceAtPosition(
                leftWheel.transform.up * -antiRoll,
                leftWheel.transform.position);
        }

        if (groundedRight)
        {
            carRb.AddForceAtPosition(
                rightWheel.transform.up * antiRoll,
                rightWheel.transform.position);
        }
    }

    public void UpdateWheelVisuals()
    {
        UpdateWheels();
    }

    private void UpdateWheels()
    {
        UpdateWheel(frontLeftWheel, frontLeftTransform);
        UpdateWheel(frontRightWheel, frontRightTransform);
        UpdateWheel(rearLeftWheel, rearLeftTransform);
        UpdateWheel(rearRightWheel, rearRightTransform);
    }

    private void UpdateWheel(
        WheelCollider wheelCollider,
        Transform wheelTransform)
    {
        Vector3 pos;
        Quaternion rot;

        wheelCollider.GetWorldPose(out pos, out rot);

        wheelTransform.position = pos;
        wheelTransform.rotation = rot;
    }

    public float CarSpeed()
    {
        return carRb.linearVelocity.magnitude * 3.6f;
    }
}