
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(CarStateMachine))]
public class CarStats : MonoBehaviour {

    #region variables , inspector
    public CarStateMachine carStateMachine;

    [Header("camera stats")]
    public List<Vector2> camraPositions = new(3);
    public Vector2 lookAtPoint = new(.42f, 1.5f);
    [Range(0, 20)] public float fowDisplaceAmount = 0.1f;
    [Range(0, 20)] public float fowDisplaceLerpSpeed = 1;
    [Range(50,90)]public int inistalFow = 80;

    [Header("car stats")]
    public driveMode driveMode = driveMode.allWheelDrive;
    [Range(120, 2500)] public int MaxPowerNM = 1200;  //  use NM as newton meters , thats how unity car collider works
    [Tooltip("this will be used to add as a boost , this will only add power to the overall power of the car !")]
    [Range(0, 1000)] public int boostPowerNM = 100;


    // holder variables
    private Vector2 initialLookAtPoint;

    // temp variables
    [Header("temp , for the cam lerping !")]
    public float lerpVal = 0;

    [ContextMenu("Set Camera Position")]
    public void SetCameraPosition() {
        if (carStateMachine == null) {
            try {
                carStateMachine = GetComponent<CarStateMachine>();
            } catch {
                throw new System.Exception("no state machine attached to car");
            }
        }
        if (Camera.main != null) {
            Camera.main.transform.position = new Vector3(transform.position.x + 0, transform.position.y + camraPositions[carStateMachine.cameraindexPos].y, transform.position.z + camraPositions[carStateMachine.cameraindexPos].x);
            Debug.Log("Camera position set!");
        } else {
            Debug.LogError("Camera or target position not found!");
        }
    }
    #endregion

    #region main
    void Awake() {
        initialLookAtPoint = lookAtPoint;
    }

    void Start() {
        carStateMachine = GetComponent<CarStateMachine>();
    }

    void FixedUpdate() {
        lookAtPoint = Vector2.Lerp(lookAtPoint, new Vector2(initialLookAtPoint.x, Mathf.Clamp(carStateMachine.KPH / 60, 0, 1) + initialLookAtPoint.y), Time.deltaTime * lerpVal);

    }
    #endregion

    #region Gizmos
    [Header("Gizmos")]
    [Range(0, 1f)] public float wireSphereRadius = .2f;
    public bool drawOnlyOnSelected = false;
    public bool drawGizmos = true;

    private void OnDrawGizmosSelected() { if (drawOnlyOnSelected && drawGizmos) GizmosLogic(); }

    private void OnDrawGizmos() { if (!drawOnlyOnSelected && drawGizmos) GizmosLogic(); }

    void GizmosLogic() {
        if (carStateMachine == null) {
            try {
                carStateMachine = GetComponent<CarStateMachine>();
            } catch {
                throw new System.Exception("no state machine attached to car");
            }
        }
        foreach (var item in camraPositions) {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.TransformPoint(new Vector3(0, item.y, item.x)), wireSphereRadius);
        }
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.TransformPoint(new Vector3(0, lookAtPoint.y, lookAtPoint.x)), wireSphereRadius);
    }
    #endregion

}

[System.Serializable]
public enum driveMode {
    frontWheelDrive,
    rearWheelDrive,
    allWheelDrive
}


#if UNITY_EDITOR

[CustomEditor(typeof(CarStats))]
public class CameraPositionSetterEditor : Editor {
    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        CarStats script = (CarStats)target;
        if (GUILayout.Button("Set Camera Position")) {
            script.SetCameraPosition();
        }
    }
}
#endif