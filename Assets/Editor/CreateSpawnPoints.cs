using UnityEditor;
using UnityEngine;

// Editor utility to create a parent GameObject named "SpawnPoints"
// with child empty GameObjects at specified spawn positions.
// Use: Tools -> Create Spawn Points
public static class CreateSpawnPoints
{
    [MenuItem("Tools/Create Spawn Points")] 
    public static void Create()
    {
        const int defaultCount = 4;
        int count = defaultCount;

        if (!EditorUtility.DisplayDialog("Create Spawn Points",
            "This will create (or overwrite) a GameObject named 'SpawnPoints' with child spawn points. Continue?",
            "Create", "Cancel"))
        {
            return;
        }

        GameObject existing = GameObject.Find("SpawnPoints");
        if (existing != null)
        {
            if (!EditorUtility.DisplayDialog("SpawnPoints exists",
                "A GameObject named 'SpawnPoints' already exists in the scene. Delete and recreate?",
                "Delete & Recreate", "Cancel"))
            {
                return;
            }
            Undo.DestroyObjectImmediate(existing);
        }

        GameObject parent = new GameObject("SpawnPoints");
        Undo.RegisterCreatedObjectUndo(parent, "Create SpawnPoints");

        float radius = 6f;
        for (int i = 0; i < count; i++)
        {
            float ang = (Mathf.PI * 2f) * i / count;
            Vector3 pos = new Vector3(Mathf.Cos(ang) * radius, 0f, Mathf.Sin(ang) * radius);

            GameObject child = new GameObject($"SpawnPoint_{i+1}");
            child.transform.SetParent(parent.transform);
            child.transform.localPosition = pos;
            Undo.RegisterCreatedObjectUndo(child, "Create SpawnPoint");
        }

        Selection.activeGameObject = parent;
        EditorUtility.DisplayDialog("SpawnPoints Created", "Created SpawnPoints with " + count + " child spawn points. Adjust positions as needed.", "OK");
    }
}
