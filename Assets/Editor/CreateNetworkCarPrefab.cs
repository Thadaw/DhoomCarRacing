using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using Photon.Pun;

// Editor utility: converts existing car prefabs into network-ready prefabs
// Run from Unity: Tools -> Setup Network Car Prefabs
public static class CreateNetworkCarPrefab
{
    private const string CAR_PREFAB_FOLDER = "Assets/Prefab";
    private const string OUTPUT_FOLDER = "Assets/Resources/Cars";

    [MenuItem("Tools/Setup Network Car Prefabs")]
    public static void SetupAllCars()
    {
        // Ensure output folder exists
        if (!Directory.Exists(OUTPUT_FOLDER))
        {
            Directory.CreateDirectory(OUTPUT_FOLDER);
            AssetDatabase.Refresh();
        }

        // Find all car prefabs
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { CAR_PREFAB_FOLDER });
        int processed = 0;

        foreach (string guid in prefabGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

            if (prefab == null) continue;

            // Check if this looks like a car prefab (has wheel children)
            Transform root = prefab.transform;
            List<Transform> wheels = FindWheelTransforms(root);

            if (wheels.Count < 4)
            {
                Debug.LogWarning($"Skipping {prefab.name}: found {wheels.Count} wheels, need 4");
                continue;
            }

            // Create a working copy
            GameObject workingCopy = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

            // Setup the prefab
            SetupCarPrefab(workingCopy, wheels);

            // Save to Resources/Cars
            string outputPath = Path.Combine(OUTPUT_FOLDER, prefab.name + "_Network.prefab");
            outputPath = outputPath.Replace("\\", "/");

            PrefabUtility.SaveAsPrefabAsset(workingCopy, outputPath);
            Object.DestroyImmediate(workingCopy);

            processed++;
            Debug.Log($"Setup network prefab: {outputPath}");
        }

        AssetDatabase.Refresh();
        Debug.Log($"Processed {processed} car prefabs into {OUTPUT_FOLDER}");
    }

    private static void SetupCarPrefab(GameObject car, List<Transform> wheels)
    {
        // Ensure root has Rigidbody
        Rigidbody rb = car.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = car.AddComponent<Rigidbody>();
        }
        rb.mass = 1200f;
        rb.linearDamping = 0.5f;
        rb.angularDamping = 3f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // Note: NetworkCar and PhotonView are on the network root, not the car model.
        // The car model only gets PhotonCarController (wired by NetworkCar.Start at runtime).

        // Ensure PhotonCarController
        PhotonCarController pcc = car.GetComponent<PhotonCarController>();
        if (pcc == null)
        {
            pcc = car.AddComponent<PhotonCarController>();
        }
        pcc.carRb = rb;

        // Map wheel transforms: Wheel_L, Wheel_R, Wheel_Back_L, Wheel_Back_R
        Transform wheelFrontLeft = null;
        Transform wheelFrontRight = null;
        Transform wheelRearLeft = null;
        Transform wheelRearRight = null;

        foreach (Transform w in wheels)
        {
            string name = w.name.ToLower();
            if (name == "wheel_l") wheelFrontLeft = w;
            else if (name == "wheel_r") wheelFrontRight = w;
            else if (name == "wheel_back_l") wheelRearLeft = w;
            else if (name == "wheel_back_r") wheelRearRight = w;
        }

        // Create wheel colliders
        GameObject wcParent = new GameObject("WheelColliders");
        wcParent.transform.SetParent(car.transform, false);

        WheelCollider wcFL = CreateWheelCollider(wcParent.transform, "WC_FrontLeft", wheelFrontLeft);
        WheelCollider wcFR = CreateWheelCollider(wcParent.transform, "WC_FrontRight", wheelFrontRight);
        WheelCollider wcRL = CreateWheelCollider(wcParent.transform, "WC_RearLeft", wheelRearLeft);
        WheelCollider wcRR = CreateWheelCollider(wcParent.transform, "WC_RearRight", wheelRearRight);

        // Wire up controller references
        pcc.frontLeftWheel = wcFL;
        pcc.frontRightWheel = wcFR;
        pcc.rearLeftWheel = wcRL;
        pcc.rearRightWheel = wcRR;

        pcc.frontLeftTransform = wheelFrontLeft;
        pcc.frontRightTransform = wheelFrontRight;
        pcc.rearLeftTransform = wheelRearLeft;
        pcc.rearRightTransform = wheelRearRight;
    }

    private static WheelCollider CreateWheelCollider(Transform parent, string name, Transform wheelMesh)
    {
        GameObject wcObj = new GameObject(name);
        wcObj.transform.SetParent(parent, false);

        WheelCollider wc = wcObj.AddComponent<WheelCollider>();

        // Position wheel collider at the wheel mesh position
        if (wheelMesh != null)
        {
            wcObj.transform.localPosition = wheelMesh.localPosition;
        }

        // Configure wheel collider
        wc.radius = 0.35f;
        wc.suspensionDistance = 0.2f;
        wc.forceAppPointDistance = 0f;

        // Suspension
        JointSpring suspSpring = wc.suspensionSpring;
        suspSpring.spring = 25000f;
        suspSpring.damper = 2500f;
        suspSpring.targetPosition = 0.5f;
        wc.suspensionSpring = suspSpring;

        // Forward friction
        WheelFrictionCurve fwdFriction = wc.forwardFriction;
        fwdFriction.extremumSlip = 0.4f;
        fwdFriction.extremumValue = 1f;
        fwdFriction.asymptoteSlip = 0.8f;
        fwdFriction.asymptoteValue = 0.5f;
        fwdFriction.stiffness = 1.5f;
        wc.forwardFriction = fwdFriction;

        // Sideways friction
        WheelFrictionCurve sideFriction = wc.sidewaysFriction;
        sideFriction.extremumSlip = 0.25f;
        sideFriction.extremumValue = 1f;
        sideFriction.asymptoteSlip = 0.5f;
        sideFriction.asymptoteValue = 0.75f;
        sideFriction.stiffness = 2f;
        wc.sidewaysFriction = sideFriction;

        return wc;
    }

    private static List<Transform> FindWheelTransforms(Transform root)
    {
        List<Transform> wheels = new List<Transform>();
        FindWheelTransformsRecursive(root, wheels);
        return wheels;
    }

    private static void FindWheelTransformsRecursive(Transform current, List<Transform> wheels)
    {
        if (current.name.ToLower().Contains("wheel"))
        {
            wheels.Add(current);
        }

        foreach (Transform child in current)
        {
            FindWheelTransformsRecursive(child, wheels);
        }
    }
}
