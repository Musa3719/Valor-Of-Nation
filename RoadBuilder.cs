using UnityEngine;
using UnityEngine.Splines;
using System.Collections.Generic;
using Unity.Mathematics;

public class RoadBuilder : MonoBehaviour
{
    public static RoadBuilder _Instance;
    public SplineContainer _ActiveSplineContainer;

    private Transform _dirtContainer;
    private Transform _asphaltContainer;
    private Transform _railContainer;

    public GameObject _RoadPointGhost;
    public GameObject _RoadGhost;

    public float _SnapDistance = 20f;
    private float _maxRoadMagnitude = 1000f;
    private float _heightOffset = 1f;

    private void Awake()
    {
        _Instance = this;

        _dirtContainer = GameObject.Find("RoadSystemDirt").transform;
        _asphaltContainer = GameObject.Find("RoadSystemAsphalt").transform;
        _railContainer = GameObject.Find("RoadSystemRail").transform;
        _RoadPointGhost = GameObject.Find("RoadPointGhost");
        _RoadGhost = GameObject.Find("RoadGhost");
    }
    private void Start()
    {
        ArrangePool(_dirtContainer);
        ArrangePool(_asphaltContainer);
        ArrangePool(_railContainer);
    }

    public void UpdateRoadGhost(Vector3 firstPos, Vector3 secondPos, TerrainPoint point)
    {
        firstPos += Vector3.up * 12f;
        secondPos += Vector3.up * 12f;
        _RoadGhost.SetActive(true);
        _RoadGhost.GetComponent<LineRenderer>().SetPosition(0, firstPos);
        _RoadGhost.GetComponent<LineRenderer>().SetPosition(1, secondPos);

        if (TerrainController._Instance.CanPlaceConstruction(point))
        {
            GameManager._Instance._RoadCannotPlaceText.SetActive(false);
            _RoadGhost.GetComponent<LineRenderer>().material = TerrainController._Instance._RoadWhiteGhostMat;
        }
        else
        {
            GameManager._Instance._RoadCannotPlaceText.SetActive(true);
            _RoadGhost.GetComponent<LineRenderer>().material = TerrainController._Instance._RoadRedGhostMat;
        }
    }
    public bool IsRoadTooShort(Vector3 worldPos)
    {
        Vector3 lastKnotPosition = _ActiveSplineContainer.transform.TransformPoint(_ActiveSplineContainer.Splines[0][_ActiveSplineContainer.Splines[0].Count - 1].Position);
        if ((worldPos - lastKnotPosition).magnitude < 40)
            return true;
        return false;
    }
    public bool IsRoadAngleTooBig(Vector3 worldPos)
    {
        if (_ActiveSplineContainer.Splines[0].Count > 1)
        {
            Vector3 firstDirection = ((Vector3)(_ActiveSplineContainer.Splines[0][_ActiveSplineContainer.Splines[0].Count - 1].Position - _ActiveSplineContainer.Splines[0][_ActiveSplineContainer.Splines[0].Count - 2].Position)).normalized;
            Vector3 secondDirection = (_ActiveSplineContainer.transform.InverseTransformPoint(worldPos) - (Vector3)_ActiveSplineContainer.Splines[0][_ActiveSplineContainer.Splines[0].Count - 1].Position).normalized;
            float angle = Vector3.Angle(firstDirection, secondDirection);
            if (angle > 60f)
                return true;
            return false;
        }
        return false;
    }
    private float GetTerrainHeightAtPosition(Vector3 pos, float rayDistance)
    {
        Ray ray = new Ray(pos + Vector3.up * rayDistance, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance * 1.5f, LayerMask.GetMask("Terrain")))
        {
            if (hit.collider.TryGetComponent<Terrain>(out Terrain terrain))
            {
                Vector3 terrainPos = pos - terrain.transform.position;

                float normX = terrainPos.x / terrain.terrainData.size.x;
                float normZ = terrainPos.z / terrain.terrainData.size.z;

                float height = terrain.SampleHeight(pos);
                Vector3 terrainNormal = terrain.terrainData.GetInterpolatedNormal(normX, normZ);
                Vector3 offset = terrainNormal * 5f;
                offset = offset.y < 0 ? Vector3.zero : offset;
                return (new Vector3(pos.x, height, pos.z) + offset).y;

                //return hit.point.y;
            }
        }
        return 0f;
    }
    private void RoadMeshToTerrainHeight(Mesh mesh, Transform meshTransform)
    {
        if (mesh == null)
        {
            Debug.LogError("Mesh is null!");
            return;
        }

        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 worldPos = meshTransform.TransformPoint(vertices[i]);

            float terrainHeight = GetTerrainHeightAtPosition(worldPos, 300f) + UnityEngine.Random.Range(-0.01f, 0.01f);
            Vector3 adjustedWorldPos = new Vector3(worldPos.x, terrainHeight + _heightOffset, worldPos.z);
            vertices[i] = meshTransform.InverseTransformPoint(adjustedWorldPos);

            if (i % 4 < 2)
                vertices[i].y -= 20f;
        }
        mesh.vertices = vertices;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        List<int> topTriangles = new List<int>();
        List<int> otherTriangles = new List<int>();
        for (int i = 0; i < triangles.Length; i += 3)
        {
            if (i + 2 >= triangles.Length) continue;

            int i0 = triangles[i];
            int i1 = triangles[i + 1];
            int i2 = triangles[i + 2];

            Vector3 v0 = vertices[i0];
            Vector3 v1 = vertices[i1];
            Vector3 v2 = vertices[i2];

            Vector3 side1 = v1 - v0;
            Vector3 side2 = v2 - v0;

            Vector3 normal = Vector3.Cross(side1, side2).normalized;
            float dot = Vector3.Dot(normal, Vector3.up);

            if (dot > 0.7f) // 0.9 yerine tolerans deðerini ayarlayabilirsin
            {
                topTriangles.Add(i0);
                topTriangles.Add(i1);
                topTriangles.Add(i2);
            }
            else
            {
                otherTriangles.Add(i0);
                otherTriangles.Add(i1);
                otherTriangles.Add(i2);
            }
        }

        mesh.subMeshCount = 2;
        mesh.SetTriangles(topTriangles.ToArray(), 0);
        mesh.SetTriangles(otherTriangles.ToArray(), 1);

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
    }

    public void ReArrangeSpline(SplineContainer container)
    {
        //container.Splines[0].ConvertToCurved();
        for (int i = 0; i < container[0].Count; i++)
        {
            if (i != 0 && i != container[0].Count - 1)
                container[0].SetTangentMode(i, TangentMode.AutoSmooth);
        }
        container.GetComponent<SplineExtrude>().Capped = true;
        container.GetComponent<SplineExtrude>().Rebuild();
        GameManager._Instance.CallForAction(() => RoadMeshToTerrainHeight(container.GetComponent<MeshFilter>().sharedMesh, container.transform), 0.01f, false);
    }
    public RoadSplineData GetRoadDataForSaving(GameObject containerObj)
    {
        RoadSplineData newData = new RoadSplineData();
        newData._RoadKnotPositions = new List<float3>();
        newData._RoadKnotRotations = new List<quaternion>();
        newData._RoadKnotTangentIn = new List<float3>();
        newData._RoadKnotTangentOut = new List<float3>();
        SplineContainer container = containerObj.GetComponent<SplineContainer>();
        foreach (BezierKnot knot in container.Splines[0])
        {
            newData._RoadKnotPositions.Add(knot.Position);
            newData._RoadKnotRotations.Add(knot.Rotation);
            newData._RoadKnotTangentIn.Add(knot.TangentIn);
            newData._RoadKnotTangentOut.Add(knot.TangentOut);
        }
        return newData;
    }
    public void SetRoadDataForLoading(GameObject roadObj, RoadSplineData data)
    {
        for (int i = 0; i < data._RoadKnotPositions.Count; i++)
        {
            BezierKnot newKnot = new BezierKnot();
            newKnot.Position = data._RoadKnotPositions[i];
            newKnot.Rotation = data._RoadKnotRotations[i];
            newKnot.TangentIn = data._RoadKnotTangentIn[i];
            newKnot.TangentOut = data._RoadKnotTangentOut[i];

            roadObj.GetComponent<SplineContainer>().Splines[0].Add(newKnot);
        }
    }
    public void AddRoad(TerrainPoint terrainPoint)
    {
        if (_ActiveSplineContainer == null)
            FindOrStartSpline(terrainPoint._Position);
        else
            TryAddKnotToSpline(terrainPoint._Position);

        GameManager._Instance.OpenOrCloseProcessingScreen(false);
    }

    private void TryAddKnotToSpline(Vector3 worldPos)
    {
        if (_ActiveSplineContainer.Splines[0] == null)
        {
            Debug.LogError("Spline null!");
            return;
        }

        if (_ActiveSplineContainer.Splines[0].Count == 0)
        {
            AddKnotToSpline(worldPos);
            return;
        }

        Vector3 lastKnotPosition = _ActiveSplineContainer.transform.TransformPoint(_ActiveSplineContainer.Splines[0][_ActiveSplineContainer.Splines[0].Count - 1].Position);
        if ((worldPos - lastKnotPosition).magnitude < 20)
            return;


        int safety = 0;
        while ((lastKnotPosition - worldPos).magnitude > _maxRoadMagnitude * 0.9f)
        {
            if (++safety > 50)
            {
                Debug.LogError("While loop break — potential infinite loop");
                break;
            }
            //Debug.Log("While Method");
            AddKnotToSpline((worldPos - lastKnotPosition).normalized * _maxRoadMagnitude * 0.9f + lastKnotPosition);
            lastKnotPosition = _ActiveSplineContainer.transform.TransformPoint(_ActiveSplineContainer.Splines[0][_ActiveSplineContainer.Splines[0].Count - 1].Position);
        }

        AddKnotToSpline(worldPos);
    }
    private void AddKnotToSpline(Vector3 worldPos)
    {
        Vector3 worldPosOld = worldPos;
        float snapDistance = _SnapDistance;
        Transform containerTransform = GetContainer(TerrainController._Instance._SelectedConstructionType);
        Vector3 snappedKnotDirection = Vector3.zero;
        foreach (Transform containerObject in containerTransform.transform)
        {
            if (containerObject.name == "Pool") continue;
            Spline spline = containerObject.GetComponent<SplineContainer>().Splines[0];
            for (int i = 0; i < spline.Count; i++)
            {
                Vector3 knotWorldPos = containerTransform.transform.TransformPoint(spline[i].Position);
                if (Vector3.Distance(new Vector3(worldPosOld.x, 0f, worldPosOld.z), new Vector3(knotWorldPos.x, 0f, knotWorldPos.z)) <= snapDistance)
                {
                    worldPos = knotWorldPos;
                    snapDistance = Vector3.Distance(new Vector3(worldPosOld.x, 0f, worldPosOld.z), new Vector3(knotWorldPos.x, 0f, knotWorldPos.z));

                    if (spline.Count > 1 && (i == 0 || i == spline.Count - 1))
                    {
                        if (i == 0)
                            snappedKnotDirection = ((Vector3)(spline[1].Position - spline[0].Position)).normalized;
                        else if (i == spline.Count - 1)
                            snappedKnotDirection = ((Vector3)(spline[spline.Count - 2].Position - spline[spline.Count - 1].Position)).normalized;
                    }
                }
            }
        }

        worldPos.y = GetTerrainHeightAtPosition(worldPos, 1000f);
        Vector3 localStart = _ActiveSplineContainer.transform.InverseTransformPoint(worldPos);

        Vector3 tangentOut = Vector3.zero;
        int knotCount = _ActiveSplineContainer.Splines[0].Count;
        if (knotCount > 0)
        {
            var prevKnot = _ActiveSplineContainer.Splines[0][knotCount - 1];
            Vector3 prevPos = prevKnot.Position;
            tangentOut = (localStart - prevPos).normalized * 5f;
        }
        Vector3 tangentIn = -tangentOut;

        if (knotCount > 0 && snappedKnotDirection != Vector3.zero)
        {
            Vector3 newRoadDir = (worldPos - (Vector3)_ActiveSplineContainer.Splines[0][knotCount - 1].Position).normalized;
            Vector3 avgDir = (newRoadDir + snappedKnotDirection).normalized;

            float tangentLength = 2f;
            Vector3 tangent = avgDir * tangentLength;
            tangentIn.z = -tangent.z;
            tangentIn.y = 0f;
            tangentIn.x = 0f;
        }

        _ActiveSplineContainer.Splines[0].Add(new BezierKnot(localStart, tangentIn, tangentOut, Quaternion.identity));
        if (_ActiveSplineContainer.Splines[0].GetLength() > _maxRoadMagnitude)
            SplitNewRoad(worldPos);
        else
            ReArrangeSpline(_ActiveSplineContainer);

        _RoadPointGhost.SetActive(true);
        _RoadPointGhost.transform.position = worldPos;
        //Debug.Log($"added knot to {_ActiveSplineContainer.Splines[0]} in {TerrainController._Instance._SelectedConstructionType}");
    }
    private void SplitNewRoad(Vector3 worldPos)
    {
        if (_ActiveSplineContainer.Splines[0].Count == 1)
        {
            Debug.Log("Count 1!");
        }

        BezierKnot midNode = _ActiveSplineContainer.Splines[0][_ActiveSplineContainer.Splines[0].Count - 2];
        _ActiveSplineContainer.Splines[0].RemoveAt(_ActiveSplineContainer.Splines[0].Count - 1);
        ReArrangeSpline(_ActiveSplineContainer);
        Vector3 midPos = _ActiveSplineContainer.transform.TransformPoint(midNode.Position);

        SplineContainer newSpline = StartSplineObject(_ActiveSplineContainer.transform.parent, midPos);
        SplineContainer oldSpline = _ActiveSplineContainer;
        ChangeActiveSplineContainer(newSpline);
        worldPos.y = GetTerrainHeightAtPosition(worldPos, 1000f);
        Vector3 connectionDirection = (worldPos - midPos).normalized;
        _ActiveSplineContainer.Splines[0].Add(new BezierKnot((Vector3)midNode.Position + connectionDirection * 30f, Vector3.zero, Vector3.zero, Quaternion.identity));
        _ActiveSplineContainer.Splines[0].Add(new BezierKnot(_ActiveSplineContainer.transform.InverseTransformPoint(worldPos), Vector3.zero, Vector3.zero, Quaternion.identity));
        SetTangentForSplit(oldSpline[0], newSpline[0], (worldPos - midPos).normalized);
        ReArrangeSpline(_ActiveSplineContainer);
    }
    private void SetTangentForSplit(Spline spline1, Spline spline2, Vector3 newRoadRid)
    {
        if (spline1.Count < 2) return;

        Vector3 oldRoadDir = ((Vector3)(spline1[spline1.Count - 1].Position - spline1[spline1.Count - 2].Position)).normalized;
        Vector3 avgDir = (oldRoadDir + newRoadRid).normalized;

        float tangentLength = 2f;
        Vector3 tangent = avgDir * tangentLength;

        BezierKnot smoothKnot = spline2[0];
        smoothKnot.TangentOut.z = tangent.z;

        spline2.SetKnot(0, smoothKnot);
    }

    private void FindOrStartSpline(Vector3 worldPos)
    {
        Transform container = GetContainer(TerrainController._Instance._SelectedConstructionType);
        float snapDistance = _SnapDistance;
        bool isEnd = false;
        Transform containerObjForEnds = null;
        foreach (Transform containerObject in container.transform)
        {
            if (containerObject.name == "Pool") continue;
            Spline spline = containerObject.GetComponent<SplineContainer>().Splines[0];
            int knotCount = spline.Count;

            for (int i = 0; i < knotCount; i++)
            {
                Vector3 knotWorldPos = containerObject.transform.TransformPoint(spline[i].Position);
                if (Vector3.Distance(new Vector3(worldPos.x, 0f, worldPos.z), new Vector3(knotWorldPos.x, 0f, knotWorldPos.z)) <= snapDistance)
                {
                    if (i == 0 || i == knotCount - 1)
                    {
                        containerObjForEnds = containerObject;
                        if (i == 0) containerObjForEnds.GetComponent<SplineContainer>().Splines[0].Reverse();
                        snapDistance = Vector3.Distance(new Vector3(worldPos.x, 0f, worldPos.z), new Vector3(knotWorldPos.x, 0f, knotWorldPos.z));
                        worldPos = knotWorldPos;
                        isEnd = true;
                    }
                    else
                    {
                        snapDistance = Vector3.Distance(new Vector3(worldPos.x, 0f, worldPos.z), new Vector3(knotWorldPos.x, 0f, knotWorldPos.z));
                        worldPos = knotWorldPos;
                        isEnd = false;
                    }
                }
            }
        }

        _RoadPointGhost.SetActive(true);
        _RoadPointGhost.transform.position = worldPos;

        if (isEnd)
        {
            ChangeActiveSplineContainer(containerObjForEnds.GetComponent<SplineContainer>());
            //Debug.Log($"Snapped and extended {spline} in {TerrainController._Instance._SelectedConstructionType}");
        }
        else
        {
            //Debug.Log($"Snapped and started new {spline} in {TerrainController._Instance._SelectedConstructionType}");
            StartSplineObject(container, worldPos);
        }

    }
    private SplineContainer StartSplineObject(Transform containerTransform, Vector3 worldPos)
    {
        GameObject newContainerObj = GetRoadFromPool(containerTransform);
        ChangeActiveSplineContainer(newContainerObj.GetComponent<SplineContainer>());
        TryAddKnotToSpline(worldPos);
        //Debug.Log($"Started new spline in {TerrainController._Instance._SelectedConstructionType}");
        ArrangePool(containerTransform);
        return newContainerObj.GetComponent<SplineContainer>();
    }
    public void ChangeActiveSplineContainer(SplineContainer newContainer)
    {
        if (_ActiveSplineContainer != null && _ActiveSplineContainer != newContainer && _ActiveSplineContainer.Splines[0].Count <= 1) Destroy(_ActiveSplineContainer.gameObject);
        if (_ActiveSplineContainer != null && newContainer == null) _RoadPointGhost.SetActive(false);
        _ActiveSplineContainer = newContainer;

        if (_ActiveSplineContainer == null)
            _RoadGhost.SetActive(false);
        else
            _RoadGhost.SetActive(true);
    }
    private GameObject GetRoadFromPool(Transform containerTransform)
    {
        if (containerTransform.Find("Pool").childCount != 0)
        {
            Transform road = containerTransform.Find("Pool").GetChild(0);
            road.parent = containerTransform;
            return road.gameObject;
        }
        else
        {
            Debug.LogError("Pool is empty!");
            GameObject newContainerObj = Instantiate(GetRoadPrefab(containerTransform.name), containerTransform);
            newContainerObj.GetComponent<MeshFilter>().mesh = new Mesh();
            newContainerObj.AddComponent<MeshCollider>();
            newContainerObj.GetComponent<SplineContainer>().AddSpline(new Spline());
            return newContainerObj;
        }
    }
    private void ArrangePool(Transform containerTransform)
    {
        while (containerTransform.Find("Pool").childCount < 40)
        {
            InstantiateSplineObject(containerTransform);
        }
    }
    public GameObject InstantiateSplineObject(Transform containerTransform, bool isToPool = true)
    {
        GameObject newContainerObj = Instantiate(GetRoadPrefab(containerTransform.name), isToPool ? containerTransform.Find("Pool") : containerTransform); ;
        newContainerObj.GetComponent<MeshFilter>().mesh = new Mesh();
        newContainerObj.AddComponent<MeshCollider>();
        newContainerObj.GetComponent<SplineContainer>().AddSpline(new Spline());
        return newContainerObj;
    }
    public Transform GetContainer(ConstructionType type)
    {
        return type switch
        {
            ConstructionType.AsphaltRoad => _asphaltContainer,
            ConstructionType.DirtRoad => _dirtContainer,
            ConstructionType.RailRoad => _railContainer,
            _ => _asphaltContainer
        };
    }
    private GameObject GetRoadPrefab()
    {
        switch (TerrainController._Instance._SelectedConstructionType)
        {
            case ConstructionType.Bridge:
                return null;
            case ConstructionType.DirtRoad:
                return TerrainController._Instance._DirtRoadPrefab;
            case ConstructionType.AsphaltRoad:
                return TerrainController._Instance._AsphaltRoadPrefab;
            case ConstructionType.RailRoad:
                return TerrainController._Instance._RailRoadPrefab;
            default:
                Debug.LogError("prefab not found");
                return null;
        }
    }
    private GameObject GetRoadPrefab(string name)
    {
        switch (name)
        {
            case "RoadSystemDirt":
                return TerrainController._Instance._DirtRoadPrefab;
            case "RoadSystemAsphalt":
                return TerrainController._Instance._AsphaltRoadPrefab;
            case "RoadSystemRail":
                return TerrainController._Instance._RailRoadPrefab;
            default:
                Debug.LogError("prefab not found");
                return null;
        }
    }
}

[System.Serializable]
public class RoadSplineData
{
    public List<float3> _RoadKnotPositions;
    public List<quaternion> _RoadKnotRotations;
    public List<float3> _RoadKnotTangentIn;
    public List<float3> _RoadKnotTangentOut;
}
