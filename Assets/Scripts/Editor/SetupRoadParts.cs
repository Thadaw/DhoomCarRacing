#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class SetupRoadParts : EditorWindow
{
    private string prefix = "Road_Part";
    private Transform trackRoot;

    [MenuItem("Tools/Setup Road Parts")]
    public static void ShowWindow()
    {
        GetWindow<SetupRoadParts>("Setup Road Parts");
    }

    private void OnGUI()
    {
        GUILayout.Label("Auto-Name Road Part Objects", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        trackRoot = (Transform)EditorGUILayout.ObjectField("Track Root", trackRoot, typeof(Transform), true);
        prefix = EditorGUILayout.TextField("Name Prefix", prefix);

        EditorGUILayout.Space();

        if (GUILayout.Button("Find Children & Rename as Road Parts", GUILayout.Height(35)))
        {
            RenameChildren();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Create Waypoints at Child Centers", GUILayout.Height(35)))
        {
            CreateWaypoints();
        }
    }

    private void RenameChildren()
    {
        if (trackRoot == null)
        {
            EditorUtility.DisplayDialog("Error", "Select a Track Root object first.", "OK");
            return;
        }

        Undo.RecordObject(trackRoot.gameObject, "Rename Road Parts");

        int index = 0;
        foreach (Transform child in trackRoot)
        {
            string newName = prefix + "_" + index.ToString("D2");
            Undo.RecordObject(child.gameObject, "Rename to " + newName);
            child.gameObject.name = newName;
            Debug.Log("Renamed: " + child.name + " -> " + newName);
            index++;
        }

        EditorUtility.SetDirty(trackRoot.gameObject);
        Debug.Log("Renamed " + index + " objects as " + prefix + "_XX");
    }

    private void CreateWaypoints()
    {
        if (trackRoot == null)
        {
            EditorUtility.DisplayDialog("Error", "Select a Track Root object first.", "OK");
            return;
        }

        GameObject waypointsParent = new GameObject("RoadWaypoints");
        Undo.RegisterCreatedObjectUndo(waypointsParent, "Create RoadWaypoints");

        int index = 0;
        foreach (Transform child in trackRoot)
        {
            Vector3 center = Vector3.zero;

            Renderer renderer = child.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                center = renderer.bounds.center;
                center.y += 1f;
            }
            else
            {
                center = child.position + Vector3.up * 1f;
            }

            GameObject wp = new GameObject("WP_" + index.ToString("D2"));
            wp.transform.position = center;
            wp.transform.SetParent(waypointsParent.transform);
            Undo.RegisterCreatedObjectUndo(wp, "Create Waypoint");
            index++;
        }

        Debug.Log("Created " + index + " waypoints from track children");
    }
}
#endif
