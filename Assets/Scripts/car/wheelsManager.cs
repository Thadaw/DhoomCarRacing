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
        for (int i = 0; i < stateMachine.wheelColliders.Length; i++) {

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

        //sidewaysSplipSim = 0;
        for (int i = 0; i < stateMachine.wheelColliders.Length; i++) {
            if (stateMachine.wheelColliders[i].GetGroundHit(out WheelHit hit)) {

                forwardSlip[i] = Mathf.Abs(hit.forwardSlip);
                sidewaysSlip[i] = Mathf.Abs(hit.sidewaysSlip);

                overallSlip[i] = Mathf.Abs(hit.forwardSlip) + Mathf.Abs(hit.sidewaysSlip);

                forwardFriction = stateMachine.wheelColliders[i].forwardFriction;
                newStiffnessForward[i] = slipFrictionCurve.Evaluate(overallSlip[i]) * curveModifier;
                forwardFriction.stiffness = newStiffnessForward[i];
                stateMachine.wheelColliders[i].forwardFriction = forwardFriction;

                sidewaysFriction = stateMachine.wheelColliders[i].sidewaysFriction;
                newStiffnessSideways[i] = slipFrictionCurve.Evaluate(overallSlip[i]) * curveModifier;
                sidewaysFriction.stiffness = newStiffnessSideways[i];
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

        }

        int wheelCount = stateMachine.wheelColliders.Length;
        if (wheelCount > 0) {
            stateMachine.overallSlip = overallSlip.Sum() / wheelCount;
            stateMachine.overallSidewaysSlip = sidewaysSlip.Sum() / wheelCount;
            stateMachine.overallForwardSlip = forwardSlip.Sum() / wheelCount;
        }

        //smoothedSidewaysSplipSim = Mathf.Lerp(smoothedSidewaysSplipSim, sidewaysSplipSim, Time.deltaTime * 4);
        //slipRadiusModifier = Mathf.Clamp(Mathf.Abs(smoothedSidewaysSplipSim) * slipSteerRadiusMultiplier, 0, modifiedRadius - stateMachine.CarStats.MaxSteerAngle);
        //modifiedRadius = Mathf.Lerp(modifiedRadius, stateMachine.KPH * (steeringRadiusModifier * steeringRadiusMultiplier), Time.deltaTime * 2);

        //stateMachine.steerModifier = modifiedRadius - slipRadiusModifier;

    }


    #region gui
     [Header("gui")]
     [HideInInspector] public float GuiXPos = 0;
     [HideInInspector] public float GuiYPos = 0;
     [HideInInspector] public float GuiYSpace = 1;
     [HideInInspector] public GUIStyle customStyle = new();
     [HideInInspector] public float GuiCellWidth = 200;
     [HideInInspector] public float GuiCellHeight = 20;

    void OnGUI() {
        return;
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
