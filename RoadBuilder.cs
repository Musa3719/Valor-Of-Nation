using UnityEngine;
using UnityEngine.Splines;
using System.Collections.Generic;

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
    private float _heightOffset = 1.5f;

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
        firstPos += Vector3.up * 5f;
        secondPos += Vector3.up * 10f;
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
        if ((worldPos - lastKnotPosition).magnitude < 20)
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
            if (angle > 105f)
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
                Vector3 offset = terrainNormal * 3.25f;
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

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 worldPos = meshTransform.TransformPoint(vertices[i]);

            float terrainHeight = GetTerrainHeightAtPosition(worldPos, 300f);
            Vector3 adjustedWorldPos = new Vector3(worldPos.x, terrainHeight + _heightOffset, worldPos.z);
            vertices[i] = meshTransform.InverseTransformPoint(adjustedWorldPos);
        }

        mesh.vertices = vertices;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        meshTransform.GetComponent<MeshRenderer>().enabled = true;
    }
    private void ReArrangeSpline(SplineContainer container)
    {
        //container.Splines[0].ConvertToCurved();
        container.GetComponent<SplineExtrude>().Capped = true;
        container.GetComponent<SplineExtrude>().Rebuild();
        container.GetComponent<MeshRenderer>().enabled = false;
        GameManager._Instance.CallForAction(() => RoadMeshToTerrainHeight(container.GetComponent<MeshFilter>().sharedMesh, container.transform), 0.01f, false);
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
                }
            }
        }

        worldPos.y = GetTerrainHeightAtPosition(worldPos, 1000f);
        Vector3 localStart = _ActiveSplineContainer.transform.InverseTransformPoint(worldPos);

        Vector3 tangent = Vector3.zero;
        int knotCount = _ActiveSplineContainer.Splines[0].Count;
        if (knotCount > 0)
        {
            var prevKnot = _ActiveSplineContainer.Splines[0][knotCount - 1];
            Vector3 prevPos = prevKnot.Position;
            tangent = (localStart - prevPos).normalized * 5f;
        }

        _ActiveSplineContainer.Splines[0].Add(new BezierKnot(localStart, -tangent, tangent, Quaternion.identity));
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

        ChangeActiveSplineContainer(StartSplineObject(_ActiveSplineContainer.transform.parent, _ActiveSplineContainer.transform.TransformPoint(midNode.Position)));
        worldPos.y = GetTerrainHeightAtPosition(worldPos, 1000f);
        _ActiveSplineContainer.Splines[0].Add(new BezierKnot(_ActiveSplineContainer.transform.InverseTransformPoint(worldPos), Vector3.zero, Vector3.zero, Quaternion.identity));
        ReArrangeSpline(_ActiveSplineContainer);
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
            GameObject newContainerObj = Instantiate(GetRoadPrefab(containerTransform.name), containerTransform.Find("Pool"));
            newContainerObj.GetComponent<MeshFilter>().mesh = new Mesh();
            newContainerObj.AddComponent<MeshCollider>();
            newContainerObj.GetComponent<SplineContainer>().AddSpline(new Spline());
        }
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
