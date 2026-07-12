using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AntiRollBar : MonoBehaviour {

	private CarStateMachine carStateMachine;


	private WheelCollider WheelL;
	private WheelCollider WheelR;
	[Range(100, 10000)] public float intensity = 5000.0f;


	void Start() {
		carStateMachine = GetComponent<CarStateMachine>();
		if (carStateMachine) {
			WheelL = carStateMachine.wheelColliders[0];
			WheelR = carStateMachine.wheelColliders[1];
		}
	}

	void FixedUpdate() {
		WheelHit hit;
		float travelL = 1.0f;
		float travelR = 1.0f;


		bool groundedL = WheelL.GetGroundHit(out hit);
		if (groundedL) {
			travelL = (-WheelL.transform.InverseTransformPoint(hit.point).y - WheelL.radius) / WheelL.suspensionDistance;
		}

		bool groundedR = WheelR.GetGroundHit(out hit);
		if (groundedR) travelR = (-WheelR.transform.InverseTransformPoint(hit.point).y - WheelR.radius) / WheelR.suspensionDistance;

		float antiRollForce = (travelL - travelR) * intensity;

		if (groundedL) carStateMachine.rigidbody.AddForceAtPosition(WheelL.transform.up * -antiRollForce, WheelL.transform.position);
		if (groundedR) carStateMachine.rigidbody.AddForceAtPosition(WheelR.transform.up * antiRollForce, WheelR.transform.position);
	}
}
