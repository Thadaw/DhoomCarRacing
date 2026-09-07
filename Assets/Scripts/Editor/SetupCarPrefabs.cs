using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class SetupCarPrefabs : EditorWindow
{
    [MenuItem("DoCarRacing/Setup Incomplete Car Prefabs")]
    static void SetupAll()
    {
        string[] prefabNames = new string[]
        {
            "Car2", "Car4", "Car5", "Car6",
            "Car_20", "car77", "Car8", "Car9"
        };

        int count = 0;
        foreach (string name in prefabNames)
        {
            string path = "Assets/Prefab/" + name + ".prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning("Prefab not found: " + path);
                continue;
            }

            if (SetupPrefab(path))
                count++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Setup complete: " + count + " prefabs updated.");
    }

    static bool SetupPrefab(string path)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        if (root == null)
        {
            Debug.LogWarning("Could not load prefab: " + path);
            return false;
        }

        bool hasRb = root.GetComponent<Rigidbody>() != null;
        bool hasCC = root.GetComponent<PhotonCarController>() != null;
        bool hasCheckpoints = root.GetComponent<Checkpoints>() != null;
        bool hasLap = root.GetComponent<PlayerLapTracker>() != null;
        bool hasWheelColliders = root.GetComponentInChildren<WheelCollider>() != null;

        if (hasRb && hasCC && hasCheckpoints && hasLap && hasWheelColliders)
        {
            Debug.Log("Skipping " + root.name + " - already fully set up");
            PrefabUtility.UnloadPrefabContents(root);
            return false;
        }

        Debug.Log("Setting up: " + root.name);

        Rigidbody rb = root.GetComponent<Rigidbody>();
        if (rb == null)
            rb = root.AddComponent<Rigidbody>();
        rb.mass = 1000f;
        rb.angularDamping = 0.05f;
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (root.GetComponent<PhotonCarController>() == null)
            root.AddComponent<PhotonCarController>();

        if (root.GetComponent<Checkpoints>() == null)
            root.AddComponent<Checkpoints>();

        if (root.GetComponent<PlayerLapTracker>() == null)
            root.AddComponent<PlayerLapTracker>();

        EditorUtility.SetDirty(root);
        PrefabUtility.SaveAsPrefabAsset(root, path);
        AssetDatabase.SaveAssets();

        Debug.Log(root.name + ": Saved OK");
        PrefabUtility.UnloadPrefabContents(root);
        return true;
    }
}
