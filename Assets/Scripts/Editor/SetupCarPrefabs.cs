using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class SetupCarPrefabs : EditorWindow
{
    static readonly string photonCarControllerGuid = "0b01b122973045c4484f2f2f272c5e55";
    static readonly string checkpointsGuid = "cb717013b15ddff448b68dff024af38a";
    static readonly string playerLapTrackerGuid = "1e9bfbf5757a24047bc624bbe461bf7b";

    [MenuItem("DoCarRacing/Setup Incomplete Car Prefabs")]
    static void SetupAll()
    {
        string[] prefabNames = new string[]
        {
            "Car2", "Car3", "Car4", "Car5", "Car6",
            "Car7", "Car9", "car77"
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

        // Fix CarSpawner arrays in all track scenes
        FixCarSpawnerArrays();

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

        Debug.Log("Setting up: " + root.name);
        bool changed = false;

        // 1. Fix Rigidbody to match Car1
        changed |= FixRigidbody(root);

        // 2. Ensure PhotonCarController exists
        changed |= EnsurePhotonCarController(root);

        // 3. Ensure Checkpoints exists
        changed |= EnsureCheckpoints(root);

        // 4. Ensure PlayerLapTracker exists
        changed |= EnsurePlayerLapTracker(root);

        // 5. Fix root BoxCollider - must be trigger for checkpoint detection
        changed |= FixRootBoxCollider(root);

        // 6. Remove incorrect root WheelCollider
        changed |= RemoveRootWheelColliders(root);

        // 7. Add body BoxCollider to Body mesh child (critical for preventing rolling)
        changed |= AddBodyCollider(root);

        // 8. Add CarCenterOfMass child if missing
        changed |= EnsureCarCenterOfMass(root);

        // 9. Add CameraPoint child if missing
        changed |= EnsureCameraPoint(root);

        // 10. Wire PhotonCarController references in the prefab itself
        changed |= WirePhotonReferences(root);

        if (changed)
        {
            EditorUtility.SetDirty(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Debug.Log(root.name + ": Saved OK");
        }
        else
        {
            Debug.Log("Skipping " + root.name + " - already fully set up");
        }

        PrefabUtility.UnloadPrefabContents(root);
        return changed;
    }

    static bool FixRigidbody(GameObject root)
    {
        Rigidbody rb = root.GetComponent<Rigidbody>();
        if (rb == null) return false;

        bool changed = false;

        if (!rb.useGravity)
        {
            rb.useGravity = true;
            changed = true;
        }

        if (rb.mass != 1000f)
        {
            rb.mass = 1000f;
            changed = true;
        }

        if (rb.angularDamping != 0.05f)
        {
            rb.angularDamping = 0.05f;
            changed = true;
        }

        if (rb.isKinematic)
        {
            rb.isKinematic = false;
            changed = true;
        }

        if (rb.interpolation != RigidbodyInterpolation.None)
        {
            rb.interpolation = RigidbodyInterpolation.None;
            changed = true;
        }

        if (changed) Debug.Log(root.name + ": Fixed Rigidbody settings");

        return changed;
    }

    static System.Type GetTypeByGuid(string guid)
    {
        string scriptPath = AssetDatabase.GUIDToAssetPath(guid);
        if (string.IsNullOrEmpty(scriptPath))
        {
            Debug.LogWarning("Could not find asset for GUID: " + guid);
            return null;
        }

        MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
        if (script == null)
        {
            Debug.LogWarning("Could not load MonoScript at: " + scriptPath);
            return null;
        }

        return script.GetClass();
    }

    static bool EnsurePhotonCarController(GameObject root)
    {
        MonoBehaviour existing = GetMonoBehaviourByGuid(root, photonCarControllerGuid);
        if (existing != null) return false;

        System.Type scriptType = GetTypeByGuid(photonCarControllerGuid);
        if (scriptType == null)
        {
            Debug.LogWarning(root.name + ": Could not resolve PhotonCarController script type");
            return false;
        }

        root.AddComponent(scriptType);
        Debug.Log(root.name + ": Added PhotonCarController");
        return true;
    }

    static bool EnsureCheckpoints(GameObject root)
    {
        if (root.GetComponent<Checkpoints>() != null) return false;

        System.Type scriptType = GetTypeByGuid(checkpointsGuid);
        if (scriptType == null)
        {
            Debug.LogWarning(root.name + ": Could not resolve Checkpoints script type");
            return false;
        }

        root.AddComponent(scriptType);
        Debug.Log(root.name + ": Added Checkpoints");
        return true;
    }

    static bool EnsurePlayerLapTracker(GameObject root)
    {
        if (root.GetComponent<PlayerLapTracker>() != null) return false;

        System.Type scriptType = GetTypeByGuid(playerLapTrackerGuid);
        if (scriptType == null)
        {
            Debug.LogWarning(root.name + ": Could not resolve PlayerLapTracker script type");
            return false;
        }

        root.AddComponent(scriptType);
        Debug.Log(root.name + ": Added PlayerLapTracker");
        return true;
    }

    static bool FixRootBoxCollider(GameObject root)
    {
        bool changed = false;

        // Remove all existing BoxColliders on root
        BoxCollider[] existingBC = root.GetComponents<BoxCollider>();
        foreach (var bc in existingBC)
        {
            Object.DestroyImmediate(bc, true);
            changed = true;
        }

        // Add the correct trigger BoxCollider (for checkpoint detection)
        BoxCollider triggerBC = root.AddComponent<BoxCollider>();
        triggerBC.isTrigger = true;
        triggerBC.size = Vector3.one;
        triggerBC.center = Vector3.zero;

        if (changed)
            Debug.Log(root.name + ": Fixed root BoxCollider (now trigger)");
        else
            Debug.Log(root.name + ": Added root trigger BoxCollider");

        return true;
    }

    static bool RemoveRootWheelColliders(GameObject root)
    {
        bool changed = false;

        WheelCollider[] wcs = root.GetComponents<WheelCollider>();
        foreach (var wc in wcs)
        {
            Object.DestroyImmediate(wc, true);
            changed = true;
        }

        if (changed) Debug.Log(root.name + ": Removed " + wcs.Length + " root WheelColliders");

        return changed;
    }

    static bool AddBodyCollider(GameObject root)
    {
        // Find the Body mesh child (Body_1, Body_5, etc.)
        Transform bodyChild = FindBodyChild(root);
        if (bodyChild == null)
        {
            Debug.LogWarning(root.name + ": No Body mesh child found (Body_*)");
            return false;
        }

        // Check if it already has a BoxCollider
        if (bodyChild.GetComponent<BoxCollider>() != null)
            return false;

        BoxCollider bc = bodyChild.gameObject.AddComponent<BoxCollider>();
        bc.isTrigger = false;
        bc.size = new Vector3(1.935035f, 1.247256f, 4.4121027f);
        bc.center = new Vector3(-0.00000453f, 0.6676689f, 0.04943037f);

        Debug.Log(root.name + ": Added body BoxCollider to " + bodyChild.name);
        return true;
    }

    static Transform FindBodyChild(GameObject root)
    {
        foreach (Transform child in root.transform)
        {
            if (child.name.StartsWith("Body"))
                return child;
        }
        return null;
    }

    static bool EnsureCarCenterOfMass(GameObject root)
    {
        Transform existing = root.transform.Find("CarCenterOfMass");
        if (existing != null) return false;

        GameObject com = new GameObject("CarCenterOfMass");
        com.transform.SetParent(root.transform, false);
        com.transform.localPosition = Vector3.zero;
        com.transform.localRotation = Quaternion.identity;
        com.transform.localScale = Vector3.one;

        Debug.Log(root.name + ": Added CarCenterOfMass child");
        return true;
    }

    static bool EnsureCameraPoint(GameObject root)
    {
        Transform existing = root.transform.Find("CameraPoint");
        if (existing != null) return false;

        GameObject cam = new GameObject("CameraPoint");
        cam.transform.SetParent(root.transform, false);
        cam.transform.localPosition = new Vector3(0f, 1.7f, -5f);
        cam.transform.localRotation = Quaternion.identity;
        cam.transform.localScale = Vector3.one;

        Debug.Log(root.name + ": Added CameraPoint child");
        return true;
    }

    static bool WirePhotonReferences(GameObject root)
    {
        MonoBehaviour pcc = GetMonoBehaviourByGuid(root, photonCarControllerGuid);
        if (pcc == null) return false;

        SerializedObject so = new SerializedObject(pcc);
        bool changed = false;

        // Wire carRb
        SerializedProperty carRbProp = so.FindProperty("carRb");
        if (carRbProp.objectReferenceValue == null)
        {
            carRbProp.objectReferenceValue = root.GetComponent<Rigidbody>();
            changed = true;
        }

        // Wire centerOfMass
        SerializedProperty comProp = so.FindProperty("centerOfMass");
        if (comProp.objectReferenceValue == null)
        {
            Transform com = root.transform.Find("CarCenterOfMass");
            if (com == null) com = root.transform.Find("CenterOfMass");
            comProp.objectReferenceValue = com;
            changed = true;
        }

        // Find all WheelColliders in children
        WheelCollider[] allWheels = root.GetComponentsInChildren<WheelCollider>();
        if (allWheels.Length >= 4)
        {
            // Sort: front (z>0) first, then rear; left (x<0) before right (x>0)
            System.Array.Sort(allWheels, (a, b) =>
            {
                bool aFront = a.transform.localPosition.z > 0;
                bool bFront = b.transform.localPosition.z > 0;
                if (aFront != bFront) return aFront ? -1 : 1;
                return a.transform.localPosition.x.CompareTo(b.transform.localPosition.x);
            });

            // Wire wheel colliders
            SerializedProperty flProp = so.FindProperty("frontLeftWheel");
            SerializedProperty frProp = so.FindProperty("frontRightWheel");
            SerializedProperty rlProp = so.FindProperty("rearLeftWheel");
            SerializedProperty rrProp = so.FindProperty("rearRightWheel");

            if (flProp.objectReferenceValue == null) { flProp.objectReferenceValue = allWheels[0]; changed = true; }
            if (frProp.objectReferenceValue == null) { frProp.objectReferenceValue = allWheels[1]; changed = true; }
            if (rlProp.objectReferenceValue == null) { rlProp.objectReferenceValue = allWheels[2]; changed = true; }
            if (rrProp.objectReferenceValue == null) { rrProp.objectReferenceValue = allWheels[3]; changed = true; }

            // Wire wheel mesh transforms
            SerializedProperty fltProp = so.FindProperty("frontLeftTransform");
            SerializedProperty frtProp = so.FindProperty("frontRightTransform");
            SerializedProperty rltProp = so.FindProperty("rearLeftTransform");
            SerializedProperty rrtProp = so.FindProperty("rearRightTransform");

            if (fltProp.objectReferenceValue == null) { fltProp.objectReferenceValue = FindChild(root.transform, "Wheel_L"); changed = true; }
            if (frtProp.objectReferenceValue == null) { frtProp.objectReferenceValue = FindChild(root.transform, "Wheel_R"); changed = true; }
            if (rltProp.objectReferenceValue == null) { rltProp.objectReferenceValue = FindChild(root.transform, "Wheel_Back_L"); changed = true; }
            if (rrtProp.objectReferenceValue == null) { rrtProp.objectReferenceValue = FindChild(root.transform, "Wheel_Back_R"); changed = true; }
        }
        else
        {
            Debug.LogWarning(root.name + ": Only " + allWheels.Length + " WheelColliders found, need 4");
        }

        if (changed)
        {
            so.ApplyModifiedProperties();
            Debug.Log(root.name + ": Wired PhotonCarController references");
        }

        return changed;
    }

    static Transform FindChild(Transform parent, string name)
    {
        Transform t = parent.Find(name);
        if (t != null) return t;
        foreach (Transform child in parent.GetComponentsInChildren<Transform>())
        {
            if (child.name == name) return child;
        }
        return null;
    }

    static void FixCarSpawnerArrays()
    {
        string[] correctPrefabPaths = new string[]
        {
            "Assets/Prefab/Car1 1.prefab",   // Index 0 = Car1
            "Assets/Prefab/Car2.prefab",      // Index 1 = Car2
            "Assets/Prefab/Car3.prefab",      // Index 2 = Car3
            "Assets/Prefab/Car4.prefab",      // Index 3 = Car4
            "Assets/Prefab/Car5.prefab",      // Index 4 = Car5
            "Assets/Prefab/Car6.prefab",      // Index 5 = Car6
            "Assets/Prefab/Car7.prefab",      // Index 6 = Car7
            null,                              // Index 7 = Car8 (missing)
            "Assets/Prefab/Car9.prefab"       // Index 8 = Car9
        };

        string[] scenePaths = new string[]
        {
            "Assets/Scenes/Track1.unity",
            "Assets/Scenes/Track2.unity",
            "Assets/Scenes/Track3.unity"
        };

        string carSpawnerScriptGuid = "af6d81b3095eef847961ddd49243493b";

        foreach (string scenePath in scenePaths)
        {
            if (!System.IO.File.Exists(scenePath)) continue;

            EditorSceneManager.OpenScene(scenePath);
            var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            GameObject[] rootObjects = scene.GetRootGameObjects();

            bool sceneChanged = false;

            foreach (GameObject rootObj in rootObjects)
            {
                Component[] spawners = rootObj.GetComponentsInChildren<Component>(true);
                foreach (Component comp in spawners)
                {
                    if (comp == null || comp is not MonoBehaviour mb) continue;
                    MonoScript ms = MonoScript.FromMonoBehaviour(mb);
                    if (ms == null) continue;
                    string scriptPath = AssetDatabase.GetAssetPath(ms);
                    string scriptGuid = AssetDatabase.AssetPathToGUID(scriptPath);
                    if (scriptGuid != carSpawnerScriptGuid) continue;

                    SerializedObject so = new SerializedObject(comp);
                    SerializedProperty prefabsProp = so.FindProperty("carsPrefabs");
                    if (prefabsProp == null) continue;

                    prefabsProp.arraySize = correctPrefabPaths.Length;
                    for (int i = 0; i < correctPrefabPaths.Length; i++)
                    {
                        SerializedProperty element = prefabsProp.GetArrayElementAtIndex(i);
                        if (correctPrefabPaths[i] != null)
                        {
                            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(correctPrefabPaths[i]);
                            element.objectReferenceValue = prefab;
                        }
                        else
                        {
                            element.objectReferenceValue = null;
                        }
                    }

                    so.ApplyModifiedProperties();
                    sceneChanged = true;
                    Debug.Log("Fixed CarSpawner in " + scenePath + " - " + comp.gameObject.name);
                }
            }

            if (sceneChanged)
            {
                EditorSceneManager.SaveScene(scene);
                Debug.Log("Saved " + scenePath);
            }
        }
    }

    static MonoBehaviour GetMonoBehaviourByGuid(GameObject root, string guid)
    {
        MonoBehaviour[] components = root.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour comp in components)
        {
            if (comp == null) continue;
            MonoScript script = MonoScript.FromMonoBehaviour(comp);
            if (script == null) continue;
            string scriptPath = AssetDatabase.GetAssetPath(script);
            string scriptGuid = AssetDatabase.AssetPathToGUID(scriptPath);
            if (scriptGuid == guid)
                return comp;
        }
        return null;
    }
}
