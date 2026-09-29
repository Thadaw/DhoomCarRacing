using System;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(CarStateMachine))]
public class WheelsManager : MonoBehaviour {

    private CarStateMachine stateMachine;

    private WheelFrictionCurve forwardFriction, sidewaysFriction;
    //carController controller;

    [Header("curve friction")]
    public AnimationCurve slipFrictionCurve;

    [Header("mods")]
    [Range(1,2)]public float curveModifier = 1;

    [Header("drift grip")]
    [Tooltip("rear sideways stiffness multiplier while drifting , lower = rear breaks loose easier = easier to drift")]
    [Range(0.1f, 1f)] public float driftRearGrip = 0.45f;
    [Tooltip("front sideways stiffness multiplier while drifting , keep some bite so the counter steer still works")]
    [Range(0.1f, 1f)] public float driftFrontGrip = 0.85f;
    [Tooltip("how fast the grip drops when the drift starts")]
    [Range(1, 15)] public float driftGripEnterSpeed = 8f;
    [Tooltip("how fast the grip comes back when the drift ends")]
    [Range(1, 15)] public float driftGripExitSpeed = 5f;
    [Tooltip("rear sideways slip considered 'drifting' even without the handbrake (throttle induced slides)")]
    [Range(0.1f, 1f)] public float driftSlipThreshold = 0.35f;
    [HideInInspector] public float driftAmount = 0f; // 0 = full grip , 1 = full drift grip  (read by camera fx etc)

    [Header("tire marks")]
    public bool tireMarksEnabled = true;
    [Tooltip("wheel slip needed before the tyre leaves a black mark")]
    [Range(0.05f, 1f)] public float skidSlipThreshold = 0.25f;
    [Range(0.05f, 0.6f)] public float tireMarkWidth = 0.22f;
    [Tooltip("seconds before the tire mark fades away")]
    [Range(1, 15)] public float tireMarkDuration = 6f;
    private TrailRenderer[] tireTrails;
    private static Material tireMarkMaterial;

    private float[] forwardSlip;
    private float[] sidewaysSlip;
    private float[] overallSlip;
    private float[] newStiffnessForward;
    private float[] newStiffnessSideways;

    // animationg the wheels , 

    private Vector3 wheelPosition;
    private Quaternion wheelRotation;


    void Start() {
        stateMachine = GetComponent<CarStateMachine>();
        SetUpWheels();
    }

    void SetUpWheels() {
        forwardSlip = new float[4];
        sidewaysSlip = new float[4];
        overallSlip = new float[4];
        newStiffnessForward = new float[4];
        newStiffnessSideways = new float[4];
        tireTrails = tireMarksEnabled ? new TrailRenderer[stateMachine.wheelColliders.Length] : null;
        for (int i = 0; i < stateMachine.wheelColliders.Length; i++) {

            if (tireMarksEnabled) CreateTireMark(i);

            forwardFriction = stateMachine.wheelColliders[i].forwardFriction;

            forwardFriction.asymptoteValue = 1;
            forwardFriction.extremumSlip = 0.065f;
            forwardFriction.asymptoteSlip = 0.8f;
            //curve.stiffness = (inputM.vertical < 0)? ForwardFriction * 2 :ForwardFriction ;
            stateMachine.wheelColliders[i].forwardFriction = forwardFriction;

            sidewaysFriction = stateMachine.wheelColliders[i].sidewaysFriction;

            sidewaysFriction.asymptoteValue = 1;
            sidewaysFriction.extremumSlip = 0.065f;
            sidewaysFriction.asymptoteSlip = 0.8f;
            //curve.stiffness = (inputM.vertical < 0)? SidewaysFriction * 2 :SidewaysFriction ;
            stateMachine.wheelColliders[i].sidewaysFriction = sidewaysFriction;

        }
    }

    void Update() {
        ManageFriction();
    }

    void ManageFriction() {

        UpdateDriftAmount();

        //sidewaysSplipSim = 0;
        bool grounded = false, skidding = false;
        for (int i = 0; i < stateMachine.wheelColliders.Length; i++) {
            grounded = false; skidding = false;
            if (stateMachine.wheelColliders[i].GetGroundHit(out WheelHit hit)) {

                grounded = true;
                forwardSlip[i] = Mathf.Abs(hit.forwardSlip);
                sidewaysSlip[i] = Mathf.Abs(hit.sidewaysSlip);

                overallSlip[i] = Mathf.Abs(hit.forwardSlip) + Mathf.Abs(hit.sidewaysSlip);
                skidding = overallSlip[i] > skidSlipThreshold;

                forwardFriction = stateMachine.wheelColliders[i].forwardFriction;
                newStiffnessForward[i] = slipFrictionCurve.Evaluate(overallSlip[i]) * curveModifier;
                forwardFriction.stiffness = newStiffnessForward[i];
                stateMachine.wheelColliders[i].forwardFriction = forwardFriction;

                sidewaysFriction = stateMachine.wheelColliders[i].sidewaysFriction;
                newStiffnessSideways[i] = slipFrictionCurve.Evaluate(overallSlip[i]) * curveModifier;

                // while drifting the grip is scaled down so the slide is easy to start and hold ,
                // rear looses the most grip , front keeps enough bite for counter steering !
                float gripScale = i >= 2
                    ? Mathf.Lerp(1f, driftRearGrip, driftAmount)
                    : Mathf.Lerp(1f, driftFrontGrip, driftAmount);

                sidewaysFriction.stiffness = newStiffnessSideways[i] * gripScale;
                stateMachine.wheelColliders[i].sidewaysFriction = sidewaysFriction;

                //sidewaysSplipSim += Mathf.Abs(hit.sidewaysSlip); // getting the slip only for the rear wheels , when sideways sliping !

                //if (i > 1) sidewaysSplipSim += Mathf.Abs(hit.sidewaysSlip); // getting the slip only for the rear wheels , when sideways sliping !
            }

            // adding rotation to the wheels 3d objects !
            stateMachine.wheelColliders[i].GetWorldPose(out wheelPosition, out wheelRotation);
            stateMachine.wheelTransforms[i].transform.localRotation = Quaternion.Euler(0, stateMachine.wheelColliders[i].steerAngle, 0);                                    //steer rotation
            if (i % 2 != 0) {
                stateMachine.wheelTransforms[i].transform.GetChild(0).transform.Rotate(stateMachine.wheelColliders[i].rpm * -6.6f * Time.deltaTime, 0, 0, Space.Self);      //engine rotation
            } else {
                stateMachine.wheelTransforms[i].transform.GetChild(0).transform.Rotate(stateMachine.wheelColliders[i].rpm * 6.6f * Time.deltaTime, 0, 0, Space.Self);       //engine rotation
            }
            stateMachine.wheelTransforms[i].transform.position = wheelPosition;

            // tire black marks follow the wheel contact patch , only emit while skidding
            UpdateTireMark(i, wheelPosition, grounded, skidding);

        }

        int wheelCount = stateMachine.wheelColliders.Length;
        if (wheelCount > 0) {
            stateMachine.overallSlip = overallSlip.Sum() / wheelCount;
            stateMachine.overallSidewaysSlip = sidewaysSlip.Sum() / wheelCount;
            stateMachine.overallForwardSlip = forwardSlip.Sum() / wheelCount;
        }

        //smoothedSidewaysSplipSim = Mathf.Lerp(smoothedSidewaysSplipSim, sidewaysSplipSim, Time.deltaTime * 4);
    }

    #region drift grip
    // decides how 'loose' the car is , 0 = full grip , 1 = full drift grip
    void UpdateDriftAmount() {
        bool handbrake = Input.GetKey(KeyCode.Space) || stateMachine.isSpacebarPressed;

        bool rearSlipping = false;
        if (stateMachine.wheelColliders.Length > 3)
            rearSlipping = sidewaysSlip[2] > driftSlipThreshold || sidewaysSlip[3] > driftSlipThreshold;

        float target = (handbrake || rearSlipping) ? 1f : 0f;
        float speed = target > driftAmount ? driftGripEnterSpeed : driftGripExitSpeed;
        driftAmount = Mathf.MoveTowards(driftAmount, target, Time.deltaTime * speed);
    }
    #endregion

    #region tire marks
    void CreateTireMark(int index) {
        if (tireTrails == null || tireTrails[index] != null) return;

        GameObject mark = new GameObject("TireMark_" + index);
        TrailRenderer trail = mark.AddComponent<TrailRenderer>();
        trail.time = tireMarkDuration;
        trail.minVertexDistance = 0.03f;
        trail.startWidth = tireMarkWidth;
        trail.endWidth = tireMarkWidth;
        trail.numCapVertices = 2;
        trail.numCornerVertices = 2;
        trail.startColor = new Color(0f, 0f, 0f, 0.8f);
        trail.endColor = new Color(0f, 0f, 0f, 0f);
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;
        trail.emitting = false;

        Material mat = TireMarkMaterial();
        if (mat != null) trail.material = mat;

        tireTrails[index] = trail;
    }

    static Material TireMarkMaterial() {
        if (tireMarkMaterial != null) return tireMarkMaterial;

        // unlit shader that supports vertex alpha so the mark can fade out
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("UI/Default");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) return null;

        tireMarkMaterial = new Material(shader);
        return tireMarkMaterial;
    }

    void UpdateTireMark(int index, Vector3 wheelCenter, bool grounded, bool skidding) {
        if (tireTrails == null || index >= tireTrails.Length || tireTrails[index] == null) return;

        TrailRenderer trail = tireTrails[index];
        // contact patch = wheel centre pushed down to the ground
        trail.transform.position = wheelCenter - stateMachine.wheelColliders[index].transform.up * stateMachine.wheelColliders[index].radius;

        bool emit = tireMarksEnabled && grounded && skidding;
        if (trail.emitting != emit) trail.emitting = emit;
    }
    #endregion


    #region gui
     [Header("gui")]
     [HideInInspector] public float GuiXPos = 0;
     [HideInInspector] public float GuiYPos = 0;
     [HideInInspector] public float GuiYSpace = 1;
     [HideInInspector] public GUIStyle customStyle = new();
     [HideInInspector] public float GuiCellWidth = 200;
     [HideInInspector] public float GuiCellHeight = 20;

    void OnGUI() {
        float pos = GuiYPos;

        // forwardSlip
        string forwardSlipString = "";
        foreach (float slipValue in forwardSlip) forwardSlipString += Mathf.Abs(slipValue).ToString("0.0") + " ";
        GUI.Label(new Rect(GuiXPos, pos, GuiCellWidth, GuiCellHeight), forwardSlipString.TrimEnd() + " forward", customStyle);
        pos += GuiYSpace;

        // newStiffnessForward
        string stiffnessForwardString = "";
        foreach (float slipValue in newStiffnessForward) stiffnessForwardString += Mathf.Abs(slipValue).ToString("0.0") + " ";
        GUI.Label(new Rect(GuiXPos, pos, GuiCellWidth, GuiCellHeight), stiffnessForwardString.TrimEnd() + " stiffnes Forward", customStyle);
        pos += GuiYSpace;

        // sidewaysSlip
        string sidewaysSlipString = "";
        foreach (float slipValue in sidewaysSlip) sidewaysSlipString += Mathf.Abs(slipValue).ToString("0.0") + " ";
        GUI.Label(new Rect(GuiXPos, pos, GuiCellWidth, GuiCellHeight), sidewaysSlipString.TrimEnd() + " sideways", customStyle);
        pos += GuiYSpace;

        // newStiffnessSideways
        string stiffnessSidewaysString = "";
        foreach (float slipValue in newStiffnessSideways) stiffnessSidewaysString += Mathf.Abs(slipValue).ToString("0.0") + " ";
        GUI.Label(new Rect(GuiXPos, pos, GuiCellWidth, GuiCellHeight), stiffnessSidewaysString.TrimEnd() + " stiffnes Sideways", customStyle);
        pos += GuiYSpace; // No increment needed after the last item

        // overallSlip
        string overallSlipString = "";
        foreach (float slipValue in overallSlip) overallSlipString += Mathf.Abs(slipValue).ToString("0.0") + " ";
        GUI.Label(new Rect(GuiXPos, pos, GuiCellWidth, GuiCellHeight), overallSlipString.TrimEnd() + " slip", customStyle);
        pos += GuiYSpace;



    }
    #endregion

}
