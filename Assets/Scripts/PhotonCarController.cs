 using UnityEngine;
using Photon.Pun;

public class PhotonCarController : MonoBehaviour
{
    [Header("Networking")]
    public PhotonView photonViewRef;
    public bool isLocalPlayerCar = false;

    [Header("External Input (AI)")]
    public bool useExternalInput = false;
    private float extThrottle;
    private float extBrake;
    private float extSteer;

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
    public float motorForce = 4375f;   // +25% over the old 3500 — top speed still capped by maxSpeed
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

    [Header("Drifting")]
    [Tooltip("rear brake torque while the handbrake (space) is held , locks the rears to start a drift")]
    public float handbrakeForce = 6000f;
    [Tooltip("rear sideways friction multiplier while drifting , lower = rear breaks loose easier")]
    [Range(0.1f, 1f)] public float driftRearGrip = 0.45f;
    [Tooltip("extra yaw torque while drifting so the car hooks into the slide , 0 = off")]
    [Range(0f, 10f)] public float driftYawAssist = 3f;
    [Range(0f, 100f)] public float driftAssistMinKPH = 20f;
    [Tooltip("rear sideways slip considered 'drifting' even without the handbrake")]
    [Range(0.1f, 2f)] public float driftSlipThreshold = 0.4f;
    [Tooltip("how fast the car enters / exits the loose drift grip")]
    public float driftGripEnterSpeed = 8f;
    public float driftGripExitSpeed = 5f;
    [HideInInspector] public float driftAmount; // 0 = gripping , 1 = full drift (read by camera fx)

    private float baseRearLeftStiffness;
    private float baseRearRightStiffness;
    private bool driftSetupDone;

    private float throttleInput;
    private float steeringInput;
    private bool isBraking;   // space = handbrake (rear lock) for the local car

    private void Start()
    {
        if (carRb == null)
            carRb = GetComponent<Rigidbody>();

        if (carRb == null)
            carRb = GetComponentInChildren<Rigidbody>();

        if (carRb == null)
        {
            Debug.LogError("PhotonCarController: No Rigidbody found on " + gameObject.name);
            return;
        }

        if (centerOfMass != null)
            carRb.centerOfMass = centerOfMass.localPosition;

        carRb.interpolation = RigidbodyInterpolation.Interpolate;

        SetupDrift();
    }

    private void SetupDrift()
    {
        if (rearLeftWheel == null || rearRightWheel == null) return;   // retry until refs are wired
        if (driftSetupDone) return;
        driftSetupDone = true;

        // remember the prefab's rear grip so drift grip scaling always starts from it
        baseRearLeftStiffness = rearLeftWheel.sidewaysFriction.stiffness;
        baseRearRightStiffness = rearRightWheel.sidewaysFriction.stiffness;

        // black tire marks under the wheels while skidding
        TireMarks.Ensure(gameObject, new WheelCollider[] { frontLeftWheel, frontRightWheel, rearLeftWheel, rearRightWheel });

        // skid sound , volume follows the wheel slip amount
        SkidSound.Ensure(gameObject, this);

        // tyre smoke while drifting
        DriftSmoke.Ensure(gameObject, this);
    }

    private void FixedUpdate()
    {
        if (carRb == null) return;

        if (useExternalInput)
        {
            // AI car — skip ownership checks, just drive
        }
        else if (photonViewRef != null)
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
            if (frontLeftWheel != null) frontLeftWheel.motorTorque = 0f;
            if (frontRightWheel != null) frontRightWheel.motorTorque = 0f;

            if (frontLeftWheel != null) frontLeftWheel.brakeTorque = brakeForce;
            if (frontRightWheel != null) frontRightWheel.brakeTorque = brakeForce;
            if (rearLeftWheel != null) rearLeftWheel.brakeTorque = brakeForce;
            if (rearRightWheel != null) rearRightWheel.brakeTorque = brakeForce;

            UpdateWheels();
            return;
        }

        GetInputs();

        SetupDrift();
        UpdateDriftAmount();

        HandleMotor();
        HandleSteering();
        HandleBrakes();
        ApplyRearGrip();

        ApplyDownforce();
        ApplyDriftControl();
        ApplyDriftAssist();
        ApplyAntiRoll();

        UpdateWheels();
    }

    public void SetInput(float throttle, float brake, float steer)
    {
        extThrottle = throttle;
        extBrake = brake;
        extSteer = steer;
    }

    private void GetInputs()
    {
        if (useExternalInput)
        {
            throttleInput = extThrottle;
            steeringInput = extSteer;
            isBraking = extBrake > 0.5f;
            return;
        }
        throttleInput = Input.GetAxis("Vertical");
        steeringInput = Input.GetAxis("Horizontal");
        isBraking = Input.GetKey(KeyCode.Space);
    }

    private void HandleMotor()
    {
        if (frontLeftWheel == null || frontRightWheel == null) return;

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
        if (frontLeftWheel == null || frontRightWheel == null) return;

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
        float frontBrake;
        float rearBrake;

        if (useExternalInput)
        {
            // AI / external input uses proportional braking (0..1 of brakeForce)
            frontBrake = rearBrake = Mathf.Clamp01(extBrake) * brakeForce;
        }
        else if (isBraking)
        {
            // handbrake: lock ONLY the rear wheels so the back steps out and the car rotates
            frontBrake = 0f;
            rearBrake = handbrakeForce;
        }
        else
        {
            // S / down arrow = brakes only while rolling forward , reverse once stopped .
            // signed speed matters here: CarSpeed() is a magnitude , so it stays > 1 while
            // driving backwards too — the brakes would then keep fighting the reverse torque
            // and the car only creeps a hair before stalling again .
            float forwardSpeed = carRb != null ? Vector3.Dot(carRb.linearVelocity, transform.forward) : CarSpeed();
            float brakeInput = Mathf.Clamp01(-throttleInput) * (forwardSpeed > 1f ? 1f : 0f);
            frontBrake = rearBrake = brakeInput * brakeForce;
        }

        if (frontLeftWheel != null) frontLeftWheel.brakeTorque = frontBrake;
        if (frontRightWheel != null) frontRightWheel.brakeTorque = frontBrake;
        if (rearLeftWheel != null) rearLeftWheel.brakeTorque = rearBrake;
        if (rearRightWheel != null) rearRightWheel.brakeTorque = rearBrake;
    }

    // 0 = gripping , 1 = full drift — handbrake held or rear wheels sliding
    private void UpdateDriftAmount()
    {
        if (!driftSetupDone)
        {
            driftAmount = 0f;
            return;
        }

        bool handbrake = !useExternalInput && isBraking;

        float rearSlip = 0f;
        int rearCount = 0;
        if (rearLeftWheel.GetGroundHit(out WheelHit hitL)) { rearSlip += Mathf.Abs(hitL.sidewaysSlip); rearCount++; }
        if (rearRightWheel.GetGroundHit(out WheelHit hitR)) { rearSlip += Mathf.Abs(hitR.sidewaysSlip); rearCount++; }
        if (rearCount > 0) rearSlip /= rearCount;

        float target = (handbrake || rearSlip > driftSlipThreshold) ? 1f : 0f;
        float speed = target > driftAmount ? driftGripEnterSpeed : driftGripExitSpeed;
        driftAmount = Mathf.MoveTowards(driftAmount, target, Time.fixedDeltaTime * speed);
    }

    // loosens the rear tyres while drifting so the slide is easy to start and hold
    private void ApplyRearGrip()
    {
        if (!driftSetupDone) return;

        float scale = Mathf.Lerp(1f, driftRearGrip, driftAmount);

        if (rearLeftWheel != null)
        {
            WheelFrictionCurve f = rearLeftWheel.sidewaysFriction;
            f.stiffness = baseRearLeftStiffness * scale;
            rearLeftWheel.sidewaysFriction = f;
        }
        if (rearRightWheel != null)
        {
            WheelFrictionCurve f = rearRightWheel.sidewaysFriction;
            f.stiffness = baseRearRightStiffness * scale;
            rearRightWheel.sidewaysFriction = f;
        }
    }

    // yaw torque while drifting so the car rotates into the slide , follows steer input so counter steer still works
    private void ApplyDriftAssist()
    {
        if (driftYawAssist <= 0f || driftAmount <= 0.01f || carRb == null) return;

        float kph = CarSpeed();
        if (kph < driftAssistMinKPH) return;
        if (Mathf.Abs(steeringInput) < 0.05f) return;

        float speedFactor = Mathf.Clamp01(kph / 100f);
        carRb.AddTorque(transform.up * (steeringInput * driftYawAssist * speedFactor), ForceMode.Acceleration);
    }

    private void ApplyDownforce()
    {
        if (carRb == null) return;
        carRb.AddForce(
            -transform.up * downforce * carRb.linearVelocity.magnitude,
            ForceMode.Force);
    }

    private void ApplyDriftControl()
    {
        if (carRb == null || carRb.isKinematic) return;
        Vector3 localVelocity =
            transform.InverseTransformDirection(carRb.linearVelocity);

        localVelocity.x *= Mathf.Lerp(driftFactor, 1f, driftAmount);

        carRb.linearVelocity =
            transform.TransformDirection(localVelocity);
    }

    private void ApplyAntiRoll()
    {
        if (carRb == null) return;
        if (frontLeftWheel != null && frontRightWheel != null)
            ApplyAntiRollAxle(frontLeftWheel, frontRightWheel);
        if (rearLeftWheel != null && rearRightWheel != null)
            ApplyAntiRollAxle(rearLeftWheel, rearRightWheel);
    }

    private void ApplyAntiRollAxle(
        WheelCollider leftWheel,
        WheelCollider rightWheel)
    {
        if (leftWheel == null || rightWheel == null || carRb == null) return;

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
        if (wheelCollider == null || wheelTransform == null)
            return;

        Vector3 pos;
        Quaternion rot;

        wheelCollider.GetWorldPose(out pos, out rot);

        wheelTransform.position = pos;
        wheelTransform.rotation = rot;
    }

    public float CarSpeed()
    {
        if (carRb == null) return 0f;
        return carRb.linearVelocity.magnitude * 3.6f;
    }

    // Current throttle (positive = gas, negative = brake/reverse).
    // Exposed so systems like CarSound can react to real driving input
    // instead of polling Input directly (which ignores external input).
    public float ThrottleInput => throttleInput;
}