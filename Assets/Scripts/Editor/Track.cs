using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class ChildLooperWindow : EditorWindow {
    private GameObject targetObject;
    private Vector2 scrollPos;
    private bool includeInactive = true;
    private bool showDepth = true;

    [MenuItem("tools/Track")]

    public static void ShowWindow() {
        GetWindow<ChildLooperWindow>("Child Looper");
    }

    private void OnGUI() {
        GUILayout.Label("Child Looper Tool", EditorStyles.boldLabel);

        // Drag & Drop field for GameObject
        EditorGUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("Target GameObject:", EditorStyles.boldLabel);

        targetObject = EditorGUILayout.ObjectField("Drag GameObject Here", targetObject, typeof(GameObject), true) as GameObject;

        EditorGUILayout.EndVertical();

        // Options
        includeInactive = EditorGUILayout.Toggle("Include Inactive", includeInactive);
        showDepth = EditorGUILayout.Toggle("Show Hierarchy Depth", showDepth);

        EditorGUILayout.Space();

        if (targetObject == null) {
            EditorGUILayout.HelpBox("Please assign a GameObject to see its children.", MessageType.Info);
            return;
        }

        if (targetObject.scene.IsValid() == false) {
            EditorGUILayout.HelpBox("This GameObject is a prefab asset. Only scene objects are supported.", MessageType.Warning);
            return;
        }

        // Button to refresh (optional)
        if (GUILayout.Button("Refresh Children List")) {
            // Force repaint
            Repaint();
        }

        // button to rename all children
        if (GUILayout.Button("Rename All Children")) {
            if (targetObject.transform.childCount > 0) {
                foreach (Transform item in targetObject.transform) {
                    item.name = "node";
                }
            }
        }

        EditorGUILayout.Space();

        // Scrollable list of children
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        EditorGUILayout.BeginVertical(GUI.skin.box);

        int childIndex = 0;
        LoopThroughChildren(targetObject.transform, 0, ref childIndex);

        EditorGUILayout.EndVertical();
        EditorGUILayout.EndScrollView();

        // Bottom info
        EditorGUILayout.LabelField($"Total children found: {childIndex}", EditorStyles.miniLabel);
    }

    private void LoopThroughChildren(Transform parent, int depth, ref int index) {
        if (parent == null) return;

        foreach (Transform child in parent) {
            if (child == parent) continue; // safety

            bool isActive = child.gameObject.activeInHierarchy || includeInactive;

            if (!includeInactive && !child.gameObject.activeInHierarchy)
                continue;

            EditorGUILayout.BeginHorizontal();

            // Optional depth spacing
            if (showDepth) {
                GUILayout.Space(depth * 15);
            }

            // Child info and buttons
            GUI.backgroundColor = isActive ? Color.white : Color.gray * 0.8f;

            // child components as string
            int childComponents = 0;
            foreach (Component item in child.GetComponents<Component>()) {
                //childComponents += item.GetType().Name + ", ";
                childComponents += 1;
            }


            if (GUILayout.Button($"{child.name} : {childComponents} ", GUILayout.Height(20))) {
                Selection.activeGameObject = child.gameObject;
                EditorGUIUtility.PingObject(child.gameObject);
            }

            // Select button
            if (GUILayout.Button("Select", GUILayout.Width(60))) {
                Selection.activeGameObject = child.gameObject;
                SceneView.FrameLastActiveSceneView();
            }

            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            index++;

            // Recursively go deeper
            if (child.childCount > 0) {
                LoopThroughChildren(child, depth + 1, ref index);
            }
        }
    }
}