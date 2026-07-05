using UnityEditor;
using UnityEngine;
using System.IO;
using Photon.Pun;

// Editor utility: creates a minimal NetworkCar prefab in Assets/Resources
// Run from Unity: Tools -> Create NetworkCar Prefab
public static class CreateNetworkCarPrefab
{
    [MenuItem("Tools/Create NetworkCar Prefab")]
    public static void CreatePrefab()
    {
        // Ensure Resources folder exists
        string resourcesPath = "Assets/Resources";
        if (!Directory.Exists(resourcesPath))
        {
            Directory.CreateDirectory(resourcesPath);
            AssetDatabase.Refresh();
        }

        // Create a root GameObject for the prefab
        GameObject root = new GameObject("NetworkCar_PrefabRoot");

        // Add PhotonView
        PhotonView pv = root.AddComponent<PhotonView>();

        // Add PhotonTransformView for transform synchronization (if available)
        PhotonTransformView ptv = null;
        try
        {
            ptv = root.AddComponent<PhotonTransformView>();
        }
        catch
        {
            // PhotonTransformView type may not be present depending on PUN package
            Debug.LogWarning("PhotonTransformView component not found in project; ensure PUN package is installed.");
        }

        // Add the NetworkCar helper script
        root.AddComponent<NetworkCar>();

        // Wire observed components on PhotonView
        if (pv != null && ptv != null)
        {
            pv.ObservedComponents = new System.Collections.Generic.List<Component> { ptv };
            // PhotonView.synchronization isn't available in all PUN versions; leave default sync settings
        }

        string prefabPath = Path.Combine(resourcesPath, "NetworkCar.prefab");
        prefabPath = prefabPath.Replace("\\", "/");

        // Save as prefab
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);

        // Cleanup
        Object.DestroyImmediate(root);

        AssetDatabase.Refresh();

        Debug.Log("Created placeholder NetworkCar prefab at: " + prefabPath + "\nOpen Unity to finalize settings (add PhotonTransformView/PhotonView settings and any child meshes)." );
    }
}
