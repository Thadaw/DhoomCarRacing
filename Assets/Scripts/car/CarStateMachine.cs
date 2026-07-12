using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Splines;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CarStats))]
[RequireComponent(typeof(CarController))]
[RequireComponent(typeof(SteerManager))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(WheelsManager))]
[RequireComponent(typeof(PlayerInput))]
public class CarStateMachine : MonoBehaviour {

    #region StateMachine
    // not good practice to expose these as public but i just happen to be lazy
    [HideInInspector] public CarStats CarStats;
    [HideInInspector] public CarController CarController;
    [HideInInspector] public SteerManager SteerManager;
    [HideInInspector] public WheelsManager WheelsManager;
    [HideInInspector] public SplineContainer splineContainer;
    [HideInInspector] public EngineController engineController;

    [Header("Car Parts")]
    [HideInInspector] public WheelCollider[] wheelColliders;
    [HideInInspector] public Transform[] wheelTransforms;
    public ParticleSystem[] nitrus;
    [HideInInspector] public Rigidbody rigidbody;
    public Transform centerOfMassTransform;

    //camera
    [HideInInspector] public int cameraindexPos = 0;

    //car
    [HideInInspector] public float KPH = 0;
    [HideInInspector] public int boostNm = 0; // this will add , not multiply to the overall power of the vehicle


    [Header("effects")]
    public int selectedPowerupIndex = 0;
    public List<Powerup> powerups;


    [Header("ui elements")]
    public GameObject powerupsUiContainer;
    public GameObject reusablePowerupsUiContainer;
    [HideInInspector] public List<RectTransform> reusablePowerupsUiObjects;

    [Header("outside variables")]
     public float overallSlip = 0;
    [HideInInspector] public float overallSidewaysSlip = 0;
    [HideInInspector] public float overallForwardSlip = 0;

    [Header("debug")]
    [HideInInspector] public float splinePositionFloat = 0;

    [Header("inputs")]
    [HideInInspector] public Vector2 moveInput;
    public bool isSpacebarPressed;
    public bool isShiftPressed;
    #endregion

    #region initializer
    void Awake() => FindValues();

    public void FindValues() {
        CarController = GetComponent<CarController>();
        SteerManager = GetComponent<SteerManager>();
        WheelsManager = GetComponent<WheelsManager>();
        CarStats = GetComponent<CarStats>();
        engineController = GetComponent<EngineController>();
        splineContainer = GameObject.FindWithTag("track")?.GetComponent<SplineContainer>();

        rigidbody = GetComponent<Rigidbody>();
        var com = transform.Find("CenterOfMass");
        centerOfMassTransform = com != null ? com : transform;
        foreach (Transform i in gameObject.transform) {
            if (i.transform.name == "carColliders") {
                wheelColliders = new WheelCollider[i.transform.childCount];
                for (int q = 0; q < i.transform.childCount; q++) {
                    wheelColliders[q] = i.transform.GetChild(q).GetComponent<WheelCollider>();
                }
            }
            if (i.transform.name == "carWheels") {
                wheelTransforms = new Transform[i.transform.childCount];
                for (int q = 0; q < i.transform.childCount; q++) {
                    wheelTransforms[q] = i.transform.GetChild(q);
                }
            }
        }

        // initial values 
        if (centerOfMassTransform) {
            print("found center of mass transform");
            rigidbody.centerOfMass = centerOfMassTransform.localPosition;
        } else {
            print("failed to find center of mass transform");
        }
    }
    #endregion

    #region getters and setters
    public bool AddPowerup(Powerup p) {
        if (powerups.Count < 3) {
            // this is using the select to basicly map thru the array , the hashset is just a collection of elemets unique . 
            var existingIndices = powerups.Select(pu => pu.index).ToHashSet();
            int nextIndex = 0;
            while (existingIndices.Contains(nextIndex)) {
                nextIndex++;
            }
            Powerup _powerUp = p;
            _powerUp.index = nextIndex;
            powerups.Add(_powerUp);
            return true;
        } else {
            return false;
        }
    }
    #endregion

    #region inputs

    public void OnMove(InputValue value) {
        if (CompareTag("Player")) moveInput = value.Get<Vector2>();
    }
    public void OnJump(InputValue input) {
        if (gameObject.CompareTag("Player")) isSpacebarPressed = input.isPressed;
    }

    public void OnSprint(InputValue value) {
        if (CompareTag("Player")) isShiftPressed = value.isPressed;
    }

    #endregion

}

[System.Serializable]
public enum PowerupType {
    nitrus, rocket, shield
}

[System.Serializable]
public struct Powerup {
    public PowerupType type;
    public int index;
}