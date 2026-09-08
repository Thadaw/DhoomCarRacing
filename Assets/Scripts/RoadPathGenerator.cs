using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class RoadPathGenerator : MonoBehaviour
{
    [Header("Road Detection")]
    public string roadPartPrefix = "Road_Part";
    public bool generateOnStart = true;
    public bool closeLoop = true;

    [Header("Waypoint Settings")]
    public float waypointHeight = 1f;
    public bool drawGizmos = true;
    public Color gizmoColor = Color.green;

    [HideInInspector]
    public List<Vector3> roadWaypoints = new List<Vector3>();

    private void Start()
    {
        if (generateOnStart)
            GeneratePath();
    }

    [ContextMenu("Generate Path")]
    public void GeneratePath()
    {
        roadWaypoints.Clear();

        GameObject[] roadParts = FindRoadParts();

        if (roadParts.Length == 0)
        {
            Debug.LogWarning("RoadPathGenerator: No road parts found with prefix '" + roadPartPrefix + "'");
            return;
        }

        Debug.Log("RoadPathGenerator: Found " + roadParts.Length + " road parts");

        List<GameObject> sorted = SortRoadParts(roadParts);

        foreach (GameObject rp in sorted)
        {
            Vector3 center = GetRoadPartCenter(rp);
            roadWaypoints.Add(center);
        }

        Debug.Log("RoadPathGenerator: Generated " + roadWaypoints.Count + " waypoints");
    }

    private GameObject[] FindRoadParts()
    {
        List<GameObject> found = new List<GameObject>();
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);

        foreach (GameObject obj in allObjects)
        {
            if (obj.name.StartsWith(roadPartPrefix))
                found.Add(obj);
        }

        return found.ToArray();
    }

    private List<GameObject> SortRoadParts(GameObject[] parts)
    {
        List<KeyValuePair<int, GameObject>> indexed = new List<KeyValuePair<int, GameObject>>();

        foreach (GameObject part in parts)
        {
            int index = ExtractIndex(part.name);
            indexed.Add(new KeyValuePair<int, GameObject>(index, part));
        }

        indexed.Sort((a, b) => a.Key.CompareTo(b.Key));

        List<GameObject> sorted = new List<GameObject>();
        foreach (var pair in indexed)
            sorted.Add(pair.Value);

        return sorted;
    }

    private int ExtractIndex(string name)
    {
        Match match = Regex.Match(name, @"\d+");
        if (match.Success)
            return int.Parse(match.Value);
        return 0;
    }

    private Vector3 GetRoadPartCenter(GameObject roadPart)
    {
        Renderer renderer = roadPart.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            Bounds bounds = renderer.bounds;
            return new Vector3(bounds.center.x, bounds.center.y + waypointHeight, bounds.center.z);
        }

        Collider collider = roadPart.GetComponentInChildren<Collider>();
        if (collider != null)
        {
            Bounds bounds = collider.bounds;
            return new Vector3(bounds.center.x, bounds.center.y + waypointHeight, bounds.center.z);
        }

        return roadPart.transform.position + Vector3.up * waypointHeight;
    }

    public Vector3 GetWaypoint(int index)
    {
        if (roadWaypoints.Count == 0) return transform.position;
        return roadWaypoints[index % roadWaypoints.Count];
    }

    public int GetWaypointCount()
    {
        return roadWaypoints.Count;
    }

    public Vector3 GetNextWaypoint(Vector3 currentPos, int currentIndex, float reachDist = 15f)
    {
        if (roadWaypoints.Count == 0) return currentPos + transform.forward * 50f;

        Vector3 target = roadWaypoints[currentIndex % roadWaypoints.Count];

        if (Vector3.Distance(currentPos, target) < reachDist)
        {
            int nextIndex = (currentIndex + 1) % roadWaypoints.Count;
            return roadWaypoints[nextIndex];
        }

        return target;
    }

    public int AdvanceWaypoint(Vector3 currentPos, int currentIndex, float reachDist = 15f)
    {
        if (roadWaypoints.Count == 0) return currentIndex;

        Vector3 target = roadWaypoints[currentIndex % roadWaypoints.Count];

        if (Vector3.Distance(currentPos, target) < reachDist)
        {
            return (currentIndex + 1) % roadWaypoints.Count;
        }

        return currentIndex;
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos || roadWaypoints.Count == 0) return;

        Gizmos.color = gizmoColor;

        for (int i = 0; i < roadWaypoints.Count; i++)
        {
            Gizmos.DrawWireSphere(roadWaypoints[i], 1.5f);

            int nextIndex = (i + 1) % roadWaypoints.Count;

            if (closeLoop || i < roadWaypoints.Count - 1)
                Gizmos.DrawLine(roadWaypoints[i], roadWaypoints[nextIndex]);
        }
    }
}
