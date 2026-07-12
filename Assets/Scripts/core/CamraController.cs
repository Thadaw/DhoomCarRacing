using Unity.Mathematics;
using UnityEngine;
using System.Linq;

public class CamraController : MonoBehaviour {
    private CarStateMachine carStateMachine;
    private Transform targetTransform;

    private Vector2 offset = new Vector2(0, 0);

    [Range(0.1f, 5)] public float siddewaysLerpSpeed = 1;

    [Range(0.1f, 2)] public float maxDistanceToTarget = 4;

    private float distanceToTarget;
    private float tmp;
    private float result;

    void Start() {
        carStateMachine = GameObject.FindGameObjectsWithTag("Player").FirstOrDefault()?.GetComponent<CarStateMachine>();
        if (carStateMachine == null || carStateMachine.CarStats == null) {
            print("no player found");
            return;
        }
        targetTransform = carStateMachine.transform;
        if (carStateMachine.CarStats.camraPositions != null && carStateMachine.CarStats.camraPositions.Count > 0)
        offset = carStateMachine.CarStats.camraPositions != null && carStateMachine.CarStats.camraPositions.Count > 0
            ? carStateMachine.CarStats.camraPositions[carStateMachine.cameraindexPos]
            : offset;
        transform.position = targetTransform.TransformPoint(new Vector3(0, offset.y, offset.x));
    }



    void FixedUpdate() {
        if (carStateMachine == null || carStateMachine.CarStats == null) return;

        transform.LookAt(targetTransform.TransformPoint(new Vector3(0, carStateMachine.CarStats.lookAtPoint.y, carStateMachine.CarStats.lookAtPoint.x)));
        offset = carStateMachine.CarStats.camraPositions[carStateMachine.cameraindexPos];

        // use this to do the siddeways smoothing follow
        transform.position = Vector3.Lerp(transform.position,
                    targetTransform.TransformPoint(new Vector3(0, offset.y, offset.x)),
                    Time.deltaTime * siddewaysLerpSpeed);

        //add this so the car dont go too far , since we do lerp earlier for the sideways effect
        transform.localPosition += transform.forward * (result / 2);  // replace 2 with a greater value to make the rubber bad effect weaker

        //used to calculate dist between cam offset and target
        distanceToTarget = Vector3.Distance(transform.position, targetTransform.position);
        tmp = distanceToTarget - offset.magnitude;
        // this will threshold , and then increase exponentially 
        result = Mathf.Sign(tmp) * Mathf.Pow(Mathf.Max(0f, Mathf.Abs(tmp) - maxDistanceToTarget), 2);


    }
}
