using System.Collections.Generic;
using UnityEngine;

public class RiverController : MonoBehaviour
{
    public List<Transform> _Bridges { get; private set; }
    private Transform _upSide;

    private Mesh _meshInstance;
    private MeshCollider _meshCollider;

    private void Awake()
    {
        _Bridges = new List<Transform>();
        _upSide = transform.Find("UpSide");
        _meshInstance = GetComponent<MeshFilter>().mesh;
        _meshCollider = GetComponent<MeshCollider>();
    }

    public bool IsTwoPointsAreInTheSameSide(Vector3 firstPoint, Vector3 secondPoint)
    {
        //Debug.Log(IsPointOnTheRightSide(firstPoint) + " " + IsPointOnTheRightSide(secondPoint));
        return IsPointOnTheRightSide(firstPoint) == IsPointOnTheRightSide(secondPoint);
    }
    private bool IsPointOnTheRightSide(Vector3 point)
    {
        Vector3 closestPoint = GetClosestPointOnMesh(point);
        Vector3 rightDir = GetRightDirectionAtPoint(closestPoint).normalized;
        for (int i = 0; i < 12; i++)
        {
            if (i == 4) { rightDir += Quaternion.AngleAxis(90f, Vector3.up) * rightDir; rightDir.Normalize(); }
            else if (i == 8) { rightDir -= Quaternion.AngleAxis(90f, Vector3.up) * rightDir * 2f; rightDir.Normalize(); }
            Physics.Raycast(closestPoint + rightDir * i + Vector3.up * 100f, -Vector3.up, out RaycastHit hitRight, 200f, LayerMask.GetMask("Water"));
            Physics.Raycast(closestPoint - rightDir * i + Vector3.up * 100f, -Vector3.up, out RaycastHit hitLeft, 200f, LayerMask.GetMask("Water"));

            if (hitRight.collider != null && hitRight.collider.CompareTag("River") && !(hitLeft.collider != null && hitLeft.collider.CompareTag("River")))
            {
                return false;
            }
            else if (hitLeft.collider != null && hitLeft.collider.CompareTag("River") && !(hitRight.collider != null && hitRight.collider.CompareTag("River")))
            {
                return true;
            }
        }

        Debug.LogError("river right and left rays returned same result!");
        return false;
    }

    public Vector3[] GetClosestCrossingPoints(Vector3 startPos)
    {
        Vector3[] array = new Vector3[2];
        float minDistance = float.MaxValue;
        Transform closestBridge = null;
        foreach (Transform bridge in _Bridges)
        {
            if ((bridge.position - startPos).magnitude < minDistance)
            {
                minDistance = (bridge.position - startPos).magnitude;
                closestBridge = bridge;
            }
        }

        if (closestBridge == null || (_upSide.position - startPos).magnitude < minDistance)
        {
            Vector3 rightDir = GetRightDirectionAtPoint(GetClosestPointOnMesh(_upSide.position)).normalized;
            Vector3 closestDirection = IsPointOnTheRightSide(startPos) ? rightDir : -rightDir;
            array[0] = _upSide.position + closestDirection * 20f - GetFlowDirection(_upSide.position).normalized * 20f;
            array[1] = _upSide.position - closestDirection * 40f + GetFlowDirection(_upSide.position).normalized * 40f;
        }
        else
        {
            Vector3 rightDir = GetRightDirectionAtPoint(GetClosestPointOnMesh(closestBridge.position)).normalized;
            Vector3 closestDirection = IsPointOnTheRightSide(startPos) ? rightDir : -rightDir;
            array[0] = closestBridge.position + closestDirection * 35f;
            array[1] = closestBridge.position - closestDirection * 35f;
        }

        return array;
    }


    private Vector3 GetFlowDirection(Vector3 point)
    {
        Vector3 closestPoint = GetClosestPointOnMesh(point);
        Physics.Raycast(closestPoint + Vector3.up * 100f, -Vector3.up, out RaycastHit hit, 200f, LayerMask.GetMask("Water"));
        bool isHit = hit.collider != null && hit.collider.CompareTag("River");

        Vector3 offset, origin;
        if (!isHit)
        {
            int sampleCount = 8;
            float radius = 2f;
            for (int i = 0; i < sampleCount; i++)
            {
                float angle = i * Mathf.PI * 2f / sampleCount;
                offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
                origin = closestPoint + offset + Vector3.up * 100f;

                if (Physics.Raycast(origin, -Vector3.up, out hit, 200f, LayerMask.GetMask("Water")))
                {
                    if (hit.collider != null && hit.collider.CompareTag("River"))
                    {
                        isHit = true;
                        break;
                    }
                }
            }

        }

        if (isHit)
        {
            MeshCollider collider = hit.collider as MeshCollider;
            Mesh mesh = _meshInstance;

            int[] triangles = mesh.triangles;
            Vector3[] vertices = mesh.vertices;

            int triIndex = hit.triangleIndex * 3;

            Vector3 p0 = vertices[triangles[triIndex + 0]];
            Vector3 p1 = vertices[triangles[triIndex + 1]];

            Vector3 worldP0 = collider.transform.TransformPoint(p0);
            Vector3 worldP1 = collider.transform.TransformPoint(p1);

            Vector3 tangent = (worldP1 - worldP0).normalized;

            int baseIndex = triangles[triIndex];
            int forwardIndex = Mathf.Min(baseIndex + 15, vertices.Length - 1);
            Vector3 forwardPoint = collider.transform.TransformPoint(vertices[forwardIndex]);

            if (Vector3.Dot((forwardPoint - closestPoint).normalized, tangent) < 0f)
            {
                tangent = -tangent;
            }

            GameObject.Find("Dir").transform.position = closestPoint;
            GameObject.Find("Dir").transform.forward = tangent;

            return tangent;
        }

        Debug.LogError("ray did not hit river!");
        return Vector3.zero;
    }

    private Vector3 GetClosestPointOnMesh(Vector3 point)
    {
        if (_meshInstance == null) return point;

        Vector3[] vertices = _meshInstance.vertices;
        Vector3 closest = transform.TransformPoint(vertices[0]);
        float minDist = (point - closest).magnitude;

        Vector3 worldPos;
        float dist;
        for (int i = 1; i < vertices.Length; i++)
        {
            worldPos = transform.TransformPoint(vertices[i]);
            dist = (point - worldPos).magnitude;

            if (dist < minDist)
            {
                minDist = dist;
                closest = worldPos;
            }
        }

        return closest;
    }
    private Vector3 GetRightDirectionAtPoint(Vector3 riverPoint)
    {
        return Quaternion.AngleAxis(90f, Vector3.up) * GetFlowDirection(riverPoint);
    }
    public Vector3 GetMidPosInTheSameSide(Vector3 point, Vector3 targetDir)
    {
        Vector3 flowDir = GetFlowDirection(point).normalized;
        if (Vector3.Dot(flowDir, targetDir) < 0)
            flowDir = Quaternion.AngleAxis(180f, Vector3.up) * flowDir;
        Vector3 farDir = GetRightDirectionAtPoint(point).normalized;
        if (!IsPointOnTheRightSide(point))
            farDir = Quaternion.AngleAxis(180f, Vector3.up) * farDir;
        Vector3 newDir = (farDir * 0.1f + flowDir * 0.9f).normalized;
        Vector3 newPos = point + newDir * 30f;
        newPos.y = TerrainController._Instance.GetTerrainHeightAtPosition(newPos, 100f);
        return newPos;
    }
}
