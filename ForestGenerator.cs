#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class ForestGenerator : MonoBehaviour
{
    public List<GameObject> treePrefabs;
    public float _AreaSize = 80f;
    public int _PolygonPointCount = 12; // Rastgele alan için nokta sayýsý
    public float _MaxDeviation = 50f; // Rastgeleliðin ne kadar olacaðý
    public float _SpawnRayHeight = 1500f;

    public LayerMask _TerrainLayer;
    public Material _TreeMaterial;

    private List<Vector2> _polygonPoints;

    [ContextMenu("Generate Forest")]
    public void GenerateForest()
    {
        ClearForest();

        if (treePrefabs == null || treePrefabs.Count == 0)
        {
            Debug.LogWarning("No tree prefabs assigned.");
            return;
        }

        GenerateRandomPolygon();
        CreatePolygonColliderMesh();

        int treeCount = Mathf.RoundToInt(_AreaSize * _AreaSize * 0.045f);
        for (int i = 0; i < treeCount; i++)
        {
            Vector2 randomPoint;
            int attempts = 0;
            do
            {
                randomPoint = new Vector2(
                    Random.Range(-_AreaSize, _AreaSize),
                    Random.Range(-_AreaSize, _AreaSize)
                );
                attempts++;
                if (attempts > 1000) break; // Sonsuz döngü korumasý
            } while (!IsPointInsidePolygon(randomPoint));

            Vector3 worldPoint = transform.position + new Vector3(randomPoint.x, _SpawnRayHeight, randomPoint.y);

            if (Physics.Raycast(worldPoint, Vector3.down, out RaycastHit hit, _SpawnRayHeight * 2, _TerrainLayer))
            {
                GameObject treePrefab = treePrefabs[Random.Range(0, treePrefabs.Count)];
                GameObject tree = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab, transform.Find("Spawned"));
                tree.transform.position = hit.point;
                tree.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            }
        }

        Dictionary<Material, List<CombineInstance>> matToCombineList = new Dictionary<Material, List<CombineInstance>>();
        Transform spawnedRoot = transform.Find("Spawned");

        for (int i = 0; i < spawnedRoot.childCount; i++)
        {
            var child = spawnedRoot.GetChild(i);
            var mf = child.GetComponentInChildren<MeshFilter>();
            var mr = child.GetComponentInChildren<MeshRenderer>();

            if (mf == null || mr == null) continue;

            Material mat = mr.sharedMaterial;

            if (!matToCombineList.ContainsKey(mat))
                matToCombineList[mat] = new List<CombineInstance>();

            CombineInstance ci = new CombineInstance();
            ci.mesh = mf.sharedMesh;
            ci.transform = transform.worldToLocalMatrix * mf.transform.localToWorldMatrix;
            matToCombineList[mat].Add(ci);
        }

        foreach (var kvp in matToCombineList)
        {
            Mesh combined = new Mesh();
            combined.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            combined.CombineMeshes(kvp.Value.ToArray(), true, true);

            GameObject combinedObj = new GameObject("Combined_" + kvp.Key.name);
            combinedObj.transform.SetParent(spawnedRoot);
            combinedObj.transform.position = transform.position;
            combinedObj.transform.localRotation = Quaternion.identity;
            combinedObj.transform.localScale = Vector3.one;

            var mf = combinedObj.AddComponent<MeshFilter>();
            mf.sharedMesh = combined;

            var mr = combinedObj.AddComponent<MeshRenderer>();
            mr.sharedMaterial = kvp.Key;

            combinedObj.isStatic = true;
        }
        
        ClearForest(true);
    }

    private void GenerateRandomPolygon()
    {
        _polygonPoints = new List<Vector2>();
        for (int i = 0; i < _PolygonPointCount; i++)
        {
            float angle = i * Mathf.PI * 2f / _PolygonPointCount;
            float radius = _AreaSize + Random.Range(-_MaxDeviation, _MaxDeviation);
            Vector2 point = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            _polygonPoints.Add(point);
        }
    }

    private bool IsPointInsidePolygon(Vector2 point)
    {
        int crossings = 0;
        for (int i = 0; i < _polygonPoints.Count; i++)
        {
            Vector2 a = _polygonPoints[i];
            Vector2 b = _polygonPoints[(i + 1) % _polygonPoints.Count];

            if (((a.y > point.y) != (b.y > point.y)) &&
                (point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y + 0.0001f) + a.x))
            {
                crossings++;
            }
        }
        return (crossings % 2 == 1);
    }

    public void CreatePolygonColliderMesh()
    {
        if (_polygonPoints == null || _polygonPoints.Count < 3)
        {
            Debug.LogWarning("Polygon not generated yet or has less than 3 points.");
            return;
        }

        Vector3 worldPoint = transform.position + new Vector3(0f, _SpawnRayHeight, 0f);

        Vector3[] vertices = new Vector3[_polygonPoints.Count];
        for (int i = 0; i < _polygonPoints.Count; i++)
        {
            Vector2 p = _polygonPoints[i];

            worldPoint = transform.TransformPoint(new Vector3(p.x, 0f, p.y)) + new Vector3(0f, _SpawnRayHeight, 0f);
            float height = 0f;

            if (Physics.Raycast(worldPoint, Vector3.down, out RaycastHit hit, _SpawnRayHeight * 2, _TerrainLayer))
            {
                height = hit.point.y - transform.position.y;
            }

            vertices[i] = new Vector3(p.x, height, p.y);
        }


        Triangulator triangulator = new Triangulator(_polygonPoints.ToArray());
        int[] triangles = triangulator.Triangulate();
        triangles = Triangulator.ReverseNormals(triangles);

        Mesh mesh = new Mesh
        {
            vertices = vertices,
            triangles = triangles
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        MeshFilter mf = gameObject.GetComponent<MeshFilter>();
        mf.mesh = mesh;

        MeshRenderer mr = gameObject.GetComponent<MeshRenderer>();
        mr.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.25f, 0.25f, 0.25f, 0.25f) };

        MeshCollider mc = gameObject.GetComponent<MeshCollider>();
        mc.sharedMesh = mesh;
        mc.convex = true;
        mc.isTrigger = true;
    }

    [ContextMenu("Clear Forest")]
    public void ClearForest(bool isSavingCombinedMesh = false)
    {
        if (transform.Find("Spawned").childCount != 0)
        {
            GameObject[] arrayForRemove = new GameObject[transform.Find("Spawned").childCount];
            for (int i = 0; i < transform.Find("Spawned").childCount; i++)
            {
                if (!isSavingCombinedMesh || !transform.Find("Spawned").GetChild(i).name.StartsWith("Combined"))
                    arrayForRemove[i] = transform.Find("Spawned").GetChild(i).gameObject;
            }
            for (int i = 0; i < arrayForRemove.Length; i++)
            {
                DestroyImmediate(arrayForRemove[i]);
            }
        }
    }

}


public class Triangulator
{
    private List<Vector2> m_points;

    public Triangulator(Vector2[] points)
    {
        m_points = new List<Vector2>(points);
    }
    public static int[] ReverseNormals(int[] triangles)
    {
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int temp = triangles[i + 1];
            triangles[i + 1] = triangles[i + 2];
            triangles[i + 2] = temp;
        }
        return triangles;
    }
    public int[] Triangulate()
    {
        List<int> indices = new List<int>();

        int n = m_points.Count;
        if (n < 3)
            return indices.ToArray();

        int[] V = new int[n];
        if (Area() > 0)
        {
            for (int v = 0; v < n; v++) V[v] = v;
        }
        else
        {
            for (int v = 0; v < n; v++) V[v] = (n - 1) - v;
        }

        int nv = n;
        int count = 2 * nv;
        for (int m = 0, v = nv - 1; nv > 2;)
        {
            if ((count--) <= 0) return indices.ToArray();

            int u = v; if (nv <= u) u = 0;
            v = u + 1; if (nv <= v) v = 0;
            int w = v + 1; if (nv <= w) w = 0;

            if (Snip(u, v, w, nv, V))
            {
                int a = V[u], b = V[v], c = V[w];
                indices.Add(a);
                indices.Add(b);
                indices.Add(c);

                for (int s = v, t = v + 1; t < nv; s++, t++) V[s] = V[t];
                nv--;
                count = 2 * nv;
            }
        }

        return indices.ToArray();
    }

    private float Area()
    {
        int n = m_points.Count;
        float A = 0f;
        for (int p = n - 1, q = 0; q < n; p = q++)
        {
            Vector2 pval = m_points[p];
            Vector2 qval = m_points[q];
            A += pval.x * qval.y - qval.x * pval.y;
        }
        return A * 0.5f;
    }

    private bool Snip(int u, int v, int w, int n, int[] V)
    {
        Vector2 A = m_points[V[u]];
        Vector2 B = m_points[V[v]];
        Vector2 C = m_points[V[w]];

        if (Mathf.Epsilon > (((B.x - A.x) * (C.y - A.y)) - ((B.y - A.y) * (C.x - A.x))))
            return false;

        for (int p = 0; p < n; p++)
        {
            if ((p == u) || (p == v) || (p == w)) continue;
            Vector2 P = m_points[V[p]];
            if (InsideTriangle(A, B, C, P)) return false;
        }
        return true;
    }

    private bool InsideTriangle(Vector2 A, Vector2 B, Vector2 C, Vector2 P)
    {
        float ax = C.x - B.x, ay = C.y - B.y;
        float bx = A.x - C.x, by = A.y - C.y;
        float cx = B.x - A.x, cy = B.y - A.y;

        float apx = P.x - A.x, apy = P.y - A.y;
        float bpx = P.x - B.x, bpy = P.y - B.y;
        float cpx = P.x - C.x, cpy = P.y - C.y;

        float aCrossBP = ax * bpy - ay * bpx;
        float cCrossAP = cx * apy - cy * apx;
        float bCrossCP = bx * cpy - by * cpx;

        return (aCrossBP >= 0f) && (bCrossCP >= 0f) && (cCrossAP >= 0f);
    }
}
#endif