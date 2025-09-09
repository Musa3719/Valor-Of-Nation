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
    private float _heightOffset = 1.25f;

    private void Awake()
    {
        _Instance = this;
        _dirtContainer = GameObject.Find("RoadSystemDirt").transform;
        _asphaltContainer = GameObject.Find("RoadSystemAsphalt").transform;
        _railContainer = GameObject.Find("RoadSystemRail").transform;
        _RoadPointGhost = GameObject.Find("RoadSystem").transform.Find("RoadPointGhost").gameObject;
        _RoadGhost = GameObject.Find("RoadSystem").transform.Find("RoadGhost").gameObject;
    }
    private void Start()
    {
        ArrangePool(_dirtContainer);
        ArrangePool(_asphaltContainer);
        ArrangePool(_railContainer);
    }

    public void UpdateRoadGhost(Vector3 firstPos, Vector3 secondPos, TerrainPoint point)
    {
        _RoadGhost.SetActive(true);
        List<Vector3> newList = new List<Vector3>();
        newList.Add(secondPos);
        TerrainController._Instance.ArrangeMergingLineRenderer(_RoadGhost.GetComponent<LineRenderer>(), firstPos, newList, -Vector3.up, upOffset: 3.25f);

        if (TerrainController._Instance.CanPlaceConstruction(point))
        {
            GameManager._Instance._RoadCannotPlaceText.SetActive(false);
            _RoadGhost.GetComponent<LineRenderer>().colorGradient = GameManager._Instance._WhiteGradientForPotentialRouteOrRoad; ;
        }
        else
        {
            GameManager._Instance._RoadCannotPlaceText.SetActive(true);
            _RoadGhost.GetComponent<LineRenderer>().colorGradient = GameManager._Instance._RedGradientForPotentialRouteOrRoad;
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
            if (angle > 100f)
                return true;
            return false;
        }
        return false;
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

            float terrainHeight = TerrainController._Instance.GetTerrainHeightAtPosition(worldPos, 80f) + UnityEngine.Random.Range(-0.01f, 0.01f);
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
            if (i == 0 || i == container[0].Count - 1)
                container[0].SetTangentMode(i, TangentMode.Continuous);
            else
                container[0].SetTangentMode(i, TangentMode.AutoSmooth);
        }
        container.GetComponent<SplineExtrude>().Capped = true;
        container.GetComponent<SplineExtrude>().Rebuild();
        GameManager._Instance.CallForAction(() => RoadMeshToTerrainHeight(container.GetComponent<MeshFilter>().sharedMesh, container.transform), 0.1f, false);
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

            roadObj.GetComponent<SplineContainer>().Splines[0].Add(newKnot, TangentMode.AutoSmooth);
        }
    }
    public void AddRoad(TerrainPoint terrainPoint)
    {
        if (_ActiveSplineContainer == null)
            FindOrStartSpline(terrainPoint._Position);
        else
            TryAddKnotToSpline(terrainPoint._Position);

        GameManager._Instance.CallForAction(() => GameManager._Instance.OpenOrCloseProcessingScreen(false), 0.1f, false);
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

        AddKnotToSpline(worldPos, true);
    }
    private void AddKnotToSpline(Vector3 worldPos, bool isCheckingSnap = false)
    {
        SplineContainer snappingSplineContainerForSmooth = CheckLastPointSnap(isCheckingSnap, ref worldPos);

        worldPos.y = TerrainController._Instance.GetTerrainHeightAtPosition(worldPos, 1000f);
        Vector3 localStart = _ActiveSplineContainer.transform.InverseTransformPoint(worldPos);

        _ActiveSplineContainer.Splines[0].Add(new BezierKnot(localStart), TangentMode.AutoSmooth);

        if (snappingSplineContainerForSmooth != null && _ActiveSplineContainer[0].Count > 1)
        {
            ReArrangeSpline(_ActiveSplineContainer);
            Vector3 beforePos = _ActiveSplineContainer.transform.TransformPoint(_ActiveSplineContainer[0][_ActiveSplineContainer[0].Count - 2].Position);
            Vector3 forwardPos = snappingSplineContainerForSmooth.transform.TransformPoint(snappingSplineContainerForSmooth[0][1].Position);
            AutoSmoothSplines(_ActiveSplineContainer, snappingSplineContainerForSmooth, worldPos, beforePos, forwardPos, _ActiveSplineContainer[0].Count - 1, 0);
        }


        if (_ActiveSplineContainer.Splines[0].GetLength() > _maxRoadMagnitude)
            SplitNewRoad(worldPos);
        else
            ReArrangeSpline(_ActiveSplineContainer);

        _RoadPointGhost.SetActive(true);
        _RoadPointGhost.transform.position = worldPos;
        //Debug.Log($"added knot to {_ActiveSplineContainer.Splines[0]} in {TerrainController._Instance._SelectedConstructionType}");
    }
    private SplineContainer CheckLastPointSnap(bool isCheckingSnap, ref Vector3 worldPos)
    {
        SplineContainer snappingSplineContainerForSmooth = null;
        Vector3 worldPosOld = worldPos;
        float snapDistance = _SnapDistance;
        Transform containerTransform = GetContainer(TerrainController._Instance._SelectedConstructionType);
        foreach (Transform containerObject in containerTransform.transform)
        {
            if (!isCheckingSnap) break;
            if (containerObject.name == "Pool") continue;
            Spline spline = containerObject.GetComponent<SplineContainer>().Splines[0];
            if (spline.Count < 2) continue;
            for (int i = 0; i < spline.Count; i++)
            {
                bool closeSnappingFromAngle = false;
                Vector3 knotWorldPos = containerTransform.transform.TransformPoint(spline[i].Position);
                float checkDistance = Vector3.Distance(new Vector3(worldPosOld.x, 0f, worldPosOld.z), new Vector3(knotWorldPos.x, 0f, knotWorldPos.z));
                if (checkDistance <= snapDistance)
                {
                    if (i == 0 || i == spline.Count - 1)
                    {
                        Vector3 newRoadDir = (knotWorldPos - _ActiveSplineContainer.transform.TransformPoint(_ActiveSplineContainer[0][_ActiveSplineContainer[0].Count - 1].Position)).normalized;
                        Vector3 oldRoadDir = ((Vector3)(i == 0 ? spline[1].Position : spline[spline.Count - 2].Position) - knotWorldPos).normalized;
                        float angle = Vector2.Angle(new Vector2(newRoadDir.x, newRoadDir.z), new Vector2(oldRoadDir.x, oldRoadDir.z));
                        if (angle <= 140f)
                        {
                            if (i == spline.Count - 1)
                                spline.Reverse();

                            snappingSplineContainerForSmooth = containerObject.GetComponent<SplineContainer>();
                        }
                        else
                            closeSnappingFromAngle = true;
                    }

                    if (!closeSnappingFromAngle)
                    {
                        worldPos = knotWorldPos;
                        snapDistance = Vector3.Distance(new Vector3(worldPosOld.x, 0f, worldPosOld.z), new Vector3(knotWorldPos.x, 0f, knotWorldPos.z));
                    }
                }
            }
        }
        return snappingSplineContainerForSmooth;
    }
    private void SplitNewRoad(Vector3 worldPos)
    {
        if (_ActiveSplineContainer.Splines[0].Count == 1)
        {
            Debug.Log("Count 1!");
        }
        worldPos.y = TerrainController._Instance.GetTerrainHeightAtPosition(worldPos, 1000f);

        BezierKnot midNode = _ActiveSplineContainer.Splines[0][_ActiveSplineContainer.Splines[0].Count - 2];
        _ActiveSplineContainer.Splines[0].RemoveAt(_ActiveSplineContainer.Splines[0].Count - 1);
        //ReArrangeSpline(_ActiveSplineContainer);
        Vector3 midPos = _ActiveSplineContainer.transform.TransformPoint(midNode.Position);

        SplineContainer oldSpline = _ActiveSplineContainer;
        SplineContainer newSpline = StartSplineObject(_ActiveSplineContainer.transform.parent, midPos);

        Vector3 connectionDirection = (worldPos - midPos).normalized;
        float connectionNodeDistance = (worldPos - midPos).magnitude > 160f ? 90f : 30f;
        Vector3 connectionNodePos = midPos + connectionDirection * connectionNodeDistance;

        _ActiveSplineContainer.Splines[0].Add(new BezierKnot(connectionNodePos), TangentMode.AutoSmooth);
        _ActiveSplineContainer.Splines[0].Add(new BezierKnot(_ActiveSplineContainer.transform.InverseTransformPoint(worldPos)), TangentMode.AutoSmooth);
        AutoSmoothSplines(oldSpline, newSpline, midPos, oldSpline.transform.TransformPoint(oldSpline.Splines[0][oldSpline.Splines[0].Count - 2].Position), connectionNodePos, oldSpline[0].Count - 1, 0);
    }
    private void AutoSmoothSplines(SplineContainer container1, SplineContainer container2, Vector3 midPos, Vector3 oldPos, Vector3 newPos, int spline1Index, int spline2Index)
    {
        Spline spline1 = container1[0];
        Spline spline2 = container2[0];
        if (spline1.Count == 0 || spline2.Count == 0) return;

        BezierKnot midKnot = SplineUtility.GetAutoSmoothKnot(midPos, oldPos, newPos);
        spline1.SetKnot(spline1Index, midKnot);
        spline2.SetKnot(spline2Index, midKnot);
        spline1.SetTangentMode(spline1Index, TangentMode.Continuous);
        spline2.SetTangentMode(spline2Index, TangentMode.Continuous);
        ReArrangeSpline(container1);
        ReArrangeSpline(container2);
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
