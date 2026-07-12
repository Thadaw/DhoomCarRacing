using System.Collections.Generic;
using UnityEngine;

public class trackWaypoints : MonoBehaviour {

    [Header("this will only show the points visually , nothing else")]

    public bool drawOnlyOnSelected = false;
    public bool closeLoop = false;
    public bool showArrowHeads = true;
    public bool reversepath = false;

    public Color linecolor;
    [Range(0, 1)] public float SphereRadius;
    public List<Transform> nodes = new List<Transform>();
    [Range(0, 10)] public float arrowHeadLength = 0.25f; // Length of the arrow head lines
    [Range(10, 100)] public float arrowHeadAngle = 20.0f; // Angle of the arrow head lines=

    private void OnDrawGizmosSelected() {
        if (drawOnlyOnSelected) gizmosLogic();
    }

    private void OnDrawGizmos() {
        if (!drawOnlyOnSelected) gizmosLogic();

    }

    /// <summary>
    /// Calculates a point on a Catmull-Rom spline.
    /// t is the time value, where 0 <= t <= 1.
    /// </summary>
    public static Vector3 GetCatmullRomPosition(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3) {
        // T^2
        float t2 = t * t;
        // T^3
        float t3 = t2 * t;

        // Blending functions for Catmull-Rom:
        // (0.5 * (-t3 + 2t2 - t)) * p0
        Vector3 a = 0.5f * (2f * p1);
        Vector3 b = 0.5f * (-p0 + p2) * t;
        Vector3 c = 0.5f * (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2;
        Vector3 d = 0.5f * (-p0 + 3f * p1 - 3f * p2 + p3) * t3;

        return a + b + c + d;
    }


    // Add this variable to your class fields
    [Range(10, 200)] public int splineSegmentsPerNode = 50;

    // ... inside gizmosLogic()
    void gizmosLogic() {
    Gizmos.color = linecolor;
    
    // Populate 'nodes' list
    Transform[] path = GetComponentsInChildren<Transform>();
    nodes = new List<Transform>();
    for (int i = 1; i < path.Length; i++) {
        nodes.Add(path[i]);
    }
    
    if (reversepath) {
        nodes.Reverse();
    }
    
    // Draw spheres and lines between nodes
    for (int i = 0; i < nodes.Count; i++) {
        // Draw sphere at current node
        Gizmos.DrawWireSphere(nodes[i].position, SphereRadius);
        
        // Draw line to next node
        int nextIndex = (i + 1) % nodes.Count;
        
        // Only draw line if not at last node (unless closeLoop is true)
        if (closeLoop || i < nodes.Count - 1) {
            Gizmos.DrawLine(nodes[i].position, nodes[nextIndex].position);
        }
    }
}
}