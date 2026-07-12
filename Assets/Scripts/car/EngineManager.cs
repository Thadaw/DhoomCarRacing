using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(CarStateMachine))]
public class EngineController : MonoBehaviour {
    private CarStateMachine stateMachine;

    [Header("Gear Settings")]
    [Range(300, 2000)] public float idleRPM = 1000f;
    [Range(300, 4000)] public float minRPM = 3000f;
    [Range(600, 13000)] public float maxRPM = 1000f;
    [Range(0, 20)] public float engineSmoothDamp = 9f;
    [Range(1, 7)] public float finalDrive = 3.6f;
    [Range(0.20f, 10f)] public float[] gears;
    public AnimationCurve enginePower;

    [HideInInspector] public int gearNum = 1;
    [HideInInspector] public float engineRPM;
    private float totalPower;
    private float torquePerWheel;

    [Header("shifter settings")]
    [Tooltip("this will remove rpm from rpm instantly to make the shifting a little more realistic and nice to look at !")]
    [Range(100, 3000)] public float upShiftRpmBounce = 550;
    [Range(50, 2000)] public float maxRpmBounceBackRPMs = 200;

    [Header("Braking")]
    [Range(800, 200000)] public float brakePower = 1000f;
    [Range(100, 200000)] public float handBrakePower = 2000f;
    private float handBrakeTorque;
    private List<float> absWheelRPMs = new();  // absolute to only store positive values
    public float throttleInput, brakeInput, velocity;

    [Header("shifter")]
    [Tooltip("me average wheel slip'drift' value requiered to allow for upshifting")]
    [Range(0.01f, 0.5f)] public float minSlipTpUpshift = .2f;
    float wheelsAvgRPM = 0;
    [Tooltip("this takes the max rpm * this '11000 * 0.01 > 800' and compares if rpm over this value to provide no power to the wheels . Rev limiter ")]
    float maxRpmProtectVal = 0.04f;     // value to not reach max rpm , used to prevent engine reving over the max rpm


    //tmp
    float engineLoad = 0;

    float calcRPM = 0;


    void Start() {
        stateMachine = GetComponent<CarStateMachine>();
        //  initialize variables
        if (absWheelRPMs.Count != stateMachine.wheelColliders.Length) absWheelRPMs = new List<float>(new float[stateMachine.wheelColliders.Length]);

    }

    void FixedUpdate() {
        automaticShifter();
    }

    private void Update() {
        // Separate throttle and brake inputs more clearly
        throttleInput = Mathf.Clamp01(stateMachine.moveInput.y); // Positive input for throttle
        brakeInput = Math.Abs(Mathf.Clamp(stateMachine.moveInput.y, -1f, 0f));   // Negative input for brake

        CalculateEnginePower();
        MoveVehicle();

        if (Input.GetKeyDown(KeyCode.F)) UpShift();
        if (Input.GetKeyDown(KeyCode.Q)) DownShift();
    }

    private void CalculateEnginePower() {
        WheelRPM();
        wheelsAvgRPM = Mathf.Lerp(wheelsAvgRPM, absWheelRPMs.Average(), Time.deltaTime * 3.5f); // damped average , so we dont get wiggly engine RPM ps. adjust the 3.5f if needed to smooth it even more !

        calcRPM = math.clamp(gearNum < 1 ? (throttleInput * maxRPM) : (wheelsAvgRPM * finalDrive * gears[gearNum]), idleRPM, maxRPM + 100);
        if (engineRPM >= maxRPM) engineRPM -= maxRpmBounceBackRPMs;
        engineRPM = Mathf.Lerp(engineRPM, calcRPM, Time.deltaTime * engineSmoothDamp);

        // do not apply any power if engine is over the rpm 
        totalPower = finalDrive * (gearNum > 0 && engineRPM < maxRPM - (maxRPM * maxRpmProtectVal) ? enginePower.Evaluate(engineRPM) * throttleInput : 0);

        // calculate the load , clamp so it dont go below 0 
        // used to the engine audio script 
        engineLoad = math.lerp(engineLoad, math.clamp(math.abs(stateMachine.moveInput.y) - (engineRPM / maxRPM), 0, 1), Time.deltaTime * 2);

    }

    private void WheelRPM() {
        for (int i = 0; i < stateMachine.wheelColliders.Length; i++) {
            absWheelRPMs[i] = Mathf.Abs(stateMachine.wheelColliders[i].rpm);
        }
    }

    public float brakeTPM;

    private void MoveVehicle() {
        // Calculate torque based on drive mode
        int drivenWheels = stateMachine.CarStats.driveMode == driveMode.allWheelDrive ? 4 : 2;
        torquePerWheel = engineRPM >=  maxRPM - (engineRPM * maxRpmProtectVal) ? 0 : (totalPower / drivenWheels) + stateMachine.boostNm;

        // Apply handbrake (only to rear wheels)
        handBrakeTorque = Input.GetKey(KeyCode.Space) ? handBrakePower : 0f;

        // Apply torque based on drive mode
        switch (stateMachine.CarStats.driveMode) {
            case driveMode.frontWheelDrive:
                stateMachine.wheelColliders[0].motorTorque = torquePerWheel;
                stateMachine.wheelColliders[1].motorTorque = torquePerWheel;
                stateMachine.wheelColliders[2].motorTorque = 0f;
                stateMachine.wheelColliders[3].motorTorque = 0f;
                break;
            case driveMode.rearWheelDrive:
                // no power or brake at front wheels 
                stateMachine.wheelColliders[0].motorTorque = 0f;
                stateMachine.wheelColliders[1].motorTorque = 0f;
                // power als long as engine is not over the max 
                stateMachine.wheelColliders[2].motorTorque = torquePerWheel;
                stateMachine.wheelColliders[3].motorTorque = torquePerWheel;
                break;
            case driveMode.allWheelDrive:
                for (int i = 0; i < stateMachine.wheelColliders.Length; i++) {
                    stateMachine.wheelColliders[i].motorTorque = torquePerWheel;
                }
                break;
        }

        brakeTPM = engineRPM >= maxRPM ? brakePower : brakeInput * brakePower;
        // Apply brakes to all wheels and handbrake to rear wheels
        for (int i = 0; i < stateMachine.wheelColliders.Length; i++) {
            // Set brake torque directly for all wheels
            // if revs too high apply brakes 
            stateMachine.wheelColliders[i].brakeTorque = brakeTPM;
        }

        // Apply handbrake only to rear wheels
        stateMachine.wheelColliders[2].brakeTorque = handBrakeTorque;
        stateMachine.wheelColliders[3].brakeTorque = handBrakeTorque;
    }

    #region shifter

    void automaticShifter() {
        if (gearNum > 0) {
            // moving >
            if (stateMachine.overallSlip < minSlipTpUpshift && engineRPM > maxRPM - (maxRPM * maxRpmProtectVal) && gearNum < gears.Length - 1) UpShift();
            if (engineRPM < minRPM + 2000 && gearNum > (throttleInput == 0 ? 0 : 1)) DownShift();
        } else if (throttleInput > 0) UpShift();
    }

    bool upShiftTimeout = false;
    void DownShift() {
        gearNum = Mathf.Max(gearNum - 1, 0);
        if (gearNum > 1)
            engineRPM += upShiftRpmBounce;
    }

    void UpShift() {
        if (upShiftTimeout) return;
        upShiftTimeout = true;

        gearNum = Mathf.Min(gearNum + 1, gears.Length - 1);
        if (gearNum > 1)
            engineRPM -= upShiftRpmBounce;

        StartCoroutine(UpShiftCooldown());
    }

    IEnumerator UpShiftCooldown() {
        yield return new WaitForSeconds(0.5f);
        upShiftTimeout = false;
    }

    #endregion

    #region GUI
    [Header("GUI")]
    public float GuiXPos = 0;
    public float GuiYPos = 0;
    public float GuiYSpace = 20;
    public GUIStyle customStyle = new();
    public float GuiCellWidth = 200;
    public float GuiCellHeight = 20;

    void OnGUI() {
        if(!CompareTag("Player"))return;
        float pos = GuiYPos;
        GUI.HorizontalSlider(new Rect(GuiXPos, pos, GuiCellWidth, GuiCellHeight), engineRPM, idleRPM, maxRPM);
        pos += GuiYSpace;
        GUI.Label(new Rect(GuiXPos, pos, GuiCellWidth, GuiCellHeight), $"Gear: {gearNum} ({gears[gearNum]:F2})", customStyle);
        pos += GuiYSpace;
        GUI.Label(new Rect(GuiXPos, pos, GuiCellWidth, GuiCellHeight), $"RPM: {engineRPM:F0}", customStyle);
        pos += GuiYSpace;
        GUI.Label(new Rect(GuiXPos, pos, GuiCellWidth, GuiCellHeight), $"Total horsepower: {totalPower:F2}", customStyle);
        pos += GuiYSpace;
        GUI.Label(new Rect(GuiXPos, pos, GuiCellWidth, GuiCellHeight), $"wheels rpm: {wheelsAvgRPM:F2}", customStyle);
        pos += GuiYSpace;
    }
    #endregion
}