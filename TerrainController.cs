using Gaia;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Splines;

public class TerrainController : MonoBehaviour
{
    public static TerrainController _Instance;
    public GameObject _EmptyPrefab;
    public GameObject _BridgePrefab;
    public GameObject _DirtRoadPrefab;
    public GameObject _AsphaltRoadPrefab;
    public GameObject _RailRoadPrefab;
    public Material _RoadRedGhostMat;
    public Material _RoadWhiteGhostMat;
    public Material _RemovalGhostMat;

    private Material _removalGhostOldMat;

    public ConstructionType _SelectedConstructionType { get; private set; }
    public bool _IsRemovalTool { get; private set; }

    private TerrainPoint _mouseTerrainPoint;
    private GameObject _constructionGhostObject;
    private GameObject _removalToolGhostObject;

    private bool _bridgeErrorFlag;

    private Coroutine _addRoadCoroutine;

    void Awake()
    {
        _Instance = this;
    }
    private void Update()
    {
        if (GameManager._Instance._IsGameStopped) return;

        CheckForConstruction();
        UpdateConstructionUI();
    }

    public void ConstructionButtonClicked(int index)
    {
        RoadBuilder._Instance.ChangeActiveSplineContainer(null);

        if (index != 0)
            _IsRemovalTool = false;

        if (index == 0)
            _IsRemovalTool = !_IsRemovalTool;
        else if (index == 1)
            _SelectedConstructionType = _SelectedConstructionType = ConstructionType.Bridge;
        else if (index == 2)
            _SelectedConstructionType = _SelectedConstructionType = ConstructionType.DirtRoad;
        else if (index == 3)
            _SelectedConstructionType = _SelectedConstructionType = ConstructionType.AsphaltRoad;
        else if (index == 4)
            _SelectedConstructionType = _SelectedConstructionType = ConstructionType.RailRoad;
    }


    private void UpdateConstructionUI()
    {
        if (_IsRemovalTool)
        {
            UpdateConstructionUICommon(true, false, false, false, false);
        }
        else if (_SelectedConstructionType is ConstructionType.Bridge)
        {
            UpdateConstructionUICommon(false, true, false, false, false);
        }
        else if (_SelectedConstructionType is ConstructionType.DirtRoad)
        {
            UpdateConstructionUICommon(false, false, true, false, false);
        }
        else if (_SelectedConstructionType is ConstructionType.AsphaltRoad)
        {
            UpdateConstructionUICommon(false, false, false, true, false);
        }
        else if (_SelectedConstructionType is ConstructionType.RailRoad)
        {
            UpdateConstructionUICommon(false, false, false, false, true);
        }
    }
    private void UpdateConstructionUICommon(bool first, bool second, bool third, bool fourth, bool fifth)
    {
        if (first ? !GameManager._Instance._ConstructionScreen.transform.Find("Remove").Find("SelectedImage").gameObject.activeSelf : GameManager._Instance._ConstructionScreen.transform.Find("Remove").Find("SelectedImage").gameObject.activeSelf)
            GameManager._Instance._ConstructionScreen.transform.Find("Remove").Find("SelectedImage").gameObject.SetActive(first);
        if (second ? !GameManager._Instance._ConstructionScreen.transform.Find("Bridge").Find("SelectedImage").gameObject.activeSelf : GameManager._Instance._ConstructionScreen.transform.Find("Bridge").Find("SelectedImage").gameObject.activeSelf)
            GameManager._Instance._ConstructionScreen.transform.Find("Bridge").Find("SelectedImage").gameObject.SetActive(second);
        if (third ? !GameManager._Instance._ConstructionScreen.transform.Find("DirtRoad").Find("SelectedImage").gameObject.activeSelf : GameManager._Instance._ConstructionScreen.transform.Find("DirtRoad").Find("SelectedImage").gameObject.activeSelf)
            GameManager._Instance._ConstructionScreen.transform.Find("DirtRoad").Find("SelectedImage").gameObject.SetActive(third);
        if (fourth ? !GameManager._Instance._ConstructionScreen.transform.Find("AsphaltRoad").Find("SelectedImage").gameObject.activeSelf : GameManager._Instance._ConstructionScreen.transform.Find("AsphaltRoad").Find("SelectedImage").gameObject.activeSelf)
            GameManager._Instance._ConstructionScreen.transform.Find("AsphaltRoad").Find("SelectedImage").gameObject.SetActive(fourth);
        if (fifth ? !GameManager._Instance._ConstructionScreen.transform.Find("RailRoad").Find("SelectedImage").gameObject.activeSelf : GameManager._Instance._ConstructionScreen.transform.Find("RailRoad").Find("SelectedImage").gameObject.activeSelf)
            GameManager._Instance._ConstructionScreen.transform.Find("RailRoad").Find("SelectedImage").gameObject.SetActive(fifth);
    }
    private void CheckForConstruction()
    {
        _mouseTerrainPoint = GetTerrainPointFromMouse();
        if (RoadBuilder._Instance._ActiveSplineContainer != null && RoadBuilder._Instance._RoadGhost.activeInHierarchy) RoadBuilder._Instance.UpdateRoadGhost(RoadBuilder._Instance._ActiveSplineContainer.Splines[0][RoadBuilder._Instance._ActiveSplineContainer.Splines[0].Count - 1].Position, _mouseTerrainPoint._Position, _mouseTerrainPoint);
        if (!_IsRemovalTool && _removalToolGhostObject != null) RemovalGhostBackToOriginal();
        if (_IsRemovalTool) { RoadBuilder._Instance._RoadPointGhost.SetActive(false); RoadBuilder._Instance._RoadGhost.SetActive(false); }
        if ((!GameManager._Instance._ConstructionScreen.activeInHierarchy || RoadBuilder._Instance._ActiveSplineContainer == null) && !_bridgeErrorFlag) { GameManager._Instance._RoadCannotPlaceText.SetActive(false); }
        if (!GameManager._Instance._ConstructionScreen.activeInHierarchy) { _IsRemovalTool = false; RoadBuilder._Instance.ChangeActiveSplineContainer(null); }
        if ((!GameManager._Instance._ConstructionScreen.activeInHierarchy || _IsRemovalTool) && _constructionGhostObject != null) Destroy(_constructionGhostObject);
        if (GameManager._Instance._ConstructionScreen.activeInHierarchy) ArrangeConstructionGhostObject(_mouseTerrainPoint);
        if (GameManager._Instance._ConstructionScreen.activeInHierarchy && GameManager._Instance._InputActions.FindAction("RemovalTool").triggered) { _IsRemovalTool = !_IsRemovalTool; RoadBuilder._Instance.ChangeActiveSplineContainer(null); }
        if (GameManager._Instance._ConstructionScreen.activeInHierarchy && Input.GetKeyDown(KeyCode.Alpha1)) { _IsRemovalTool = false; _SelectedConstructionType = ConstructionType.Bridge; RoadBuilder._Instance.ChangeActiveSplineContainer(null); }
        if (GameManager._Instance._ConstructionScreen.activeInHierarchy && Input.GetKeyDown(KeyCode.Alpha2)) { _IsRemovalTool = false; _SelectedConstructionType = ConstructionType.DirtRoad; RoadBuilder._Instance.ChangeActiveSplineContainer(null); }
        if (GameManager._Instance._ConstructionScreen.activeInHierarchy && Input.GetKeyDown(KeyCode.Alpha3)) { _IsRemovalTool = false; _SelectedConstructionType = ConstructionType.AsphaltRoad; RoadBuilder._Instance.ChangeActiveSplineContainer(null); }
        if (GameManager._Instance._ConstructionScreen.activeInHierarchy && Input.GetKeyDown(KeyCode.Alpha4)) { _IsRemovalTool = false; _SelectedConstructionType = ConstructionType.RailRoad; RoadBuilder._Instance.ChangeActiveSplineContainer(null); }
        if (GameManager._Instance._ConstructionScreen.activeInHierarchy && Input.GetKeyDown(KeyCode.Return)) { RoadBuilder._Instance.ChangeActiveSplineContainer(null); }

        if (GameManager._Instance._ConstructionScreen.activeInHierarchy && _IsRemovalTool)
        {
            ArrangeRemovalToolGhost();
        }

        if (GameManager._Instance._ConstructionScreen.activeInHierarchy && !_IsRemovalTool && (_SelectedConstructionType == ConstructionType.DirtRoad || _SelectedConstructionType == ConstructionType.AsphaltRoad || _SelectedConstructionType == ConstructionType.RailRoad))
        {
            ArrangeRoadPointGhost();
        }

        if (GameManager._Instance._ConstructionScreen.activeInHierarchy && Mouse.current.leftButton.wasPressedThisFrame)
        {
            ArrangeConstructionEvent();
        }
    }
    private void ArrangeRemovalToolGhost()
    {
        Physics.Raycast(Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue()), out RaycastHit hit, 30000f, LayerMask.GetMask("UpperTerrain"));
        if (hit.collider != null && (hit.collider.CompareTag("DirtRoad") || hit.collider.CompareTag("AsphaltRoad") || hit.collider.CompareTag("RailRoad") || hit.collider.CompareTag("Bridge")))
        {
            if (_removalToolGhostObject != null)
                _removalToolGhostObject.GetComponent<MeshRenderer>().material = _removalGhostOldMat;
            _removalToolGhostObject = hit.collider.gameObject;

            _removalGhostOldMat = _removalToolGhostObject.GetComponent<MeshRenderer>().material;
            _removalToolGhostObject.GetComponent<MeshRenderer>().material = _RemovalGhostMat;
        }
        else
        {
            if (_removalToolGhostObject != null)
                RemovalGhostBackToOriginal();
        }
    }
    private void RemovalGhostBackToOriginal()
    {
        _removalToolGhostObject.GetComponent<MeshRenderer>().material = _removalGhostOldMat;
        _removalToolGhostObject = null;
    }

    private void ArrangeRoadPointGhost()
    {
        bool isSnapped = false;
        Vector3 worldPos = _mouseTerrainPoint._Position;
        Vector3 worldPosOld = worldPos;
        float snapDistance = RoadBuilder._Instance._SnapDistance;
        Transform containerTransform = RoadBuilder._Instance.GetContainer(TerrainController._Instance._SelectedConstructionType);
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
                    isSnapped = true;
                    RoadBuilder._Instance._RoadPointGhost.SetActive(true);
                    RoadBuilder._Instance._RoadPointGhost.transform.position = worldPos;
                }
            }
        }
        if (!isSnapped)
        {
            RoadBuilder._Instance._RoadPointGhost.SetActive(false);
        }
    }

    private void ArrangeConstructionEvent()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (_IsRemovalTool)
        {
            RemoveConstruction();
        }
        else if (CanPlaceConstruction(_mouseTerrainPoint))
        {
            switch (_SelectedConstructionType)
            {
                case ConstructionType.Bridge:
                    AddBridge(_mouseTerrainPoint);
                    break;
                case ConstructionType.DirtRoad:
                    AddRoad(_mouseTerrainPoint);
                    break;
                case ConstructionType.AsphaltRoad:
                    AddRoad(_mouseTerrainPoint);
                    break;
                case ConstructionType.RailRoad:
                    AddRoad(_mouseTerrainPoint);
                    break;
                default:
                    Debug.LogError("Construction Type Not Found!");
                    break;
            }
        }
    }

    private void ArrangeConstructionGhostObject(TerrainPoint terrainPoint)
    {
        if (_IsRemovalTool) return;

        if (_constructionGhostObject == null)
        {
            CreateGhostObject(terrainPoint);
        }
        else if (!IsGhostPrefabCorrect())
        {
            Destroy(_constructionGhostObject);
            CreateGhostObject(terrainPoint);
        }

        ArrangePlacingConstructionPosition(terrainPoint);

        if (!CanPlaceConstruction(terrainPoint) && _constructionGhostObject.transform.Find("RealPlaceMesh") != null && _constructionGhostObject.transform.Find("RealPlaceMesh").gameObject.activeInHierarchy)
        {
            _constructionGhostObject.transform.Find("CannotPlaceMesh").gameObject.SetActive(true);
            _constructionGhostObject.transform.Find("RealPlaceMesh").gameObject.SetActive(false);
        }
        else if (CanPlaceConstruction(terrainPoint) && _constructionGhostObject.transform.Find("RealPlaceMesh") != null && !_constructionGhostObject.transform.Find("RealPlaceMesh").gameObject.activeInHierarchy)
        {
            _constructionGhostObject.transform.Find("CannotPlaceMesh").gameObject.SetActive(false);
            _constructionGhostObject.transform.Find("RealPlaceMesh").gameObject.SetActive(true);
        }
    }
    private void ArrangePlacingConstructionPosition(TerrainPoint terrainPoint)
    {
        switch (_SelectedConstructionType)
        {
            case ConstructionType.Bridge:
                Physics.Raycast(Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue()), out RaycastHit hitRiver, 30000f, LayerMask.GetMask("Water"));
                if (hitRiver.collider != null && hitRiver.collider.CompareTag("River"))
                    ArrangeBridgeTransformFromRiver(hitRiver);
                else
                    _constructionGhostObject.transform.position = terrainPoint._Position;
                break;
            case ConstructionType.DirtRoad:
                _constructionGhostObject.transform.position = terrainPoint._Position;
                break;
            case ConstructionType.AsphaltRoad:
                _constructionGhostObject.transform.position = terrainPoint._Position;
                break;
            case ConstructionType.RailRoad:
                _constructionGhostObject.transform.position = terrainPoint._Position;
                break;
            default:
                Debug.LogError("Construction type not found.");
                break;
        }
    }
    private void ArrangeBridgeTransformFromRiver(RaycastHit riverHit)
    {
        MeshCollider collider = riverHit.collider as MeshCollider;
        Mesh mesh = collider.sharedMesh;

        int[] triangles = mesh.triangles;
        Vector3[] vertices = mesh.vertices;

        int triIndex = riverHit.triangleIndex * 3;

        Vector3 p0 = vertices[triangles[triIndex + 0]];
        Vector3 p1 = vertices[triangles[triIndex + 1]];
        Vector3 p2 = vertices[triangles[triIndex + 2]];

        Vector3 worldP0 = collider.transform.TransformPoint(p0);
        Vector3 worldP1 = collider.transform.TransformPoint(p1);
        Vector3 worldP2 = collider.transform.TransformPoint(p2);

        Vector3 tangent = (worldP1 - worldP0).normalized;
        Vector3 rotatedTangent = Quaternion.Euler(0f, 90f, 0f) * tangent;

        RaycastHit hit;
        Vector3 startPoint = Vector3.zero, endPoint = Vector3.zero;
        Vector3 offset = rotatedTangent;
        while (true)
        {
            Physics.Raycast(Camera.main.transform.position, riverHit.point + offset - Camera.main.transform.position, out hit, 30000f, LayerMask.GetMask("Water"));
            if (hit.collider != null && hit.collider.CompareTag("River"))
            {
                endPoint = hit.point;
                offset += rotatedTangent;
            }
            else if (offset.magnitude > rotatedTangent.magnitude * 200)
            {
                Debug.LogError("While take too long...");
                break;
            }
            else
                break;
        }
        offset = -rotatedTangent;
        while (true)
        {
            Physics.Raycast(Camera.main.transform.position, riverHit.point + offset - Camera.main.transform.position, out hit, 30000f, LayerMask.GetMask("Water"));
            if (hit.collider != null && hit.collider.CompareTag("River"))
            {
                startPoint = hit.point;
                offset -= rotatedTangent;
            }
            else if (offset.magnitude > rotatedTangent.magnitude * 200)
            {
                Debug.LogError("While take too long...");
                break;
            }
            else
                break;
        }

        Vector3 targetPos = (startPoint + endPoint) / 2f;
        _constructionGhostObject.transform.position = targetPos - Vector3.up * 2.5f;

        Quaternion rotation = Quaternion.identity;
        Physics.Raycast(Camera.main.transform.position, targetPos - Camera.main.transform.position, out RaycastHit finalHit, 30000f, LayerMask.GetMask("Water"));
        if (finalHit.collider != null && finalHit.collider.CompareTag("River"))
            rotation = Quaternion.LookRotation(tangent, finalHit.normal);
        _constructionGhostObject.transform.rotation = rotation;
    }

    public bool CanPlaceConstruction(TerrainPoint point)
    {
        switch (_SelectedConstructionType)
        {
            case ConstructionType.Bridge:
                foreach (var collider in _constructionGhostObject.GetComponent<BridgeConstructionGhost>()._TouchingColliders)
                {
                    if (collider != null && collider.CompareTag("Bridge")) return false;
                }
                if (_constructionGhostObject.transform.Find("EdgeDetectCollider_1").GetComponent<BridgeEdgeDetection>()._Colliders.Count != 0) return false;
                if (_constructionGhostObject.transform.Find("EdgeDetectCollider_2").GetComponent<BridgeEdgeDetection>()._Colliders.Count != 0) return false;
                if (point._TerrainLowerType == TerrainLowerType.River) return true;
                return false;
            case ConstructionType.DirtRoad:
            case ConstructionType.AsphaltRoad:
            case ConstructionType.RailRoad:
                if (RoadBuilder._Instance._ActiveSplineContainer != null && IsRouteTouchingLandOrWater(RoadBuilder._Instance._RoadGhost, LayerMask.NameToLayer("Water")))
                {
                    GameManager._Instance._RoadCannotPlaceText.GetComponent<TextMeshProUGUI>().text = Localization._Instance._UI[22];
                    return false;
                }
                if (RoadBuilder._Instance._ActiveSplineContainer != null && RoadBuilder._Instance.IsRoadAngleTooBig(point._Position))
                {
                    GameManager._Instance._RoadCannotPlaceText.GetComponent<TextMeshProUGUI>().text = Localization._Instance._UI[21];
                    return false;
                }
                if (RoadBuilder._Instance._ActiveSplineContainer != null && RoadBuilder._Instance.IsRoadTooShort(point._Position))
                {
                    GameManager._Instance._RoadCannotPlaceText.GetComponent<TextMeshProUGUI>().text = Localization._Instance._UI[20];
                    return false;
                }
                return true;
            default:
                Debug.LogError("construction type not found");
                return false;
        }
    }
    private void CreateGhostObject(TerrainPoint terrainPoint)
    {
        _constructionGhostObject = Instantiate(GetGhostPrefab(_SelectedConstructionType), terrainPoint._Position, Quaternion.identity);
        _constructionGhostObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        _constructionGhostObject.tag = "Untagged";
        if (_constructionGhostObject.GetComponentInChildren<BridgeUnitController>() != null)
        {
            _constructionGhostObject.AddComponent<BridgeConstructionGhost>();
            Destroy(_constructionGhostObject.GetComponentInChildren<BridgeUnitController>().gameObject);
            Destroy(_constructionGhostObject.GetComponent<Rigidbody>());
        }
        else
        {
            if (_SelectedConstructionType == ConstructionType.DirtRoad)
                _constructionGhostObject.name = "DirtRoad";
            else if (_SelectedConstructionType == ConstructionType.AsphaltRoad)
                _constructionGhostObject.name = "AsphaltRoad";
            else if (_SelectedConstructionType == ConstructionType.RailRoad)
                _constructionGhostObject.name = "RailRoad";
        }
        //destroy road object components if needed
    }
    private GameObject GetGhostPrefab(ConstructionType type)
    {
        switch (type)
        {
            case ConstructionType.Bridge:
                return _BridgePrefab;
            case ConstructionType.DirtRoad:
                return _EmptyPrefab;
            case ConstructionType.AsphaltRoad:
                return _EmptyPrefab;
            case ConstructionType.RailRoad:
                return _EmptyPrefab;
            default:
                Debug.LogError("Ghost prefab not found");
                return null;
        }
    }
    private bool IsGhostPrefabCorrect()
    {
        if (_constructionGhostObject.name.StartsWith("Bridge"))
        {
            return _SelectedConstructionType == ConstructionType.Bridge;
        }
        else if (_constructionGhostObject.name.StartsWith("DirtRoad"))
        {
            return _SelectedConstructionType == ConstructionType.DirtRoad;
        }
        else if (_constructionGhostObject.name.StartsWith("AsphaltRoad"))
        {
            return _SelectedConstructionType == ConstructionType.AsphaltRoad;
        }
        else if (_constructionGhostObject.name.StartsWith("RailRoad"))
        {
            return _SelectedConstructionType == ConstructionType.RailRoad;
        }

        Debug.LogError("Ghost Object Name is incorrect!");
        return false;
    }
    private void AddBridge(TerrainPoint terrainPoint)
    {
        if (terrainPoint._TerrainUpperType == TerrainUpperType.None && terrainPoint._TerrainLowerType == TerrainLowerType.River)
        {
            Instantiate(_BridgePrefab, _constructionGhostObject.transform.position, _constructionGhostObject.transform.rotation).transform.SetParent(GameObject.Find("Bridges").transform);
        }
    }
    private void AddRoad(TerrainPoint terrainPoint)
    {
        GameManager._Instance.CoroutineCall(ref _addRoadCoroutine, AddRoadCoroutine(terrainPoint), this);
    }
    private IEnumerator AddRoadCoroutine(TerrainPoint terrainPoint)
    {
        GameManager._Instance.OpenOrCloseProcessingScreen(true);
        yield return null;
        RoadBuilder._Instance.AddRoad(terrainPoint);
    }

    public TerrainPoint GetTerrainPointFromObject(Transform transform)
    {
        Ray terrainRay = new Ray(transform.position, Vector3.down);
        return GetTerrainCommon(terrainRay);
    }
    public TerrainPoint GetTerrainPointFromMouse()
    {
        Ray terrainRay = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        return GetTerrainCommon(terrainRay);
    }
    private TerrainPoint GetTerrainCommon(Ray terrainRay)
    {
        TerrainPoint terrainPoint = new TerrainPoint();
        if (Physics.Raycast(terrainRay, out RaycastHit hit, 30000f, GameManager._Instance._TerrainRayLayers) && hit.collider != null)
        {
            terrainPoint._Position = hit.point;
            terrainPoint._Normal = hit.normal;
            terrainPoint._TerrainLowerType = GetLowerType(hit, out float baseSupplyMultiplier);
            terrainPoint._BaseSupplyMultiplier = baseSupplyMultiplier;
            terrainPoint._Temperature = GetTemperature(hit);
            //Debug.Log(terrainPoint._TerrainLowerType + " " + terrainPoint._BaseSupplyMultiplier + " " + terrainPoint._Temperature);
        }
        if (Physics.Raycast(terrainRay, out hit, 30000f, LayerMask.GetMask("UpperTerrain")) && hit.collider != null)
        {
            terrainPoint._TerrainUpperType = GetUpperType(hit);
            terrainPoint._UpperTypeObject = GetUpperTypeObject(hit);
        }
        return terrainPoint;
    }

    private TerrainLowerType GetLowerType(RaycastHit hit, out float baseSupplyMultiplier)
    {
        if (hit.collider != null && (hit.collider.CompareTag("Sea") || hit.collider.CompareTag("Lake") || hit.collider.CompareTag("River")))
        {
            baseSupplyMultiplier = 0f;
            if (hit.collider.CompareTag("Sea")) return TerrainLowerType.Sea;
            else if (hit.collider.CompareTag("Lake")) return TerrainLowerType.Lake;
            else if (hit.collider.CompareTag("River")) return TerrainLowerType.River;
            return TerrainLowerType.Sea;
        }

        Terrain terrain = hit.collider.GetComponent<Terrain>();
        Vector3 localPos = hit.transform.InverseTransformPoint(hit.point);
        int x = Mathf.FloorToInt(localPos.x / terrain.terrainData.size.x * terrain.terrainData.alphamapWidth);
        int y = Mathf.FloorToInt(localPos.z / terrain.terrainData.size.z * terrain.terrainData.alphamapHeight);
        float[,,] alphaMap = terrain.terrainData.GetAlphamaps(x, y, 1, 1);
        float[] textureWeights = new float[alphaMap.GetLength(2)];
        for (int i = 0; i < alphaMap.GetLength(2); i++)
        {
            textureWeights[i] = alphaMap[0, 0, i];
        }
        TerrainLowerType lowerType = GetDominantTextureType(textureWeights, out baseSupplyMultiplier);
        return lowerType;
    }
    private TerrainUpperType GetUpperType(RaycastHit hit)
    {
        if (hit.collider == null) return TerrainUpperType.None;
        if (hit.collider.CompareTag("Forest")) return TerrainUpperType.Forest;
        if (hit.collider.CompareTag("City")) return TerrainUpperType.City;
        if (hit.collider.CompareTag("Bridge")) return TerrainUpperType.Bridge;
        if (hit.collider.CompareTag("DirtRoad")) return TerrainUpperType.DirtRoad;
        if (hit.collider.CompareTag("AsphaltRoad")) return TerrainUpperType.AsphaltRoad;
        if (hit.collider.CompareTag("RailRoad")) return TerrainUpperType.RailRoad;
        return TerrainUpperType.None;
    }
    private GameObject GetUpperTypeObject(RaycastHit hit)
    {
        if (hit.collider == null) return null;
        if (hit.collider.CompareTag("Forest")) return hit.collider.gameObject;
        if (hit.collider.CompareTag("City")) return hit.collider.gameObject;
        if (hit.collider.CompareTag("Bridge")) return hit.collider.gameObject;
        if (hit.collider.CompareTag("DirtRoad")) return hit.collider.gameObject;
        if (hit.collider.CompareTag("AsphaltRoad")) return hit.collider.gameObject;
        if (hit.collider.CompareTag("RailRoad")) return hit.collider.gameObject;
        return null;
    }
    private float GetTemperature(RaycastHit hit)
    {
        float baseTemp = 25f;
        float locationEffect = -hit.point.z * 0.0022f + hit.point.x * 0.0008f;

        float excess = 0;
        if (hit.point.y > 750f)
            excess = hit.point.y - 750f;

        if (locationEffect < 0)
            locationEffect *= 1.5f;
        else
            excess = Mathf.Clamp(excess - locationEffect * 18f, 0, 1000f);

        float heightEffect = -(hit.point.y - 360f) * 0.035f - excess / 10f;

        var weather = GameObject.FindAnyObjectByType<Gaia.ProceduralWorldsGlobalWeather>();
        float weatherOffset = weather.IsRaining ? -4f : (weather.IsSnowing ? -9f : 0f);
        return baseTemp + heightEffect + locationEffect + weatherOffset;
    }

    private TerrainLowerType GetDominantTextureType(float[] textureWeights, out float baseSupplyMultiplier)
    {
        baseSupplyMultiplier = 0;
        int dominantIndex = 0;
        float maxWeight = textureWeights[0];
        float weightMultiplier = 1f;
        for (int i = 0; i < textureWeights.Length; i++)
        {
            if (i == 1)
                baseSupplyMultiplier = Mathf.Clamp01(1f - (textureWeights[0] * 2f + textureWeights[5] * 2f + textureWeights[7] * 2f));

            if (i == 0)//desert
                weightMultiplier = 1.15f;
            else if (i == 1)//plain
                weightMultiplier = 0.6f;
            else if (i == 5)//rocky
                weightMultiplier = 3f;
            else if (i == 7)//snowy
                weightMultiplier = 10f;

            textureWeights[i] += i == 1 ? textureWeights[6] : 0f;//adds Stone Moss weight to plain weight
            if (textureWeights[i] * weightMultiplier > maxWeight && (i == 0 || i == 1 || i == 5 || i == 7))
            {
                maxWeight = textureWeights[i] * weightMultiplier;
                dominantIndex = i;
            }
        }

        switch (dominantIndex)
        {
            case 0:
                return TerrainLowerType.Desert;
            case 1:
                return TerrainLowerType.Plain;
            case 5:
                return TerrainLowerType.Rocky;
            case 7:
                return TerrainLowerType.Snowy;
            default:
                return TerrainLowerType.Plain;
        }
    }

    private void RemoveConstruction()
    {
        var terrainPoint = TerrainController._Instance.GetTerrainPointFromMouse();
        if (terrainPoint._TerrainUpperType != TerrainUpperType.None && terrainPoint._UpperTypeObject == null)
        {
            Debug.LogError("upper object is null!");
            return;
        }

        switch (terrainPoint._TerrainUpperType)
        {
            case TerrainUpperType.None:
                break;
            case TerrainUpperType.Forest:
                break;
            case TerrainUpperType.City:
                break;
            case TerrainUpperType.Bridge:
                if (terrainPoint._UpperTypeObject.GetComponentInChildren<BridgeUnitController>().IsCarryingAnyUnit())
                {
                    if (!_bridgeErrorFlag)
                    {
                        GameManager._Instance._RoadCannotPlaceText.SetActive(true);
                        _bridgeErrorFlag = true;
                        GameManager._Instance._RoadCannotPlaceText.GetComponent<TextMeshProUGUI>().text = Localization._Instance._UI[23];
                        GameManager._Instance.CallForAction(() => _bridgeErrorFlag = false, 4f, false); ;
                    }
                    break;
                }
                _removalToolGhostObject = null;
                _removalGhostOldMat = null;
                Destroy(terrainPoint._UpperTypeObject);
                break;
            case TerrainUpperType.DirtRoad:
                _removalToolGhostObject = null;
                _removalGhostOldMat = null;
                Destroy(terrainPoint._UpperTypeObject);
                break;
            case TerrainUpperType.AsphaltRoad:
                _removalToolGhostObject = null;
                _removalGhostOldMat = null;
                Destroy(terrainPoint._UpperTypeObject);
                break;
            case TerrainUpperType.RailRoad:
                _removalToolGhostObject = null;
                _removalGhostOldMat = null;
                Destroy(terrainPoint._UpperTypeObject);
                break;
            default:
                break;
        }
    }

    public bool IsRouteTouchingLandOrWater(GameObject GhostObject, int checkLayer)
    {
        if (GhostObject == null) return false;

        Vector3 tempPos;
        RaycastHit hit;
        Vector3 startPos = GhostObject.GetComponent<LineRenderer>().GetPosition(GhostObject.GetComponent<LineRenderer>().positionCount - 2);
        Vector3 endPos = GhostObject.GetComponent<LineRenderer>().GetPosition(GhostObject.GetComponent<LineRenderer>().positionCount - 1);
        float lerpValue = 0f;

        float distance = (endPos - startPos).magnitude;
        if (distance < 1f) distance = 1f;
        while (lerpValue < 1f)
        {
            tempPos = Vector3.Lerp(startPos, endPos, lerpValue);
            Physics.Raycast(tempPos + Vector3.up * 100f, -Vector3.up, out hit, 200f, GameManager._Instance._TerrainRayLayers);
            if (hit.collider != null && hit.collider.gameObject.layer == checkLayer && (tempPos - startPos).magnitude > 5f)
                return true;
            if (hit.collider != null && hit.collider.gameObject.CompareTag("River") && (tempPos - startPos).magnitude > 5f)
                return true;
            lerpValue += 8f / distance;
        }
        return false;
    }

    public Vector3 GetClosestPointToRoad(Vector3 mouseWorldPos)
    {
        float closestDistance = float.MaxValue;
        Vector3 closestPoint = mouseWorldPos;
        float snapDistance = 25000f;

        foreach (Transform road in GameObject.Find("RoadSystemDirt").transform)
        {
            if (road.name == "Pool") continue;

            Mesh mesh = road.GetComponent<MeshFilter>().sharedMesh;
            Transform t = road.GetComponent<MeshFilter>().transform;

            Vector3[] vertices = mesh.vertices;
            foreach (var vertex in vertices)
            {
                Vector3 worldVertex = t.TransformPoint(vertex);
                float dist = Vector3.SqrMagnitude(worldVertex - mouseWorldPos);
                if (dist < closestDistance && dist < snapDistance)
                {
                    closestDistance = dist;
                    closestPoint = worldVertex;
                }
            }
        }
        foreach (Transform road in GameObject.Find("RoadSystemAsphalt").transform)
        {
            if (road.name == "Pool") continue;

            Mesh mesh = road.GetComponent<MeshFilter>().sharedMesh;
            Transform t = road.GetComponent<MeshFilter>().transform;

            Vector3[] vertices = mesh.vertices;
            foreach (var vertex in vertices)
            {
                Vector3 worldVertex = t.TransformPoint(vertex);
                float dist = Vector3.SqrMagnitude(worldVertex - mouseWorldPos);
                if (dist < closestDistance && dist < snapDistance)
                {
                    closestDistance = dist;
                    closestPoint = worldVertex;
                }
            }
        }
        foreach (Transform road in GameObject.Find("RoadSystemRail").transform)
        {
            if (road.name == "Pool") continue;

            Mesh mesh = road.GetComponent<MeshFilter>().sharedMesh;
            Transform t = road.GetComponent<MeshFilter>().transform;

            Vector3[] vertices = mesh.vertices;
            foreach (var vertex in vertices)
            {
                Vector3 worldVertex = t.TransformPoint(vertex);
                float dist = Vector3.SqrMagnitude(worldVertex - mouseWorldPos);
                if (dist < closestDistance && dist < snapDistance)
                {
                    closestDistance = dist;
                    closestPoint = worldVertex;
                }
            }
        }

        return closestPoint;
    }
}

public enum ConstructionType
{
    Bridge,
    DirtRoad,
    AsphaltRoad,
    RailRoad
}
public enum TerrainLowerType
{
    Plain,
    Rocky,
    Snowy,
    Desert,
    River,
    Lake,
    Sea,
}
public enum TerrainUpperType
{
    None,
    Forest,
    City,
    Bridge,
    DirtRoad,
    AsphaltRoad,
    RailRoad,
}
public class TerrainPoint
{
    public Vector3 _Position;
    public Vector3 _Normal;
    public float _Temperature;
    public float _BaseSupplyMultiplier;

    public TerrainLowerType _TerrainLowerType;
    public TerrainUpperType _TerrainUpperType;

    public GameObject _UpperTypeObject;

    public bool _IsWater => _TerrainLowerType == TerrainLowerType.River || _TerrainLowerType == TerrainLowerType.Lake || _TerrainLowerType == TerrainLowerType.Sea;
    public bool _IsRoad => _TerrainUpperType == TerrainUpperType.DirtRoad || _TerrainUpperType == TerrainUpperType.AsphaltRoad || _TerrainUpperType == TerrainUpperType.RailRoad;
    public bool _IsBridge => _TerrainUpperType == TerrainUpperType.Bridge;
}
