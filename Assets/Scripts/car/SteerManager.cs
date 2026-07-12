using System;
using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(CarStateMachine))]
public class SteerManager : MonoBehaviour {

    private CarStateMachine stateMachine;

    [Tooltip("this will make the steer have less effect , higher speed = less steer thus evaluate curve at speeds apporpiately")]
    public AnimationCurve steeringCurve;
    [Range(1, 2)] public float curveMod = 1;

    [Header("max turn angle")]
    [Tooltip("lower = more steer")]
    [Range(2, 5)] public float MaxSteerAngle = 4;
    [Tooltip("this will add counter steer mod , making it possible to hold the drift and also be able to correct the steer ! , prefered to be set about 10-15 , lowe = less mod = harder to hold the drift")]
    [Range(0, 3)] public float slipSteerRadiusMultiplier = 1;

    [Header("steering turn speed")]
    [Tooltip("lerped input for turing the wheels , this determines how fast the speed turn , higher float value = faster steer turn")]
    [Range(1, 2)] public float steerTurnSpeed = 1;
    [Tooltip("slow down the steering wheel speed based on the kph , higher = slower steer")]
    [Range(0, 0.003f)] public float steerSpeedMod = 0.001f;


    [Header("debug , needs testing")]
    [Range(1, 5)] public int steerSnapBackSpeed = 2;

    public float overallSlip;
    public float modifiedRadius;
    public float steerModifier = 0;
    public float steerSeed;
    private Vector2 lerpedmoveInput;
    private float wheelbase = 2.5f, trackwidth = 1.5f;

    void Start() {
        stateMachine = GetComponent<CarStateMachine>();
        wheelbase = Vector3.Distance(stateMachine.wheelTransforms[0].position, stateMachine.wheelTransforms[2].position);
        trackwidth = Vector3.Distance(stateMachine.wheelTransforms[0].position, stateMachine.wheelTransforms[1].position);
    }

    void FixedUpdate() {

        overallSlip = Mathf.Lerp(overallSlip, stateMachine.overallSlip, Time.deltaTime * 2);

        // radius calculate , less angle at higher speeds , no understeer this way 
        // calculate sideways slip for when losing traction and needs to be corrected , important to held the drift !
        modifiedRadius = steeringCurve.Evaluate(stateMachine.KPH) * curveMod;
        steerModifier = math.max(0, modifiedRadius - (modifiedRadius * (overallSlip * slipSteerRadiusMultiplier)));


        //concventional input forwading to the wheel turn logic !
        // decrease steer speed by kph
        steerSeed = stateMachine.moveInput.x != 0 ? steerTurnSpeed - (stateMachine.KPH * steerSpeedMod) : steerTurnSpeed * steerSnapBackSpeed;
        lerpedmoveInput = Vector2.Lerp(lerpedmoveInput, stateMachine.moveInput, Time.deltaTime * steerSeed);


        if (lerpedmoveInput.x > 0) {
            stateMachine.wheelColliders[0].steerAngle = Mathf.Rad2Deg * Mathf.Atan(wheelbase / ((MaxSteerAngle + steerModifier) + (trackwidth / 2))) * lerpedmoveInput.x;
            stateMachine.wheelColliders[1].steerAngle = Mathf.Rad2Deg * Mathf.Atan(wheelbase / ((MaxSteerAngle + steerModifier) - (trackwidth / 2))) * lerpedmoveInput.x;
        } else if (lerpedmoveInput.x < 0) {
            stateMachine.wheelColliders[0].steerAngle = Mathf.Rad2Deg * Mathf.Atan(wheelbase / ((MaxSteerAngle + steerModifier) - (trackwidth / 2))) * lerpedmoveInput.x;
            stateMachine.wheelColliders[1].steerAngle = Mathf.Rad2Deg * Mathf.Atan(wheelbase / ((MaxSteerAngle + steerModifier) + (trackwidth / 2))) * lerpedmoveInput.x;
        } else {
            stateMachine.wheelColliders[0].steerAngle = 0;
            stateMachine.wheelColliders[1].steerAngle = 0;
        }



    }

}
