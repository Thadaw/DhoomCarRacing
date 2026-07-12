using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

[RequireComponent(typeof(CarStateMachine))]
public class scriptableRaccer : MonoBehaviour {
    private CarStateMachine stateMachine;

    public Spline Map;

    public bool drawOnlyOnSelected = false;
    [Range(0, 1)] public float SphereRadius;


    public trackWaypoints track;

    public int currentnode = 0;
    [Range(1, 100)] public float speedModifier = 2;
    [Range(0, 100)] public float cornerSpeedModifier = 2;


    [Range(0.1f, 10f)] public float minDistance = 0.1f;
    [Range(1, 50)] public float maxDistance = 20;

    [Header("next node angle in degrees")]
    public float nextnodeangle;

    public float steerAffect = 0;


    [Header("debug")]
    [Range(0, 1)] public float throttle = 1;
    [Range(0, 1)] public float brakeMultiplier = 1;
    [Range(0, 10)] public float tmpMultiplier = 1;
    public float turnAmount = 0;
    public float brakeAmount = 0;
    public float distToNextNode = 0;

    public float angleMultiplier = 0;

    [Range(0, 200)] public float topSpeed = 200;
    [Range(0, 1)] public float currentSpeed = 1;    // this goes only from 0 to 1
    [Range(0, 200)] public float currentSpeedKPH = 1;   // this for visualising

    public float amountToAddtoRigidBody = 1; // this is used to rubber band the ai racer !
    [Range(0, 200)] public float amountToAddtoRigidBodyTorque = 1;
    public float debug1 = 0;

    void Start() {
        stateMachine = GetComponent<CarStateMachine>();

        //transform.position = track.nodes[0].position;
        //currentnode = 0;
    }


    void FixedUpdate() {
        if (track.nodes.Count < 2) return;

        int nextIndex = Mathf.Clamp(currentnode + 1, 0, track.nodes.Count - 1);
        distToNextNode = Vector3.Distance(transform.position, track.nodes[nextIndex].position);
        // this is a simple closed node list , linked list 
        if (distToNextNode <= minDistance) {
            currentnode++;
            if (currentnode >= track.nodes.Count - 2) {
                currentnode = 0;
            }
        }

        if (distToNextNode >= maxDistance && currentnode != 0) {
            currentnode--;
        }

        currentnode = Mathf.Clamp(currentnode, 0, track.nodes.Count - 1);

        // this determines how curved the next curve is thus giving us a braking value , so we can stop before the curve or turn
        if (currentnode <= track.nodes.Count - 3) {
            //Vector3.Angle(A - B, C - B)
            nextnodeangle = Vector3.Angle(transform.position - track.nodes[currentnode + 1].position, track.nodes[currentnode + 2].position - track.nodes[currentnode + 1].position);
            //steerAffect = Mathf.Clamp(Mathf.Abs(180 - nextnodeangle), 0, 180) / 180 * Mathf.Clamp(cornerSpeedModifier, 0, speedModifier);
            steerAffect = Mathf.Lerp(steerAffect, Mathf.Clamp(Mathf.Abs(180 - nextnodeangle), 0, 180) / 180 * cornerSpeedModifier, Time.deltaTime * 2);
        }


        // this determines the angel between us and the point that we look at !
        Vector3 relative = transform.InverseTransformPoint(track.nodes[currentnode + 1].position);
        relative /= relative.magnitude;

        turnAmount = relative.x / relative.magnitude;   // actual turn wheel amount

        brakeAmount = Mathf.Clamp(steerAffect * brakeMultiplier, 0, 1);  // actual brake vehicle
        angleMultiplier = tmpMultiplier * Mathf.Abs(steerAffect);

        currentSpeed = Math.Clamp(1 - angleMultiplier, .1f, 1);
        currentSpeedKPH = topSpeed * currentSpeed;
        debug1 = (currentSpeedKPH - stateMachine.KPH) * amountToAddtoRigidBody;
        stateMachine.rigidbody.AddForce(transform.forward * debug1, ForceMode.Acceleration);
        stateMachine.rigidbody.AddTorque((transform.up * turnAmount) * amountToAddtoRigidBodyTorque, ForceMode.Acceleration);
        // calculate the speed that should be now, that is the amount we add , remove 


        if (gameObject.CompareTag("ai")) {
            stateMachine.moveInput = new Vector2(turnAmount, throttle - brakeAmount);
        }
    }



    #region gizmos 

    private void OnDrawGizmosSelected() {
        if (drawOnlyOnSelected) gizmosLogic();
    }

    private void OnDrawGizmos() {
        if (!drawOnlyOnSelected) gizmosLogic();

    }

    void gizmosLogic() {
        if (track.nodes.Count < 3) return; // Need at least 3 nodes to draw this angle

        // Ensure we don't go out of bounds for currentnode + 1 and currentnode + 2
        if (currentnode <= track.nodes.Count - 3) {
            Vector3 A = transform.position;
            Vector3 B = track.nodes[currentnode + 1].position;
            Vector3 C = track.nodes[currentnode + 2].position;

            // --- Draw the Vertex B (the next node) ---
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(B, SphereRadius);

            // --- Draw the two rays forming the angle ---

            // Ray 1: From B to A (Agent's position)
            Gizmos.color = Color.cyan; // Color for the ray to the agent
            Gizmos.DrawLine(B, A);

            // Ray 2: From B to C (Node after next)
            Gizmos.color = Color.magenta; // Color for the ray to the subsequent node
            Gizmos.DrawLine(B, C);

            // Optional: Draw a line from A (Agent) to C (Node after next) to complete a triangle
            // Gizmos.color = Color.gray;
            // Gizmos.DrawLine(A, C);
        }
    }


    #endregion


}
