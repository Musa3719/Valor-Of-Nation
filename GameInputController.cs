using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class GameInputController : MonoBehaviour
{
    public Color _NotSelectedSquadColor;
    public Color _SelectedSquadColor;

    public static GameInputController _Instance;
    public List<GameObject> _SelectedUnits { get; private set; }
    public List<Squad> _SelectedSquads { get; private set; }
    public Order _CurrentPlayerOrder { get; set; }
    public Transform _ArmyUIContent { get; private set; }
    public Transform _OrderUITransform { get; private set; }

    public float _SplitPercent;


    private float _updateUICounter;

    private void Awake()
    {
        _Instance = this;
        _SelectedUnits = new List<GameObject>();
        _SelectedSquads = new List<Squad>();
        _CurrentPlayerOrder = new MoveOrder();
        _ArmyUIContent = GameObject.Find("InGameScreen").transform.Find("OtherInGameMenus").Find("Left").Find("Army").Find("ScrollView").Find("Viewport").Find("Content");
        _OrderUITransform = GameManager._Instance._InGameScreen.transform.Find("OtherInGameMenus").Find("Left").Find("Army").Find("OrderUI");
        _SplitPercent = 0.5f;
        _OrderUITransform.Find("SplitUnitSlider").GetComponent<Slider>().value = _SplitPercent;
        _OrderUITransform.Find("SplitUnitSlider").GetComponent<Slider>().onValueChanged.AddListener((float newValue) => { _SplitPercent = newValue; UpdateUI(); });
    }
    private void Update()
    {
        if (GameManager._Instance._IsGameStopped) return;


        ArrangeSquadSelectionUI();
        ArrangeRouteGhost();
        ArrangeSelectedUnits();
        ArrangeOrderToSelectedUnits();
        ArrangeCityUI();
        ArrangeOrderUIButtons();

        _updateUICounter += Time.deltaTime;
        if (_updateUICounter > 0.2f)
            UpdateUI();

        Debug.Log(_SelectedSquads.Count);
    }

    public void SelectThisTypeButtonClicked()
    {
        if (_SelectedSquads.Count == 0) return;
        SelectSquadUIOnlyThisType(_SelectedSquads[_SelectedSquads.Count - 1]);
    }
    public void SelectAllSquadsButtonClicked()
    {
        SelectAllSquads();
    }
    public void ToNewSquadButtonClicked()
    {
        if (_SelectedUnits.Count == 0 || _SelectedSquads.Count <= 1) return;
        if (!IsAllSquadsInSameUnit()) return;
        if (!IsAllSquadsSameType()) return;

        Squad firstSquad = _SelectedSquads[0];
        List<Squad> otherSquads = new List<Squad>();
        for (int i = 1; i < _SelectedSquads.Count; i++)
        {
            otherSquads.Add(_SelectedSquads[i]);
        }

        for (int i = 0; i < otherSquads.Count; i++)
        {
            firstSquad._Amount += otherSquads[i]._Amount;

            if (otherSquads[i]._AttachedUnit._Squads.Contains(otherSquads[i]))
                otherSquads[i]._AttachedUnit._Squads.Remove(otherSquads[i]);

            otherSquads[i]._AttachedUnit = null;
            DeSelectSquad(otherSquads[i]);
        }

    }
    public void DeSelectUnitButtonClicked(GameObject deSelectingObj)
    {
        RemoveSelectedUnit(deSelectingObj);

        UpdateUI();
    }
    public void ToNewUnitButtonClicked()
    {
        if (_SelectedUnits.Count == 0) return;
        if (!IsAllSelectedSquadsCloseToEachother()) return;

        Unit newUnitComponent = GameManager._Instance.CreateLandUnit(_SelectedUnits[0].transform.position).GetComponent<Unit>();
        foreach (var selectedSquad in _SelectedSquads)
        {
            if (selectedSquad._AttachedUnit != null && selectedSquad._AttachedUnit._Squads.Contains(selectedSquad))
                selectedSquad._AttachedUnit._Squads.Remove(selectedSquad);

            selectedSquad._AttachedUnit = newUnitComponent;
            newUnitComponent._Squads.Add(selectedSquad);
        }
        _SelectedUnits.Add(newUnitComponent.gameObject);
    }
    public void SplitUnitButtonClicked()
    {
        if (_SelectedUnits.Count == 0) return;
        if (!IsAllSelectedSquadsCloseToEachother()) return;

        Unit newUnitComponent = GameManager._Instance.CreateLandUnit(_SelectedUnits[0].transform.position).GetComponent<Unit>();
        foreach (var selectedSquad in _SelectedSquads)
        {
            if (selectedSquad._Amount <= 1) continue;

            Squad newSquad = selectedSquad.CreateNewSquadObject(newUnitComponent);

            newSquad._Amount = (int)((float)selectedSquad._Amount * _SplitPercent);
            selectedSquad._Amount -= newSquad._Amount;

            if (newSquad._Amount > 0)
                newUnitComponent._Squads.Add(newSquad);
        }
        if (newUnitComponent._Squads.Count > 0)
            _SelectedUnits.Add(newUnitComponent.gameObject);
        else
            Destroy(newUnitComponent.gameObject);
    }
    
    public void RemoveSelectedUnit(GameObject deSelectingObj)
    {
        List<Squad> squadsWillBeDeselected = new List<Squad>();
        foreach (var selectedSquad in _SelectedSquads)
        {
            if (selectedSquad._AttachedUnit == deSelectingObj.GetComponent<Unit>())
                squadsWillBeDeselected.Add(selectedSquad);
        }
        foreach (var squad in squadsWillBeDeselected)
        {
            DeSelectSquad(squad);
        }

        if (_SelectedUnits.Contains(deSelectingObj))
            _SelectedUnits.Remove(deSelectingObj);
    }
    public void SelectSquad(Squad squad)
    {
        if (GameInputController._Instance._SelectedSquads.Contains(squad)) return;

        GameInputController._Instance._SelectedSquads.Add(squad);
        squad._IsSquadSelected = true;

        GameInputController._Instance._ArmyUIContent.transform.parent.parent.parent.Find("OrderUI").Find("SelectThisTypeButton").GetComponent<Image>().sprite = GameInputController._Instance._SelectedSquads[GameInputController._Instance._SelectedSquads.Count - 1]._Icon;
    }
    public void DeSelectSquad(Squad squad)
    {
        if (!GameInputController._Instance._SelectedSquads.Contains(squad)) return;

        GameInputController._Instance._SelectedSquads.Remove(squad);
        squad._IsSquadSelected = false;

        if (GameInputController._Instance._SelectedSquads.Count > 0)
            GameInputController._Instance._ArmyUIContent.transform.parent.parent.parent.Find("OrderUI").Find("SelectThisTypeButton").GetComponent<Image>().sprite = GameInputController._Instance._SelectedSquads[GameInputController._Instance._SelectedSquads.Count - 1]._Icon;
    }

    private bool IsAllSelectedSquadsCloseToEachother(float distanceThreshold = 25f)
    {
        if (_SelectedSquads.Count == 0) return false;

        Vector3 firstPosition = _SelectedSquads[0]._AttachedUnit.transform.position;
        foreach (var selectedSquad in _SelectedSquads)
        {
            if ((selectedSquad._AttachedUnit.transform.position - firstPosition).magnitude > distanceThreshold)
                return false;
        }

        return true;
    }
    private bool IsAllSquadsInSameUnit()
    {
        if (_SelectedSquads.Count == 0) return false;

        Unit attachedUnit = _SelectedSquads[0]._AttachedUnit;
        foreach (var selectedSquad in _SelectedSquads)
        {
            if (selectedSquad._AttachedUnit != attachedUnit)
                return false;
        }
        return true;
    }
    private bool IsAllSquadsSameType()
    {
        if (_SelectedSquads.Count == 0) return false;

        var type = _SelectedSquads[0].GetType();
        foreach (var selectedSquad in _SelectedSquads)
        {
            if (selectedSquad.GetType() != type)
                return false;
        }
        return true;
    }

    private void ArrangeOrderUIButtons()
    {
        if (IsAllSelectedSquadsCloseToEachother() && !_OrderUITransform.Find("ToNewUnitButton").GetComponent<Button>().interactable)
        {
            _OrderUITransform.Find("ToNewUnitButton").GetComponent<Button>().interactable = true;
            _OrderUITransform.Find("SplitUnitButton").GetComponent<Button>().interactable = true;
        }
        else if (!IsAllSelectedSquadsCloseToEachother() && _OrderUITransform.Find("ToNewUnitButton").GetComponent<Button>().interactable)
        {
            _OrderUITransform.Find("ToNewUnitButton").GetComponent<Button>().interactable = false;
            _OrderUITransform.Find("SplitUnitButton").GetComponent<Button>().interactable = false;
        }

        if (_SelectedUnits.Count > 0 && !_OrderUITransform.Find("StopOrderButton").GetComponent<Button>().interactable)
        {
            _OrderUITransform.Find("StopOrderButton").GetComponent<Button>().interactable = true;
        }
        else if (_SelectedUnits.Count == 0 && _OrderUITransform.Find("StopOrderButton").GetComponent<Button>().interactable)
        {
            _OrderUITransform.Find("StopOrderButton").GetComponent<Button>().interactable = false;
        }

        if (_SelectedSquads.Count > 0 && !_OrderUITransform.Find("SelectThisTypeButton").GetComponent<Button>().interactable)
        {
            _OrderUITransform.Find("SelectThisTypeButton").GetComponent<Button>().interactable = true;
        }
        else if (_SelectedSquads.Count == 0 && _OrderUITransform.Find("SelectThisTypeButton").GetComponent<Button>().interactable)
        {
            _OrderUITransform.Find("SelectThisTypeButton").GetComponent<Button>().interactable = false;
        }

        if (IsAllSquadsInSameUnit() && IsAllSquadsSameType() && _SelectedSquads.Count > 1 && !_OrderUITransform.Find("ToNewSquadButton").GetComponent<Button>().interactable)
        {
            _OrderUITransform.Find("ToNewSquadButton").GetComponent<Button>().interactable = true;
        }
        else if ((!IsAllSquadsInSameUnit() || !IsAllSquadsSameType() || _SelectedSquads.Count <= 1) && _OrderUITransform.Find("ToNewSquadButton").GetComponent<Button>().interactable)
        {
            _OrderUITransform.Find("ToNewSquadButton").GetComponent<Button>().interactable = false;
        }
    }
    private void ArrangeSquadSelectionUI()
    {
        var squadRefs = FindObjectsByType<SquadRefForUI>(FindObjectsSortMode.None);
        if (Mouse.current.rightButton.wasPressedThisFrame && IsMouseOverUnitUI())
        {
            if (GameManager._Instance._InputActions.FindAction("Sprint").ReadValue<float>() == 0f)
                GameInputController._Instance._SelectedSquads.ClearSelected();
            foreach (var item in squadRefs)
            {
                item._IsPermittedToRightClickSelect = true;
            }

            UpdateUI();
        }

        if (Mouse.current.leftButton.wasPressedThisFrame && IsMouseOverUnitUI())
        {
            bool isShift = GameManager._Instance._InputActions.FindAction("Sprint").ReadValue<float>() != 0f;
            if (!isShift && !IsMouseOverSquadUI(squadRefs) && !IsMouseOverAnyButtonOrSliderUI())
                GameInputController._Instance._SelectedSquads.ClearSelected();
        }
    }

    public bool IsMouseOverAnyButtonOrSliderUI()
    {
        Button[] targetButtons = _OrderUITransform.parent.GetComponentsInChildren<Button>();
        Slider[] targetSliders = _OrderUITransform.parent.GetComponentsInChildren<Slider>();

        PointerEventData pointer = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, results);

        foreach (RaycastResult result in results)
        {
            if (targetButtons.Contains(result.gameObject) || targetSliders.Contains(result.gameObject))
                return true;
        }

        return false;
    }

    private bool IsMouseOverSquadUI(SquadRefForUI[] squadRefs)
    {
        foreach (var item in squadRefs)
        {
            if (item._IsMouseOver)
                return true;
        }
        return false;
    }

    private bool IsMouseOverUnitUI()
    {
        if (!GameManager._Instance._InGameScreen.transform.Find("OtherInGameMenus").Find("Left").gameObject.activeInHierarchy) return false;

        return RectTransformUtility.RectangleContainsScreenPoint(GameManager._Instance._InGameScreen.transform.Find("OtherInGameMenus").Find("Left").GetComponent<RectTransform>(), Input.mousePosition, null);
    }

    public void UpdateUI()
    {
        _updateUICounter = 0;

        if (GameManager._Instance._InGameScreen.transform.Find("OtherInGameMenus").Find("Left").Find("Army").gameObject.activeInHierarchy)
            UpdateArmyUI();
        else if (GameManager._Instance._InGameScreen.transform.Find("OtherInGameMenus").Find("Left").Find("City").gameObject.activeInHierarchy)
            UpdateCityUI();
    }
    private void UpdateCityUI()
    {

    }
    private void UpdateArmyUI()
    {
        if (_SelectedUnits.Count == 0)
        {
            CloseAllInGameOtherUI();
            return;
        }
        _OrderUITransform.Find("SplitUnitPercent").GetComponent<TextMeshProUGUI>().text = "%" + ((int)(_SplitPercent * 100)).ToString();

        int unitUICount = _ArmyUIContent.childCount;
        if (_SelectedUnits.Count > unitUICount)
        {
            for (int i = 0; i < _SelectedUnits.Count; i++)
            {
                if (_ArmyUIContent.childCount <= i)
                {
                    GameObject newUnitUI = Instantiate(GameManager._Instance._UnitUIPrefab, _ArmyUIContent);
                    newUnitUI.GetComponent<UnitRefForUI>()._UnitReferance = _SelectedUnits[i].GetComponent<Unit>();
                }
                else
                {
                    _ArmyUIContent.transform.GetChild(i).GetComponent<UnitRefForUI>()._UnitReferance = _SelectedUnits[i].GetComponent<Unit>();
                }
            }
        }
        else if (_SelectedUnits.Count < unitUICount)
        {
            for (int i = unitUICount - _SelectedUnits.Count - 1; i > -1; i--)
            {
                _ArmyUIContent.GetChild(i).transform.localEulerAngles = new Vector3(_ArmyUIContent.GetChild(i).transform.localEulerAngles.x, -1f, _ArmyUIContent.GetChild(i).transform.localEulerAngles.z);
                Destroy(_ArmyUIContent.GetChild(i).gameObject);
            }
        }

        Transform unitUI = null;
        int indexForDestroyingUI = 0;
        for (int i = 0; i < _ArmyUIContent.childCount; i++)
        {
            unitUI = _ArmyUIContent.GetChild(i);
            if (unitUI.localEulerAngles.y != 0f)
            {
                indexForDestroyingUI++;
                continue;
            }

            Unit refUnit = _SelectedUnits[i - indexForDestroyingUI].GetComponent<Unit>();
            unitUI.GetComponent<UnitRefForUI>()._UnitReferance = refUnit;
            unitUI.transform.Find("SelfContent").Find("NonInfAmount").GetComponent<TextMeshProUGUI>().text = refUnit.GetTotalNonInfantryAmount().ToString();
            unitUI.transform.Find("SelfContent").Find("InfAmount").GetComponent<TextMeshProUGUI>().text = refUnit.GetTotalInfantryAmount().ToString();
            UpdateSquadUI(unitUI, refUnit);
            int squadCount = refUnit._Squads.Count;
            unitUI.GetComponent<RectTransform>().sizeDelta = new Vector2(unitUI.GetComponent<RectTransform>().sizeDelta.x, 50f + squadCount * 30f);

            unitUI.transform.Find("SelfContent").Find("DeSelectUnitButton").GetComponent<Button>().onClick.RemoveAllListeners();
            unitUI.transform.Find("SelfContent").Find("DeSelectUnitButton").GetComponent<Button>().onClick.AddListener(SoundManager._Instance.PlayButtonSound);
            unitUI.transform.Find("SelfContent").Find("DeSelectUnitButton").GetComponent<Button>().onClick.AddListener(() => DeSelectUnitButtonClicked(refUnit.gameObject));
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(_ArmyUIContent.GetComponent<RectTransform>());
    }

    private void UpdateSquadUI(Transform unitUI, Unit refUnit)
    {
        int squadUICount = unitUI.transform.Find("Squads").childCount;
        if (refUnit._Squads.Count > squadUICount)
        {
            for (int i = 0; i < (refUnit._Squads.Count - squadUICount); i++)
            {
                Instantiate(GameManager._Instance._SquadUIPrefab, unitUI.transform.Find("Squads"));
            }

        }
        else if (refUnit._Squads.Count < squadUICount)
        {
            for (int i = 0; i < squadUICount - refUnit._Squads.Count; i++)
            {
                unitUI.transform.Find("Squads").GetChild(i).localEulerAngles = new Vector3(unitUI.transform.Find("Squads").GetChild(i).localEulerAngles.x, -1f, unitUI.transform.Find("Squads").GetChild(i).localEulerAngles.z);
                Destroy(unitUI.transform.Find("Squads").GetChild(i).gameObject);
            }
        }

        if (refUnit._Squads.Count == 0) return;

        squadUICount = unitUI.transform.Find("Squads").childCount;
        Transform squadUI = null;
        Squad squad = null;
        int indexForDestroyingUI = 0;
        for (int i = 0; i < squadUICount; i++)
        {
            squadUI = unitUI.transform.Find("Squads").GetChild(i);
            squad = refUnit._Squads[i - indexForDestroyingUI];

            if (squadUI.localEulerAngles.y != 0f)
            {
                indexForDestroyingUI++;
                continue;
            }

            squadUI.GetComponent<SquadRefForUI>()._SquadReferance = squad;

            if (squad._IsSquadSelected)
                squadUI.GetComponent<Image>().color = _SelectedSquadColor;
            else
                squadUI.GetComponent<Image>().color = _NotSelectedSquadColor;

            squadUI.Find("Icon").GetComponent<Image>().sprite = squad._Icon;
            squadUI.Find("Amount").GetComponent<TextMeshProUGUI>().text = squad._Amount.ToString();
            squadUI.Find("Name").GetComponent<TextMeshProUGUI>().text = squad._DisplayName;

            if (squad is Infantry && (squad as Infantry)._IsUsingTrucks)
                squadUI.Find("Icon").GetComponent<Image>().sprite = GameManager._Instance._InfantryAttachedToTrucksIcon;
        }
    }

    private void ArrangeCityUI()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Physics.Raycast(Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue()), out RaycastHit hit, 30000f, LayerMask.GetMask("UpperTerrain"));
        if (hit.collider != null && hit.collider.GetComponent<City>() != null && !hit.collider.GetComponent<City>()._IsEnemy)
        {
            //hover
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (!GameManager._Instance._InGameScreen.transform.Find("OtherInGameMenus").Find("Left").Find("City").gameObject.activeInHierarchy)
                {
                    OpenCityScreen();
                }
            }

        }
        else
        {
            //remove hover
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (GameManager._Instance._InGameScreen.transform.Find("OtherInGameMenus").Find("Left").Find("City").gameObject.activeInHierarchy)
                    CloseAllInGameOtherUI();
            }
        }
    }

    public void OpenArmyScreen()
    {
        if (GameManager._Instance._ConstructionScreen.activeInHierarchy) return;

        CloseAllInGameOtherUI();

        GameManager._Instance._InGameScreen.transform.Find("OtherInGameMenus").gameObject.SetActive(true);
        GameManager._Instance._InGameScreen.transform.Find("OtherInGameMenus").Find("Left").Find("Army").gameObject.SetActive(true);
        GameManager._Instance._InGameScreen.transform.Find("OtherInGameMenus").Find("Right").Find("ArmyDetails").gameObject.SetActive(true);

        UpdateUI();
    }
    public void OpenCityScreen()
    {
        if (GameManager._Instance._ConstructionScreen.activeInHierarchy) return;

        CloseAllInGameOtherUI();

        GameManager._Instance._InGameScreen.transform.Find("OtherInGameMenus").gameObject.SetActive(true);
        GameManager._Instance._InGameScreen.transform.Find("OtherInGameMenus").Find("Left").Find("City").gameObject.SetActive(true);
        GameManager._Instance._InGameScreen.transform.Find("OtherInGameMenus").Find("Right").Find("CityDetails").gameObject.SetActive(true);
        UpdateUI();
    }


    public void CloseAllInGameOtherUI()
    {
        foreach (Transform otherMenus in GameManager._Instance._InGameScreen.transform.Find("OtherInGameMenus").Find("Left"))
        {
            otherMenus.gameObject.SetActive(false);
        }
        foreach (Transform otherMenus in GameManager._Instance._InGameScreen.transform.Find("OtherInGameMenus").Find("Right"))
        {
            otherMenus.gameObject.SetActive(false);
        }
        GameManager._Instance._InGameScreen.transform.Find("OtherInGameMenus").gameObject.SetActive(false);
    }

    private void ArrangeRouteGhost()
    {
        foreach (var obj in _SelectedUnits)
        {
            obj.GetComponent<Unit>().ArrangeCurrentRouteGhost();

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                obj.transform.Find("PotentialRouteGhost").gameObject.SetActive(false);
            else if (_CurrentPlayerOrder is MoveOrder)
                obj.transform.Find("PotentialRouteGhost").gameObject.SetActive(true);
            else
                obj.transform.Find("PotentialRouteGhost").gameObject.SetActive(false);

            _CurrentPlayerOrder.ArrangeOrderGhostForPlayer(obj, obj.transform.position, _CurrentPlayerOrder._OrderPosition, isPressingShiftForMoveOrder: GameManager._Instance._InputActions.FindAction("Sprint").ReadValue<float>() != 0f || GameManager._Instance._InputActions.FindAction("Alt").ReadValue<float>() != 0f);
        }
    }
    private void ArrangeOrderToSelectedUnits()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Physics.Raycast(Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue()), out RaycastHit hit, 30000f, GameManager._Instance._TerrainAndWaterLayers);
        if (hit.collider != null)
            _CurrentPlayerOrder._OrderPosition = GameManager._Instance._InputActions.FindAction("Alt").ReadValue<float>() == 0 ? hit.point : TerrainController._Instance.GetClosestRoadKnot(TerrainController._Instance.GetTerrainPointFromMouse()._Position);
        else
            return;

        if (!Mouse.current.rightButton.wasPressedThisFrame) return;

        foreach (var obj in _SelectedUnits)
        {
            _CurrentPlayerOrder.ExecuteOrder(obj, isClearingForMoveOrder: GameManager._Instance._InputActions.FindAction("Sprint").ReadValue<float>() == 0f && GameManager._Instance._InputActions.FindAction("Alt").ReadValue<float>() == 0f);
        }
    }
    private void ArrangeSelectedUnits()
    {
        if (GameManager._Instance._ConstructionScreen.activeInHierarchy)
        {
            if (_SelectedUnits.Count > 0)
                _SelectedUnits.ClearSelected();
            return;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Physics.Raycast(Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue()), out RaycastHit hit, 30000f, LayerMask.GetMask("Unit"));
        if (hit.collider != null && hit.collider.transform.parent.GetComponent<Unit>() != null && !hit.collider.transform.parent.GetComponent<Unit>()._IsEnemy)
        {
            //hover
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (GameManager._Instance._InputActions.FindAction("Sprint").ReadValue<float>() == 0f || (_SelectedUnits.Count > 0 && _SelectedUnits[0].GetComponent<Unit>()._IsNaval != hit.collider.transform.parent.GetComponent<Unit>()._IsNaval))
                    _SelectedUnits.ClearSelected();

                if (_SelectedUnits.Contains(hit.collider.transform.parent.gameObject))
                {
                    RemoveSelectedUnit(hit.collider.transform.parent.gameObject);
                    if (_SelectedUnits.Count == 0)
                        CloseAllInGameOtherUI();
                }
                else
                {
                    _SelectedUnits.Add(hit.collider.transform.parent.gameObject);
                    OpenArmyScreen();
                }
            }
        }
        else
        {
            //remove hover
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                _SelectedUnits.ClearSelected();
                if (GameManager._Instance._InGameScreen.transform.Find("OtherInGameMenus").Find("Left").Find("Army").gameObject.activeInHierarchy)
                    CloseAllInGameOtherUI();
            }
        }
    }

    public GameObject GetNearestShip(Transform checkerTransform)
    {
        Collider[] hits = Physics.OverlapSphere(checkerTransform.position, 50f);
        GameObject closestShip = null;
        float minDistance = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("NavalUnit"))
            {
                float distance = Vector3.Distance(checkerTransform.position, hit.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestShip = hit.gameObject;
                }
            }
        }

        if (closestShip == null)
            Debug.LogError("Closest Ship not found");
        return closestShip;
    }
    public Vector3 GetNearestTerrainPosition(Vector3 origin)
    {
        for (float radius = 5f; radius <= 60f; radius += 5f)
        {
            float angleStep = 360f / 36;

            for (int i = 0; i < 36; i++)
            {
                float angle = i * angleStep;
                float rad = angle * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(rad), 0, Mathf.Sin(rad)) * radius;
                Vector3 checkPos = origin + offset + Vector3.up * 200f;

                Physics.Raycast(checkPos, Vector3.down, out RaycastHit hit, 200f * 2, GameManager._Instance._TerrainAndWaterLayers);
                if (hit.collider != null && hit.collider.gameObject.layer == LayerMask.NameToLayer("Terrain"))
                    return hit.point;
            }
        }

        return Vector3.negativeInfinity;
    }

    #region InstantOrders

    public void ActivateStopOrder()
    {
        foreach (var obj in _SelectedUnits)
        {
            obj.GetComponent<Unit>()._TargetPositions.Clear();
        }
    }

    private void SelectSquadUIOnlyThisType(Squad squad)
    {
        if (GameManager._Instance._InputActions.FindAction("Sprint").ReadValue<float>() == 0f)
            GameInputController._Instance._SelectedSquads.ClearSelected();


        var squadRefs = FindObjectsByType<SquadRefForUI>(FindObjectsSortMode.None);
        foreach (var item in squadRefs)
        {
            if (item._SquadReferance.GetType() == squad.GetType())
                SelectSquad(item._SquadReferance);
        }
        UpdateUI();
    }
    private void SelectAllSquads()
    {
        if (_SelectedUnits.Count == 0) return;

        var squadRefs = FindObjectsByType<SquadRefForUI>(FindObjectsSortMode.None);
        foreach (var item in squadRefs)
        {
            SelectSquad(item._SquadReferance);
        }
        UpdateUI();
    }
    #endregion
}