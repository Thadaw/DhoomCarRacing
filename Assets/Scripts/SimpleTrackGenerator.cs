using UnityEngine;
using System.Collections.Generic;

public class SimpleTrackGenerator : MonoBehaviour
{
    [Header("Track Shape")]
    public TrackShape trackShape = TrackShape.Oval;
    public float trackRadius = 80f;
    public float straightLength = 80f;
    public int resolution = 120;
    public float cornerRadius = 30f;

    [Header("Road")]
    public float roadWidth = 20f;
    public float roadThickness = 0.3f;
    public float borderWidth = 3f;
    public float borderHeight = 2.5f;
    public Material roadMaterial;
    public Material borderMaterial;
    public Material groundMaterial;

    [Header("Checkpoints")]
    public int checkpointCount = 10;

    [Header("Generation")]
    public bool generateOnStart = true;

    public enum TrackShape { Oval, RoundedRectangle, SimpleCircuit }

    private List<Vector3> pathPoints = new List<Vector3>();
    private GameObject roadParent;
    private GameObject checkpointParent;

    public static List<Vector3> LastGeneratedPath { get; private set; }

    private void Start()
    {
        if (generateOnStart)
            Generate();
    }

    [ContextMenu("Generate Track")]
    public void Generate()
    {
        Clear();

        roadParent = new GameObject("Road_Parts");
        checkpointParent = new GameObject("Checkpoints");

        GeneratePath();
        BuildGround();
        BuildRoadSurface();
        BuildLaneMarkings();
        BuildEdgeLines();
        BuildBorders();
        BuildRoadParts();
        BuildCheckpoints();

        LastGeneratedPath = new List<Vector3>(pathPoints);

        Debug.Log($"SimpleTrackGenerator: Generated {pathPoints.Count} path points, {checkpointCount} checkpoints, roadWidth={roadWidth}");
    }

    private void GeneratePath()
    {
        pathPoints.Clear();

        switch (trackShape)
        {
            case TrackShape.Oval:
                GenerateOval();
                break;
            case TrackShape.RoundedRectangle:
                GenerateRoundedRectangle();
                break;
            case TrackShape.SimpleCircuit:
                GenerateSimpleCircuit();
                break;
        }
    }

    private void GenerateOval()
    {
        for (int i = 0; i < resolution; i++)
        {
            float angle = (float)i / resolution * Mathf.PI * 2f;
            float x = Mathf.Cos(angle) * trackRadius;
            float z = Mathf.Sin(angle) * (trackRadius * 0.55f);
            pathPoints.Add(transform.position + new Vector3(x, 0f, z));
        }
    }

    private void GenerateRoundedRectangle()
    {
        float halfW = straightLength / 2f;
        float halfH = trackRadius;
        float r = cornerRadius;
        int cornerSegs = 16;
        int straightSegH = resolution / 8;
        int straightSegV = resolution / 8;

        for (int i = 0; i <= straightSegH; i++)
        {
            float t = (float)i / straightSegH;
            pathPoints.Add(transform.position + new Vector3(Mathf.Lerp(-halfW + r, halfW - r, t), 0f, halfH));
        }

        for (int i = 1; i <= cornerSegs; i++)
        {
            float a = (float)i / cornerSegs * Mathf.PI * 0.5f;
            pathPoints.Add(transform.position + new Vector3(halfW - r + Mathf.Sin(a) * r, 0f, halfH - r + Mathf.Cos(a) * r));
        }

        for (int i = 0; i <= straightSegV; i++)
        {
            float t = (float)i / straightSegV;
            pathPoints.Add(transform.position + new Vector3(halfW, 0f, Mathf.Lerp(halfH - r, -halfH + r, t)));
        }

        for (int i = 1; i <= cornerSegs; i++)
        {
            float a = (float)i / cornerSegs * Mathf.PI * 0.5f;
            pathPoints.Add(transform.position + new Vector3(halfW - r + Mathf.Cos(a) * r, 0f, -halfH + r - Mathf.Sin(a) * r));
        }

        for (int i = 0; i <= straightSegH; i++)
        {
            float t = (float)i / straightSegH;
            pathPoints.Add(transform.position + new Vector3(Mathf.Lerp(halfW - r, -halfW + r, t), 0f, -halfH));
        }

        for (int i = 1; i <= cornerSegs; i++)
        {
            float a = (float)i / cornerSegs * Mathf.PI * 0.5f;
            pathPoints.Add(transform.position + new Vector3(-halfW + r - Mathf.Sin(a) * r, 0f, -halfH + r - Mathf.Cos(a) * r));
        }

        for (int i = 0; i <= straightSegV; i++)
        {
            float t = (float)i / straightSegV;
            pathPoints.Add(transform.position + new Vector3(-halfW, 0f, Mathf.Lerp(-halfH + r, halfH - r, t)));
        }

        for (int i = 1; i < cornerSegs; i++)
        {
            float a = (float)i / cornerSegs * Mathf.PI * 0.5f;
            pathPoints.Add(transform.position + new Vector3(-halfW + r - Mathf.Cos(a) * r, 0f, halfH - r + Mathf.Sin(a) * r));
        }
    }

    private void GenerateSimpleCircuit()
    {
        float r = trackRadius * 0.4f;
        int segs = resolution / 5;

        for (int i = 0; i < segs; i++)
        {
            float a = (float)i / segs * Mathf.PI;
            pathPoints.Add(transform.position + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r));
        }

        for (int i = 0; i < segs; i++)
        {
            float t = (float)i / segs;
            pathPoints.Add(transform.position + new Vector3(-r + t * straightLength, 0f, 0f));
        }

        for (int i = 0; i < segs; i++)
        {
            float a = (float)i / segs * Mathf.PI;
            pathPoints.Add(transform.position + new Vector3(straightLength - r + Mathf.Cos(a + Mathf.PI) * r, 0f, Mathf.Sin(a + Mathf.PI) * r));
        }

        for (int i = 0; i < segs; i++)
        {
            float t = (float)i / segs;
            pathPoints.Add(transform.position + new Vector3(straightLength - r - t * straightLength, 0f, 0f));
        }
    }

    private void BuildGround()
    {
        GameObject ground = new GameObject("Ground");
        ground.transform.SetParent(transform);
        ground.transform.position = new Vector3(transform.position.x, -0.5f, transform.position.z);

        MeshFilter mf = ground.AddComponent<MeshFilter>();
        MeshRenderer mr = ground.AddComponent<MeshRenderer>();
        MeshCollider mc = ground.AddComponent<MeshCollider>();

        float size = Mathf.Max(straightLength, trackRadius) * 4f;
        Mesh groundMesh = CreateGroundMesh(size);
        mf.mesh = groundMesh;
        mc.sharedMesh = groundMesh;

        if (groundMaterial != null)
            mr.material = groundMaterial;
        else
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(0.22f, 0.32f, 0.12f);
            mr.material = mat;
        }
    }

    private Mesh CreateGroundMesh(float size)
    {
        Mesh mesh = new Mesh();
        mesh.name = "Ground";
        float hs = size / 2f;

        mesh.vertices = new Vector3[]
        {
            new Vector3(-hs, 0f, -hs),
            new Vector3(hs, 0f, -hs),
            new Vector3(hs, 0f, hs),
            new Vector3(-hs, 0f, hs)
        };

        mesh.triangles = new int[] { 0, 2, 1, 0, 3, 2 };

        float uvScale = size / 20f;
        mesh.uv = new Vector2[]
        {
            new Vector2(0, 0), new Vector2(uvScale, 0),
            new Vector2(uvScale, uvScale), new Vector2(0, uvScale)
        };

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private void BuildRoadSurface()
    {
        if (pathPoints.Count < 2) return;

        GameObject roadSurface = new GameObject("RoadSurface");
        roadSurface.transform.SetParent(transform);

        MeshFilter mf = roadSurface.AddComponent<MeshFilter>();
        MeshRenderer mr = roadSurface.AddComponent<MeshRenderer>();
        MeshCollider mc = roadSurface.AddComponent<MeshCollider>();

        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        float halfW = roadWidth / 2f;
        float accumU = 0f;

        for (int i = 0; i < pathPoints.Count; i++)
        {
            int next = (i + 1) % pathPoints.Count;
            Vector3 p = pathPoints[i];
            Vector3 pn = pathPoints[next];
            Vector3 dir = (pn - p).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;

            Vector3 leftPt = p - right * halfW;
            Vector3 rightPt = p + right * halfW;

            verts.Add(new Vector3(leftPt.x, 0.15f, leftPt.z));
            verts.Add(new Vector3(rightPt.x, 0.15f, rightPt.z));

            if (i > 0)
            {
                float segLen = Vector3.Distance(pathPoints[i], pathPoints[i - 1]);
                accumU += segLen / roadWidth;
            }

            uvs.Add(new Vector2(0, accumU));
            uvs.Add(new Vector2(1, accumU));

            if (i > 0)
            {
                int baseIdx = i * 2;
                tris.Add(baseIdx - 2);
                tris.Add(baseIdx);
                tris.Add(baseIdx - 1);

                tris.Add(baseIdx - 1);
                tris.Add(baseIdx);
                tris.Add(baseIdx + 1);
            }
        }

        int last = (pathPoints.Count - 1) * 2;
        tris.Add(last);
        tris.Add(0);
        tris.Add(last + 1);

        tris.Add(last + 1);
        tris.Add(0);
        tris.Add(1);

        Mesh roadMesh = new Mesh();
        roadMesh.name = "RoadSurface";
        roadMesh.SetVertices(verts);
        roadMesh.SetTriangles(tris.ToArray(), 0);
        roadMesh.SetUVs(0, uvs);
        roadMesh.RecalculateNormals();
        roadMesh.RecalculateBounds();

        mf.mesh = roadMesh;
        mc.sharedMesh = roadMesh;

        if (roadMaterial != null)
            mr.material = roadMaterial;
        else
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(0.25f, 0.25f, 0.28f);
            mat.SetFloat("_Glossiness", 0.4f);
            mr.material = mat;
        }
    }

    private void BuildLaneMarkings()
    {
        if (pathPoints.Count < 2) return;

        GameObject markingParent = new GameObject("LaneMarkings");
        markingParent.transform.SetParent(transform);

        Material centerMat = new Material(Shader.Find("Standard"));
        centerMat.color = new Color(1f, 1f, 0.2f, 0.9f);
        centerMat.SetFloat("_Glossiness", 0.8f);

        float markingWidth = 0.5f;
        float markingHeight = 0.1f;
        float accumU = 0f;
        float dashLength = 6f;
        float gapLength = 6f;

        for (int i = 0; i < pathPoints.Count; i++)
        {
            int next = (i + 1) % pathPoints.Count;
            Vector3 p = pathPoints[i];
            Vector3 pn = pathPoints[next];
            Vector3 dir = (pn - p).normalized;
            float segLen = Vector3.Distance(p, pn);

            accumU += segLen;
            float cyclePos = accumU % (dashLength + gapLength);

            if (cyclePos < dashLength)
            {
                Vector3 mid = (p + pn) * 0.5f;
                Vector3 pos = new Vector3(mid.x, markingHeight, mid.z);

                GameObject dash = new GameObject($"CenterDash_{i}");
                dash.transform.SetParent(markingParent.transform);
                dash.transform.position = pos;
                dash.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

                MeshFilter mf = dash.AddComponent<MeshFilter>();
                MeshRenderer mr = dash.AddComponent<MeshRenderer>();
                mf.mesh = CreateQuadMesh(markingWidth, segLen + 0.2f);
                mr.material = centerMat;
            }
        }
    }

    private void BuildEdgeLines()
    {
        if (pathPoints.Count < 2) return;

        GameObject edgeParent = new GameObject("EdgeLines");
        edgeParent.transform.SetParent(transform);

        Material edgeMat = new Material(Shader.Find("Standard"));
        edgeMat.color = new Color(1f, 1f, 1f, 0.9f);
        edgeMat.SetFloat("_Glossiness", 0.8f);

        float edgeWidth = 0.5f;
        float edgeHeight = 0.1f;
        float edgeOffset = roadWidth / 2f - 1f;

        CreateEdgeLine(edgeParent.transform, pathPoints, -edgeOffset, edgeMat, edgeWidth, edgeHeight, "LeftEdge");
        CreateEdgeLine(edgeParent.transform, pathPoints, edgeOffset, edgeMat, edgeWidth, edgeHeight, "RightEdge");
    }

    private void CreateEdgeLine(Transform parent, List<Vector3> pts, float xOffset, Material mat, float width, float height, string name)
    {
        GameObject edge = new GameObject(name);
        edge.transform.SetParent(parent);

        MeshFilter mf = edge.AddComponent<MeshFilter>();
        MeshRenderer mr = edge.AddComponent<MeshRenderer>();

        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();

        float hw = width / 2f;

        for (int i = 0; i < pts.Count; i++)
        {
            int next = (i + 1) % pts.Count;
            Vector3 p = pts[i];
            Vector3 pn = pts[next];
            Vector3 dir = (pn - p).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;

            Vector3 center = p + right * xOffset;
            Vector3 left = center - right * hw;
            Vector3 rightPt = center + right * hw;

            verts.Add(new Vector3(left.x, height, left.z));
            verts.Add(new Vector3(rightPt.x, height, rightPt.z));

            if (i > 0)
            {
                int baseIdx = i * 2;
                tris.Add(baseIdx - 2);
                tris.Add(baseIdx);
                tris.Add(baseIdx - 1);
                tris.Add(baseIdx - 1);
                tris.Add(baseIdx);
                tris.Add(baseIdx + 1);
            }
        }

        int last = (pts.Count - 1) * 2;
        tris.Add(last);
        tris.Add(0);
        tris.Add(last + 1);
        tris.Add(last + 1);
        tris.Add(0);
        tris.Add(1);

        Mesh mesh = new Mesh();
        mesh.name = name;
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris.ToArray(), 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        mf.mesh = mesh;
        mr.material = mat;
    }

    private void BuildBorders()
    {
        if (pathPoints.Count < 2) return;

        GameObject borderParent = new GameObject("Borders");
        borderParent.transform.SetParent(transform);

        Material leftMat, rightMat;
        if (borderMaterial != null)
        {
            leftMat = borderMaterial;
            rightMat = borderMaterial;
        }
        else
        {
            leftMat = new Material(Shader.Find("Standard"));
            leftMat.color = new Color(0.9f, 0.2f, 0.2f);
            rightMat = new Material(Shader.Find("Standard"));
            rightMat.color = new Color(0.9f, 0.2f, 0.2f);
        }

        CreateBorderStrip(borderParent.transform, pathPoints, -1f, leftMat, "LeftBorder");
        CreateBorderStrip(borderParent.transform, pathPoints, 1f, rightMat, "RightBorder");
    }

    private void CreateBorderStrip(Transform parent, List<Vector3> pts, float side, Material mat, string name)
    {
        GameObject border = new GameObject(name);
        border.transform.SetParent(parent);

        MeshFilter mf = border.AddComponent<MeshFilter>();
        MeshRenderer mr = border.AddComponent<MeshRenderer>();

        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        float halfW = roadWidth / 2f;
        float borderOffset = halfW + borderWidth * 0.5f;
        float accumU = 0f;

        for (int i = 0; i < pts.Count; i++)
        {
            int next = (i + 1) % pts.Count;
            Vector3 p = pts[i];
            Vector3 pn = pts[next];
            Vector3 dir = (pn - p).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;

            Vector3 innerPt = p + right * side * halfW;
            Vector3 outerPt = p + right * side * borderOffset;

            verts.Add(new Vector3(innerPt.x, -0.2f, innerPt.z));
            verts.Add(new Vector3(innerPt.x, borderHeight, innerPt.z));
            verts.Add(new Vector3(outerPt.x, borderHeight, outerPt.z));
            verts.Add(new Vector3(outerPt.x, -0.2f, outerPt.z));

            if (i > 0)
            {
                float segLen = Vector3.Distance(pts[i], pts[i - 1]);
                accumU += segLen / borderWidth;
            }

            float uvV0 = accumU;
            float uvV1 = accumU + 0.3f;
            uvs.Add(new Vector2(0, uvV0));
            uvs.Add(new Vector2(0, uvV1));
            uvs.Add(new Vector2(1, uvV1));
            uvs.Add(new Vector2(1, uvV0));

            if (i > 0)
            {
                int baseIdx = i * 4;
                int prevBase = (i - 1) * 4;

                tris.Add(prevBase + 0);
                tris.Add(baseIdx + 0);
                tris.Add(prevBase + 1);
                tris.Add(prevBase + 1);
                tris.Add(baseIdx + 0);
                tris.Add(baseIdx + 1);

                tris.Add(prevBase + 1);
                tris.Add(baseIdx + 1);
                tris.Add(prevBase + 2);
                tris.Add(prevBase + 2);
                tris.Add(baseIdx + 1);
                tris.Add(baseIdx + 2);

                tris.Add(prevBase + 2);
                tris.Add(baseIdx + 2);
                tris.Add(prevBase + 3);
                tris.Add(prevBase + 3);
                tris.Add(baseIdx + 2);
                tris.Add(baseIdx + 3);

                tris.Add(prevBase + 3);
                tris.Add(baseIdx + 3);
                tris.Add(prevBase + 0);
                tris.Add(prevBase + 0);
                tris.Add(baseIdx + 3);
                tris.Add(baseIdx + 0);
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = name;
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris.ToArray(), 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        mf.mesh = mesh;

        if (borderMaterial != null)
        {
            mr.material = borderMaterial;
        }
        else
        {
            mat.color = new Color(0.85f, 0.15f, 0.1f);
            mat.SetFloat("_Glossiness", 0.4f);
            mr.material = mat;
        }

        BoxCollider col = border.AddComponent<BoxCollider>();
        col.center = new Vector3(0, (borderHeight - 0.2f) * 0.5f, 0);
        col.size = new Vector3(borderWidth, borderHeight + 0.4f, 1f);
    }

    private void BuildRoadParts()
    {
        for (int i = 0; i < pathPoints.Count; i++)
        {
            int next = (i + 1) % pathPoints.Count;
            Vector3 start = pathPoints[i];
            Vector3 end = pathPoints[next];

            Vector3 midpoint = (start + end) * 0.5f;
            Vector3 dir = (end - start).normalized;
            float length = Vector3.Distance(start, end);

            GameObject roadPart = new GameObject($"Road_Part{i}");
            roadPart.transform.SetParent(roadParent.transform);
            roadPart.transform.position = new Vector3(midpoint.x, 0f, midpoint.z);
            roadPart.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

            MeshFilter mf = roadPart.AddComponent<MeshFilter>();
            MeshRenderer mr = roadPart.AddComponent<MeshRenderer>();
            mf.mesh = CreateQuadMesh(roadWidth, length + 0.5f);
            mr.enabled = false;
        }
    }

    private Mesh CreateQuadMesh(float width, float height)
    {
        Mesh mesh = new Mesh();
        float hw = width / 2f;
        float hh = height / 2f;

        mesh.vertices = new Vector3[]
        {
            new Vector3(-hw, -hh, 0),
            new Vector3(hw, -hh, 0),
            new Vector3(hw, hh, 0),
            new Vector3(-hw, hh, 0)
        };

        mesh.triangles = new int[] { 0, 2, 1, 0, 3, 2 };

        mesh.uv = new Vector2[]
        {
            new Vector2(0, 0), new Vector2(1, 0),
            new Vector2(1, 1), new Vector2(0, 1)
        };

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private void BuildCheckpoints()
    {
        // Evenly space checkpoints, skip index 0 (FinishLine position)
        float spacing = (float)pathPoints.Count / checkpointCount;

        for (int i = 0; i < checkpointCount; i++)
        {
            int idx = (1 + Mathf.RoundToInt(i * spacing)) % pathPoints.Count;
            int nextIdx = (idx + 1) % pathPoints.Count;

            Vector3 pos = pathPoints[idx];
            Vector3 dir = (pathPoints[nextIdx] - pathPoints[idx]).normalized;

            GameObject cp = new GameObject($"CP_{i}");
            cp.transform.SetParent(checkpointParent.transform);
            cp.transform.position = pos + Vector3.up * 2f;
            cp.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

            BoxCollider col = cp.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(roadWidth + 4f, 5f, 2f);

            RaceCheckpoint rcp = cp.AddComponent<RaceCheckpoint>();
            rcp.checkpointIndex = i;
            rcp.isFinishLine = false;
        }

        GameObject finish = new GameObject("FinishLine");
        finish.transform.SetParent(checkpointParent.transform);
        finish.transform.position = pathPoints[0] + Vector3.up * 2f;
        Vector3 finishDir = (pathPoints[1] - pathPoints[0]).normalized;
        finish.transform.rotation = Quaternion.LookRotation(finishDir, Vector3.up);

        BoxCollider finishCol = finish.AddComponent<BoxCollider>();
        finishCol.isTrigger = true;
        finishCol.size = new Vector3(roadWidth + 4f, 5f, 2f);

        RaceCheckpoint finishRC = finish.AddComponent<RaceCheckpoint>();
        finishRC.checkpointIndex = 0;
        finishRC.isFinishLine = true;
    }

    public void Clear()
    {
        if (roadParent != null) DestroyImmediate(roadParent);
        if (checkpointParent != null) DestroyImmediate(checkpointParent);
        pathPoints.Clear();
    }

    public List<Vector3> GetPathPoints() => new List<Vector3>(pathPoints);

    private void OnDrawGizmosSelected()
    {
        if (pathPoints.Count == 0) return;

        Gizmos.color = Color.green;
        for (int i = 0; i < pathPoints.Count; i++)
        {
            int next = (i + 1) % pathPoints.Count;
            Gizmos.DrawLine(pathPoints[i], pathPoints[next]);
        }
    }
}
