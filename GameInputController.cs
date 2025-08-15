using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class GameInputController : MonoBehaviour
{
    public Color _NotSelectedSquadColor;
    public Color _SelectedSquadColor;

    public Color _NotSelectedCityColor;
    public Color _HoverCityColor;
    public Color _SelectedCityColor;

    public Color _NotSelectedUnitColor;
    public Color _HoverUnitColor;
    public Color _SelectedUnitColor;

    public static GameInputController _Instance;
    public List<GameObject> _SelectedUnits { get; private set; }
    public List<Squad> _SelectedSquads { get; private set; }
    public bool _IsOrderTypeAll { get; private set; }
    public Order _CurrentPlayerOrder { get; set; }
    public Transform _ArmyUIContent { get; private set; }
    public Transform _OrderUITransform { get; private set; }

    public float _SplitPercent;


    private GameObject _lastSelectedCityObj;
    private GameObject _lastHoveringObj;

    private float _updateUICounter;
    List<GameObject> _unitsWillBeOrdered;

    private void Awake()
    {
        _Instance = this;
        _unitsWillBeOrdered = new List<GameObject>();
        _SelectedUnits = new List<GameObject>();
        _SelectedSquads = new List<Squad>();
        _CurrentPlayerOrder = new MoveOrder();
        _ArmyUIContent = GameObject.Find("InGameScreen").transform.Find("OtherInGameMenus").Find("Left").Find("Army").Find("ScrollView").Find("Viewport").Find("Content");
        _OrderUITransform = GameManager._Instance._InGameScreen.transform.Find("OtherInGameMenus").Find("Left").Find("Army").Find("OrderUI");
        _SplitPercent = 0.5f;
        _OrderUITransform.Find("SplitUnitSlider").GetComponent<Slider>().value = _SplitPercent;
        _OrderUITransform.Find("SplitUnitSlider").GetComponent<Slider>().onValueChanged.AddListener((float newValue) => { _SplitPercent = newValue; UpdateUI(); });
        _IsOrderTypeAll = true;
    }
    private void Update()
    {
        if (GameManager._Instance._IsGameStopped) return;

        if (_IsOrderTypeAll)
            _unitsWillBeOrdered = _SelectedUnits;
        else
        {
            _unitsWillBeOrdered = new List<GameObject>();

            foreach (var selectedSquad in _SelectedSquads)
            {
                if (!_unitsWillBeOrdered.Contains(selectedSquad._AttachedUnit.gameObject))
                    _unitsWillBeOrdered.Add(selectedSquad._AttachedUnit.gameObject);
            }
        }

        ArrangeSquadSelectionUI();
        ArrangeRouteGhost(_unitsWillBeOrdered);

        ArrangeSelectedUnits();
        ArrangeCurrentOrderToSelectedUnits(_unitsWillBeOrdered);

        ArrangeCityUI();
        ArrangeOrderUIButtons();

        _updateUICounter += Time.deltaTime;
        if (_updateUICounter > 0.2f)
            UpdateUI();
    }

    public void GetOnTruckButtonClicked()
    {
        foreach (Squad squad in _SelectedSquads)
        {
            if (squad is Infantry)
                new GetOnTruckOrder().ExecuteOrder(squad);
            else if (squad is Artillery || squad is AntiTank || squad is AntiAir)
                new GetTowedOrder().ExecuteOrder(squad);
        }
    }
    public void GetOffTruckButtonClicked()
    {
        foreach (Squad squad in _SelectedSquads)
        {
            if (squad is Infantry)
                new GetOffTruckOrder().ExecuteOrder(squad);
            else if (squad is Artillery || squad is AntiTank || squad is AntiAir)
                new GetTowedOffOrder().ExecuteOrder(squad);
        }
    }
    public void SelectAllSquadsButtonClicked()
    {
        if (_SelectedUnits.Count == 0) return;

        var squadRefs = FindObjectsByType<SquadRefForUI>(FindObjectsSortMode.None);
        foreach (var item in squadRefs)
        {
            SelectSquad(item._SquadReferance);
        }
        UpdateUI();
    }
    public void OrderTypeButtonClicked()
    {
        _IsOrderTypeAll = !_IsOrderTypeAll;
    }

    public void DeSelectUnitButtonClicked(GameObject deSelectingObj)
    {
        DeSelectUnit(deSelectingObj);

        UpdateUI();
    }
    public void ToNewUnitButtonClicked()
    {
        if (_SelectedSquads.Count == 0) return;
        if (!IsAllSelectedSquadsCloseToEachother()) return;

        List<Squad> squadsWillTryToAttachTruck = new List<Squad>();
        Unit newUnitComponent = GameManager._Instance.CreateUnit(_SelectedSquads[0]._AttachedUnit.transform.position + new Vector3(Random.Range(-0.01f, 0.01f), 0f, Random.Range(-0.01f, 0.01f)), _SelectedSquads[0]._AttachedUnit.GetComponent<Unit>()._IsNaval).GetComponent<Unit>();
        foreach (var selectedSquad in _SelectedSquads)
        {
            if (selectedSquad is ICarryUnit)
            {
                List<Transform> carryingUnits = new List<Transform>();
                foreach (Transform item in selectedSquad._AttachedUnit.transform.Find("CarryingUnits"))
                {
                    carryingUnits.Add(item);
                }
                foreach (Transform carryingUnit in carryingUnits)
                {
                    if (newUnitComponent.transform.Find("CarryingUnits").childCount > 0)
                    {
                        newUnitComponent.transform.Find("CarryingUnits").GetChild(0).GetComponent<Unit>().MergeWithAnotherUnit(carryingUnit.GetComponent<Unit>());
                    }
                    else
                    {
                        carryingUnit.SetParent(newUnitComponent.transform.Find("CarryingUnits"));
                        carryingUnit.GetComponent<Unit>()._AttachedToUnitObject = newUnitComponent.gameObject;
                        carryingUnit.transform.position = newUnitComponent.transform.position;
                    }
                }
            }

            if (selectedSquad._AttachedUnit != null && selectedSquad._AttachedUnit._Squads.Contains(selectedSquad))
                selectedSquad._AttachedUnit.RemoveSquad(selectedSquad);

            ArrangeDetachFromTrucks(selectedSquad, squadsWillTryToAttachTruck);

            selectedSquad._AttachedUnit = newUnitComponent;
            newUnitComponent.AddSquad(selectedSquad);
        }

        SelectUnit(newUnitComponent.gameObject);

        foreach (Squad tryToAttachSquad in squadsWillTryToAttachTruck)
        {
            TryToAttachSquadToTruck(tryToAttachSquad);
        }

        _SelectedSquads.ClearSelected();
        foreach (Squad squad in newUnitComponent._Squads)
        {
            SelectSquad(squad);
        }

        UpdateUI();
    }
    public void SplitUnitButtonClicked()
    {
        if (_SelectedSquads.Count == 0) return;
        if (!IsAllSelectedSquadsCloseToEachother()) return;

        List<Squad> squadsWillTryToAttachTruck = new List<Squad>();
        Unit newUnitComponent = GameManager._Instance.CreateUnit(_SelectedSquads[0]._AttachedUnit.transform.position, _SelectedSquads[0]._AttachedUnit.GetComponent<Unit>()._IsNaval).GetComponent<Unit>();
        List<Unit> selectedSquadsUnits = new List<Unit>();
        foreach (var selectedSquad in _SelectedSquads)
        {
            if (selectedSquad._Amount <= 1) continue;

            Squad newSquad = selectedSquad.CreateNewSquadObject(newUnitComponent);

            newSquad._Amount = (int)(selectedSquad._Amount * _SplitPercent);
            float actualPercentForThisSquad = (newSquad._Amount * 1f) / (selectedSquad._Amount * 1f);
            selectedSquad._Amount -= newSquad._Amount;

            if (!selectedSquadsUnits.Contains(selectedSquad._AttachedUnit))
            {
                selectedSquadsUnits.Add(selectedSquad._AttachedUnit);
                newUnitComponent.AddStorage(selectedSquad._AttachedUnit.SplitStorage(_SplitPercent));
                newUnitComponent._Name = "New :" + selectedSquad._AttachedUnit._Name;
            }

            if (newSquad._Amount > 0)
                newUnitComponent.AddSquad(newSquad);
            else
                newSquad = null;

            ArrangeCarryingWhenSplit(selectedSquad, newSquad, newUnitComponent, actualPercentForThisSquad);
            ArrangeDetachFromTrucks(selectedSquad, squadsWillTryToAttachTruck, newSquad);
        }
        if (newUnitComponent._Squads.Count > 0)
            SelectUnit(newUnitComponent.gameObject);
        else
        {
            Destroy(newUnitComponent.gameObject);
        }

        foreach (Squad tryToAttachSquad in squadsWillTryToAttachTruck)
        {
            TryToAttachSquadToTruck(tryToAttachSquad);
        }

        _SelectedSquads.ClearSelected();
        foreach (Squad squad in newUnitComponent._Squads)
        {
            SelectSquad(squad);
        }

        UpdateUI();
    }
    private void ArrangeCarryingWhenSplit(Squad selectedSquad, Squad newSquad, Unit newUnitComponent, float splitPercent)
    {
        if (newSquad == null) return;
        if (newSquad._Amount > 0 && selectedSquad is ICarryUnit && (selectedSquad as ICarryUnit)._CurrentCarry != 0)
        {
            int carryTransferAmount = 0;
            List<Transform> carryingUnits = new List<Transform>();
            foreach (Transform item in selectedSquad._AttachedUnit.transform.Find("CarryingUnits"))
            {
                carryingUnits.Add(item);
            }
            foreach (Transform carryingUnit in carryingUnits)
            {
                Unit carryingNewUnitComponent = GameManager._Instance.CreateUnit(carryingUnit.transform.position, carryingUnit.GetComponent<Unit>()._IsNaval).GetComponent<Unit>();
                carryingNewUnitComponent._AttachedToUnitObject = newUnitComponent.gameObject;
                carryingNewUnitComponent.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
                carryingNewUnitComponent.transform.SetParent(newUnitComponent.transform.Find("CarryingUnits"));
                carryingNewUnitComponent.transform.position = newUnitComponent.transform.position;
                carryingNewUnitComponent.gameObject.SetActive(false);

                foreach (Squad carryingUnitSquad in carryingUnit.GetComponent<Unit>()._Squads)
                {
                    Squad newSquadForCarry = carryingUnitSquad.CreateNewSquadObject(carryingNewUnitComponent);

                    newSquadForCarry._Amount = (int)(carryingUnitSquad._Amount * splitPercent);
                    carryingUnitSquad._Amount -= newSquadForCarry._Amount;

                    if (newSquadForCarry._Amount > 0)
                        carryingNewUnitComponent.AddSquad(newSquadForCarry);

                    carryTransferAmount += newSquadForCarry._Weight;
                }
            }

            (selectedSquad as ICarryUnit)._CurrentCarry -= carryTransferAmount;
            (newSquad as ICarryUnit)._CurrentCarry += carryTransferAmount;
        }
    }
    private void ArrangeDetachFromTrucks(Squad selectedSquad, List<Squad> squadsWillTryToAttachTruck, Squad newSquad = null)
    {
        if (selectedSquad is Infantry && (selectedSquad as Infantry)._AttachedTruck != null)
        {
            if (newSquad != null)
            {
                squadsWillTryToAttachTruck.Add(newSquad);
                (selectedSquad as Infantry)._AttachedTruck._CurrentManCarry -= newSquad._Weight;
            }

            (selectedSquad as Infantry)._AttachedTruck._CurrentManCarry -= selectedSquad._Weight;
            (selectedSquad as Infantry)._AttachedTruck = null;
            squadsWillTryToAttachTruck.Add(selectedSquad);
        }
        else if (selectedSquad is Artillery && (selectedSquad as Artillery)._TowedTo != null)
        {
            if (newSquad != null)
            {
                squadsWillTryToAttachTruck.Add(newSquad);
                (selectedSquad as Artillery)._TowedTo._CurrentTow -= newSquad._Weight;
            }
            squadsWillTryToAttachTruck.Add(selectedSquad);
            (selectedSquad as Artillery)._TowedTo._CurrentTow -= selectedSquad._Weight;
            (selectedSquad as Artillery)._TowedTo = null;
        }
        else if (selectedSquad is AntiTank && (selectedSquad as AntiTank)._TowedTo != null)
        {
            if (newSquad != null)
            {
                squadsWillTryToAttachTruck.Add(newSquad);
                (selectedSquad as Artillery)._TowedTo._CurrentTow -= newSquad._Weight;
            }
            squadsWillTryToAttachTruck.Add(selectedSquad);
            (selectedSquad as AntiTank)._TowedTo._CurrentTow -= selectedSquad._Weight;
            (selectedSquad as AntiTank)._TowedTo = null;
        }
        else if (selectedSquad is AntiAir && (selectedSquad as AntiAir)._TowedTo != null)
        {
            if (newSquad != null)
            {
                squadsWillTryToAttachTruck.Add(newSquad);
                (selectedSquad as Artillery)._TowedTo._CurrentTow -= newSquad._Weight;
            }
            squadsWillTryToAttachTruck.Add(selectedSquad);
            (selectedSquad as AntiAir)._TowedTo._CurrentTow -= selectedSquad._Weight;
            (selectedSquad as AntiAir)._TowedTo = null;
        }
    }
    private void TryToAttachSquadToTruck(Squad squad)
    {
        if (squad == null || squad._AttachedUnit == null || !IsGetOnTruckPossible(squad)) return;

        if (squad is Infantry)
            new GetOnTruckOrder().ExecuteOrder(squad);
        else if (squad is Artillery || squad is AntiTank || squad is AntiAir)
            new GetTowedOrder().ExecuteOrder(squad);
    }

    public void StopOrderButtonClicked()
    {
        foreach (var obj in _unitsWillBeOrdered)
        {
            obj.GetComponent<Unit>()._TargetPositions.Clear();
        }
    }

    private void SelectOnlyThisTypeSquadsButtonClicked(Squad squad)
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
    public void GetOnShipButtonClicked(Unit unit)
    {
        new GetOnShipOrder().ExecuteOrder(unit.gameObject);
    }
    public void GetOnTrainButtonClicked(Unit unit)
    {
        new GetOnTrainOrder().ExecuteOrder(unit.gameObject);
    }
    public void GetOnCargoPlaneButtonClicked(Unit unit)
    {
        new GetOnCargoPlaneOrder().ExecuteOrder(unit.gameObject);
    }
    public void EvacuateButtonClicked(Unit unit)
    {
        if (unit._IsNaval)
            new EvacuateShipOrder().ExecuteOrder(unit.gameObject);
        else if (unit._IsTrain)
            new EvacuateTrainOrder().ExecuteOrder(unit.gameObject);
        else if (unit._IsAir)
            new EvacuateCargoPlaneOrder().ExecuteOrder(unit.gameObject);
    }

    public void SelectUnit(GameObject unit)
    {
        if (_SelectedUnits.Contains(unit)) return;

        _lastHoveringObj = null;
        unit.transform.Find("UnitUI").Find("SelectedUI").GetComponent<Image>().color = _SelectedUnitColor;
        unit.transform.Find("UnitUI").Find("NameText").GetComponent<TextMeshProUGUI>().text = unit.GetComponent<Unit>()._Name;

        if (unit.transform.Find("CurrentRouteGhost").GetComponent<LineRenderer>().colorGradient != GameManager._Instance._SelectedGradientForCurrentRoute)
            unit.transform.Find("CurrentRouteGhost").GetComponent<LineRenderer>().colorGradient = GameManager._Instance._SelectedGradientForCurrentRoute;

        _SelectedUnits.Add(unit);
    }
    public void DeSelectUnit(GameObject unit)
    {
        if (!_SelectedUnits.Contains(unit)) return;

        List<Squad> squadsWillBeDeselected = new List<Squad>();
        foreach (var selectedSquad in _SelectedSquads)
        {
            if (selectedSquad._AttachedUnit == unit.GetComponent<Unit>())
                squadsWillBeDeselected.Add(selectedSquad);
        }
        foreach (var squad in squadsWillBeDeselected)
        {
            DeSelectSquad(squad);
        }

        unit.transform.Find("UnitUI").Find("NameText").GetComponent<TextMeshProUGUI>().text = "";
        unit.transform.Find("UnitUI").Find("SelectedUI").GetComponent<Image>().color = _NotSelectedUnitColor;
        unit.transform.Find("PotentialRouteGhost").gameObject.SetActive(false);

        if (unit.transform.Find("CurrentRouteGhost").GetComponent<LineRenderer>().colorGradient != GameManager._Instance._NotSelectedGradientForCurrentRoute)
            unit.transform.Find("CurrentRouteGhost").GetComponent<LineRenderer>().colorGradient = GameManager._Instance._NotSelectedGradientForCurrentRoute;
        //unit.transform.Find("CurrentRouteGhost").gameObject.SetActive(false);

        _SelectedUnits.Remove(unit);
    }
    public void SelectSquad(Squad squad)
    {
        if (GameInputController._Instance._SelectedSquads.Contains(squad)) return;
        if (squad._AttachedUnit._AttachedToUnitObject != null) return;

        GameInputController._Instance._SelectedSquads.Add(squad);
        squad._IsSquadSelected = true;
    }
    public void DeSelectSquad(Squad squad)
    {
        if (!GameInputController._Instance._SelectedSquads.Contains(squad)) return;

        GameInputController._Instance._SelectedSquads.Remove(squad);
        squad._IsSquadSelected = false;
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

        if (_unitsWillBeOrdered.Count > 0 && !_OrderUITransform.Find("StopOrderButton").GetComponent<Button>().interactable)
        {
            _OrderUITransform.Find("StopOrderButton").GetComponent<Button>().interactable = true;
        }
        else if (_unitsWillBeOrdered.Count == 0 && _OrderUITransform.Find("StopOrderButton").GetComponent<Button>().interactable)
        {
            _OrderUITransform.Find("StopOrderButton").GetComponent<Button>().interactable = false;
        }

        if (_SelectedSquads.Count > 0 && CanAnySelectedSquadAttachToTruck())
        {
            if (!_OrderUITransform.Find("GetOnTruckButton").GetComponent<Button>().interactable)
                _OrderUITransform.Find("GetOnTruckButton").GetComponent<Button>().interactable = true;
        }
        else
        {
            if (_OrderUITransform.Find("GetOnTruckButton").GetComponent<Button>().interactable)
                _OrderUITransform.Find("GetOnTruckButton").GetComponent<Button>().interactable = false;
        }

        if (_SelectedSquads.Count > 0 && CanAnySelectedSquadDetachFromTruck())
        {
            if (!_OrderUITransform.Find("GetOffTruckButton").GetComponent<Button>().interactable)
                _OrderUITransform.Find("GetOffTruckButton").GetComponent<Button>().interactable = true;
        }
        else
        {
            if (_OrderUITransform.Find("GetOffTruckButton").GetComponent<Button>().interactable)
                _OrderUITransform.Find("GetOffTruckButton").GetComponent<Button>().interactable = false;
        }
    }
    private bool CanAnySelectedSquadAttachToTruck()
    {
        foreach (Squad squad in _SelectedSquads)
        {
            if (squad is Infantry && (squad as Infantry)._AttachedTruck == null && IsGetOnTruckPossible(squad))
                return true;
            if (squad is Artillery && (squad as Artillery)._TowedTo == null && IsGetOnTruckPossible(squad))
                return true;
            if (squad is AntiTank && (squad as AntiTank)._TowedTo == null && IsGetOnTruckPossible(squad))
                return true;
            if (squad is AntiAir && (squad as AntiAir)._TowedTo == null && IsGetOnTruckPossible(squad))
                return true;
        }
        return false;
    }
    private bool CanAnySelectedSquadDetachFromTruck()
    {
        foreach (Squad squad in _SelectedSquads)
        {
            if (squad is Infantry && (squad as Infantry)._AttachedTruck != null)
                return true;
            if (squad is Artillery && (squad as Artillery)._TowedTo != null)
                return true;
            if (squad is AntiTank && (squad as AntiTank)._TowedTo != null)
                return true;
            if (squad is AntiAir && (squad as AntiAir)._TowedTo != null)
                return true;
        }
        return false;
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
        _OrderUITransform.Find("OrderTypeButton").Find("Text (TMP)").GetComponent<TextMeshProUGUI>().text = Localization._Instance._UI[_IsOrderTypeAll ? 50 : 51];

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
            unitUI.transform.Find("SelfContent").Find("CarryCapacity").GetComponent<TextMeshProUGUI>().text = refUnit._CanCarryAnotherUnit ? refUnit.GetCurrentCarry().ToString() + "/" + refUnit.GetCarryingCapacity().ToString() : "";

            List<Squad> carryingSquads = new List<Squad>();
            for (int t = 0; t < refUnit.transform.Find("CarryingUnits").childCount; t++)
            {
                foreach (Squad squad in refUnit.transform.Find("CarryingUnits").GetChild(t).GetComponent<Unit>()._Squads)
                {
                    carryingSquads.Add(squad);
                }
            }
            UpdateSquadUI(unitUI, refUnit, carryingSquads);
            int squadCount = refUnit._Squads.Count + carryingSquads.Count;
            unitUI.GetComponent<RectTransform>().sizeDelta = new Vector2(unitUI.GetComponent<RectTransform>().sizeDelta.x, 50f + squadCount * 30f);

            unitUI.transform.Find("SelfContent").Find("DeSelectUnitButton").GetComponent<Button>().onClick.RemoveAllListeners();
            unitUI.transform.Find("SelfContent").Find("DeSelectUnitButton").GetComponent<Button>().onClick.AddListener(SoundManager._Instance.PlayButtonSound);
            unitUI.transform.Find("SelfContent").Find("DeSelectUnitButton").GetComponent<Button>().onClick.AddListener(() => DeSelectUnitButtonClicked(refUnit.gameObject));

            ArrangeCarryButtonsUI(unitUI, refUnit);
        }

        List<Transform> unitUIList = new List<Transform>();
        foreach (Transform item in _ArmyUIContent.transform)
        {
            unitUIList.Add(item);
        }
        foreach (var item in unitUIList)
        {
            if (item.GetComponent<UnitRefForUI>()._UnitReferance._CarryWeight == 0)
                Destroy(item.gameObject);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(_ArmyUIContent.GetComponent<RectTransform>());
    }

    private void UpdateSquadUI(Transform unitUI, Unit refUnit, List<Squad> carryingSquads)
    {
        int allSquadCount = refUnit._Squads.Count + carryingSquads.Count;
        int squadUICount = unitUI.transform.Find("Squads").childCount;
        if (allSquadCount > squadUICount)
        {
            for (int i = 0; i < (allSquadCount - squadUICount); i++)
            {
                Instantiate(GameManager._Instance._SquadUIPrefab, unitUI.transform.Find("Squads"));
            }

        }
        else if (allSquadCount < squadUICount)
        {
            for (int i = 0; i < squadUICount - allSquadCount; i++)
            {
                unitUI.transform.Find("Squads").GetChild(i).localEulerAngles = new Vector3(unitUI.transform.Find("Squads").GetChild(i).localEulerAngles.x, -1f, unitUI.transform.Find("Squads").GetChild(i).localEulerAngles.z);
                Destroy(unitUI.transform.Find("Squads").GetChild(i).gameObject);
            }
        }

        if (allSquadCount == 0) return;

        squadUICount = unitUI.transform.Find("Squads").childCount;
        Transform squadUI = null;
        int indexForDestroyingUI = 0;
        for (int i = 0; i < squadUICount; i++)
        {
            squadUI = unitUI.transform.Find("Squads").GetChild(i);
            if (squadUI.localEulerAngles.y != 0f)
            {
                indexForDestroyingUI++;
                continue;
            }
            Squad squad = (refUnit._Squads.Count > i - indexForDestroyingUI) ? refUnit._Squads[i - indexForDestroyingUI] : carryingSquads[i - refUnit._Squads.Count - indexForDestroyingUI];

            squadUI.GetComponent<SquadRefForUI>()._SquadReferance = squad;

            if (squad._IsSquadSelected)
                squadUI.GetComponent<Image>().color = _SelectedSquadColor;
            else
                squadUI.GetComponent<Image>().color = _NotSelectedSquadColor;


            squadUI.Find("Icon").GetComponent<Button>().onClick.RemoveAllListeners();
            squadUI.Find("Icon").GetComponent<Button>().onClick.AddListener(() => SelectOnlyThisTypeSquadsButtonClicked(squad));
            squadUI.Find("Icon").GetComponent<Button>().onClick.AddListener(SoundManager._Instance.PlayButtonSound);


            squadUI.Find("Icon").GetComponent<Image>().sprite = squad._Icon;
            squadUI.Find("Amount").GetComponent<TextMeshProUGUI>().text = squad._Amount.ToString();
            squadUI.Find("Name").GetComponent<TextMeshProUGUI>().text = squad._DisplayName;


            if (squad is Infantry && (squad as Infantry)._AttachedTruck != null)
                squadUI.Find("Icon").GetComponent<Image>().sprite = GameManager._Instance._InfantryAttachedToTrucksIcon;
            else if (squad is Artillery && (squad as Artillery)._TowedTo != null)
                squadUI.Find("Icon").GetComponent<Image>().sprite = GameManager._Instance._ArtilleryTowedIcon;
            else if (squad is AntiTank && (squad as AntiTank)._TowedTo != null)
                squadUI.Find("Icon").GetComponent<Image>().sprite = GameManager._Instance._AntiTankTowedIcon;
            else if (squad is AntiAir && (squad as AntiAir)._TowedTo != null)
                squadUI.Find("Icon").GetComponent<Image>().sprite = GameManager._Instance._AntiAirTowedIcon;

            if (squad._IsSquadSelected && IsHoveringSplit())
                squadUI.Find("Amount").GetComponent<TextMeshProUGUI>().text = squad._Amount.ToString() + " (-" + ((int)(squad._Amount * _SplitPercent)).ToString() + ")";

            if (refUnit._Squads.Count <= i - indexForDestroyingUI)
            {
                //carrying ui setup
                squadUI.GetComponent<Image>().color = new Color(0.45f, 0.45f, 1f);
                squadUI.Find("Icon").GetComponent<Button>().interactable = false;
            }

        }
    }
    private bool IsHoveringSplit()
    {
        bool _isMouseOverSplitButton = RectTransformUtility.RectangleContainsScreenPoint(_OrderUITransform.Find("SplitUnitButton").GetComponent<RectTransform>(), Input.mousePosition, null);
        bool _isMouseOverSplitSlider = RectTransformUtility.RectangleContainsScreenPoint(_OrderUITransform.Find("SplitUnitSlider").GetComponent<RectTransform>(), Input.mousePosition, null);
        return _isMouseOverSplitButton || _isMouseOverSplitSlider;
    }
    private void ArrangeCarryButtonsUI(Transform unitUI, Unit refUnit)
    {
        if (refUnit._CanCarryAnotherUnit)
        {
            if (unitUI.transform.Find("SelfContent").Find("GetOnShipButton").gameObject.activeSelf)
                unitUI.transform.Find("SelfContent").Find("GetOnShipButton").gameObject.SetActive(false);
            if (unitUI.transform.Find("SelfContent").Find("GetOnTrainButton").gameObject.activeSelf)
                unitUI.transform.Find("SelfContent").Find("GetOnTrainButton").gameObject.SetActive(false);
            if (unitUI.transform.Find("SelfContent").Find("GetOnCargoPlaneButton").gameObject.activeSelf)
                unitUI.transform.Find("SelfContent").Find("GetOnCargoPlaneButton").gameObject.SetActive(false);
            if (!unitUI.transform.Find("SelfContent").Find("EvacuateButton").gameObject.activeSelf)
                unitUI.transform.Find("SelfContent").Find("EvacuateButton").gameObject.SetActive(true);

            unitUI.transform.Find("SelfContent").Find("EvacuateButton").GetComponent<Button>().interactable = IsEvacuatePossible(refUnit);
            unitUI.transform.Find("SelfContent").Find("EvacuateButton").GetComponent<Button>().onClick.RemoveAllListeners();
            unitUI.transform.Find("SelfContent").Find("EvacuateButton").GetComponent<Button>().onClick.AddListener(SoundManager._Instance.PlayButtonSound);
            unitUI.transform.Find("SelfContent").Find("EvacuateButton").GetComponent<Button>().onClick.AddListener(() => EvacuateButtonClicked(refUnit));
        }
        else
        {
            if (!unitUI.transform.Find("SelfContent").Find("GetOnShipButton").gameObject.activeSelf)
                unitUI.transform.Find("SelfContent").Find("GetOnShipButton").gameObject.SetActive(true);
            if (!unitUI.transform.Find("SelfContent").Find("GetOnTrainButton").gameObject.activeSelf)
                unitUI.transform.Find("SelfContent").Find("GetOnTrainButton").gameObject.SetActive(true);
            if (!unitUI.transform.Find("SelfContent").Find("GetOnCargoPlaneButton").gameObject.activeSelf)
                unitUI.transform.Find("SelfContent").Find("GetOnCargoPlaneButton").gameObject.SetActive(true);
            if (unitUI.transform.Find("SelfContent").Find("EvacuateButton").gameObject.activeSelf)
                unitUI.transform.Find("SelfContent").Find("EvacuateButton").gameObject.SetActive(false);

            unitUI.transform.Find("SelfContent").Find("GetOnShipButton").GetComponent<Button>().interactable = IsGetOnShipPossible(refUnit.gameObject);
            unitUI.transform.Find("SelfContent").Find("GetOnShipButton").GetComponent<Button>().onClick.RemoveAllListeners();
            unitUI.transform.Find("SelfContent").Find("GetOnShipButton").GetComponent<Button>().onClick.AddListener(SoundManager._Instance.PlayButtonSound);
            unitUI.transform.Find("SelfContent").Find("GetOnShipButton").GetComponent<Button>().onClick.AddListener(() => GetOnShipButtonClicked(refUnit));

            unitUI.transform.Find("SelfContent").Find("GetOnTrainButton").GetComponent<Button>().interactable = IsGetOnTrainPossible(refUnit.gameObject);
            unitUI.transform.Find("SelfContent").Find("GetOnTrainButton").GetComponent<Button>().onClick.RemoveAllListeners();
            unitUI.transform.Find("SelfContent").Find("GetOnTrainButton").GetComponent<Button>().onClick.AddListener(SoundManager._Instance.PlayButtonSound);
            unitUI.transform.Find("SelfContent").Find("GetOnTrainButton").GetComponent<Button>().onClick.AddListener(() => GetOnTrainButtonClicked(refUnit));

            unitUI.transform.Find("SelfContent").Find("GetOnCargoPlaneButton").GetComponent<Button>().interactable = IsGetOnCargoPlanePossible(refUnit.gameObject);
            unitUI.transform.Find("SelfContent").Find("GetOnCargoPlaneButton").GetComponent<Button>().onClick.RemoveAllListeners();
            unitUI.transform.Find("SelfContent").Find("GetOnCargoPlaneButton").GetComponent<Button>().onClick.AddListener(SoundManager._Instance.PlayButtonSound);
            unitUI.transform.Find("SelfContent").Find("GetOnCargoPlaneButton").GetComponent<Button>().onClick.AddListener(() => GetOnCargoPlaneButtonClicked(refUnit));
        }
    }

    private void ArrangeCityUI()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Physics.Raycast(Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue()), out RaycastHit hit, 30000f, LayerMask.GetMask("UpperTerrain"));
        if (hit.collider != null && hit.collider.GetComponent<City>() != null && !hit.collider.GetComponent<City>()._IsEnemy)
        {
            OpenCityHoverInNeed(hit);

            if (Mouse.current.leftButton.wasPressedThisFrame && _lastHoveringObj == hit.collider.gameObject && _SelectedUnits.Count == 0)
            {
                if (!GameManager._Instance._InGameScreen.transform.Find("OtherInGameMenus").Find("Left").Find("City").gameObject.activeInHierarchy)
                {
                    OpenCityScreen(hit.collider.gameObject);
                }
            }

        }
        else
        {
            CloseCityHover();

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (GameManager._Instance._InGameScreen.transform.Find("OtherInGameMenus").Find("Left").Find("City").gameObject.activeInHierarchy)
                    CloseAllInGameOtherUI();
            }
        }
    }
    private void OpenCityHoverInNeed(RaycastHit hit)
    {
        if (_lastSelectedCityObj == null && _lastHoveringObj == null)
        {
            CloseCityHover();
            _lastHoveringObj = hit.collider.gameObject;
            _lastHoveringObj.transform.Find("CityUI").Find("SelectedUI").GetComponent<Image>().color = _HoverCityColor;
        }
    }
    private void CloseCityHover()
    {
        if (_lastHoveringObj != null && _lastHoveringObj.CompareTag("City"))
        {
            _lastHoveringObj.transform.Find("CityUI").Find("SelectedUI").GetComponent<Image>().color = _NotSelectedCityColor;
            _lastHoveringObj = null;
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
    public void OpenCityScreen(GameObject cityObject)
    {
        if (GameManager._Instance._ConstructionScreen.activeInHierarchy) return;

        CloseAllInGameOtherUI();

        _lastSelectedCityObj = cityObject;
        cityObject.transform.Find("CityUI").Find("SelectedUI").GetComponent<Image>().color = _SelectedCityColor;

        GameManager._Instance._InGameScreen.transform.Find("OtherInGameMenus").gameObject.SetActive(true);
        GameManager._Instance._InGameScreen.transform.Find("OtherInGameMenus").Find("Left").Find("City").gameObject.SetActive(true);
        GameManager._Instance._InGameScreen.transform.Find("OtherInGameMenus").Find("Right").Find("CityDetails").gameObject.SetActive(true);
        UpdateUI();
    }


    public void CloseAllInGameOtherUI()
    {
        if (_lastSelectedCityObj != null)
            _lastSelectedCityObj.transform.Find("CityUI").Find("SelectedUI").GetComponent<Image>().color = _NotSelectedCityColor;

        if (_lastHoveringObj != null && _lastHoveringObj.CompareTag("City"))
            _lastHoveringObj.transform.Find("CityUI").Find("SelectedUI").GetComponent<Image>().color = _NotSelectedCityColor;
        else if (_lastHoveringObj != null && _lastHoveringObj.layer == LayerMask.NameToLayer("Unit"))
            _lastHoveringObj.transform.parent.Find("UnitUI").Find("SelectedUI").GetComponent<Image>().color = _NotSelectedUnitColor;

        _lastSelectedCityObj = null;
        _lastHoveringObj = null;

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

    private void ArrangeRouteGhost(List<GameObject> unitsWillBeOrdered)
    {
        foreach (var selectedUnit in _SelectedUnits)
        {
            if (!unitsWillBeOrdered.Contains(selectedUnit))
                selectedUnit.transform.Find("PotentialRouteGhost").gameObject.SetActive(false);
        }
        foreach (var obj in unitsWillBeOrdered)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                obj.transform.Find("PotentialRouteGhost").gameObject.SetActive(false);
            else if (_CurrentPlayerOrder is MoveOrder)
                obj.transform.Find("PotentialRouteGhost").gameObject.SetActive(true);
            else
                obj.transform.Find("PotentialRouteGhost").gameObject.SetActive(false);

            if (_CurrentPlayerOrder is MoveOrder)
                (_CurrentPlayerOrder as MoveOrder).ArrangeOrderGhostForPlayer(obj, obj.transform.position, _CurrentPlayerOrder._OrderPosition, GameManager._Instance._InputActions.FindAction("Sprint").ReadValue<float>() != 0f || GameManager._Instance._InputActions.FindAction("Alt").ReadValue<float>() != 0f);
        }
    }
    private void ArrangeCurrentOrderToSelectedUnits(List<GameObject> unitsWillBeOrdered)
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Physics.Raycast(Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue()), out RaycastHit hit, 30000f, GameManager._Instance._TerrainAndWaterLayers);
        if (hit.collider != null)
            _CurrentPlayerOrder._OrderPosition = GameManager._Instance._InputActions.FindAction("Alt").ReadValue<float>() == 0 ? hit.point : TerrainController._Instance.GetClosestRoadKnot(TerrainController._Instance.GetTerrainPointFromMouse()._Position);
        else
            return;

        if (!Mouse.current.rightButton.wasPressedThisFrame) return;

        foreach (var obj in unitsWillBeOrdered)
        {
            if (_CurrentPlayerOrder is MoveOrder)
                (_CurrentPlayerOrder as MoveOrder).ExecuteOrder(obj, GameManager._Instance._InputActions.FindAction("Sprint").ReadValue<float>() == 0f && GameManager._Instance._InputActions.FindAction("Alt").ReadValue<float>() == 0f);
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
        if (hit.collider != null && hit.collider.transform.parent != null && hit.collider.transform.parent.GetComponent<Unit>() != null && !hit.collider.transform.parent.GetComponent<Unit>()._IsEnemy)
        {
            OpenUnitHoverInNeed(hit);

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (GameManager._Instance._InputActions.FindAction("Sprint").ReadValue<float>() == 0f || (_SelectedUnits.Count > 0 && _SelectedUnits[0].GetComponent<Unit>()._IsNaval != hit.collider.transform.parent.GetComponent<Unit>()._IsNaval))
                    _SelectedUnits.ClearSelected();

                if (_SelectedUnits.Contains(hit.collider.transform.parent.gameObject))
                {
                    DeSelectUnit(hit.collider.transform.parent.gameObject);
                    if (_SelectedUnits.Count == 0)
                        CloseAllInGameOtherUI();
                }
                else
                {
                    SelectUnit(hit.collider.transform.parent.gameObject);
                    OpenArmyScreen();
                }
            }
        }
        else
        {
            CloseUnitHover();

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                _SelectedUnits.ClearSelected();
                if (GameManager._Instance._InGameScreen.transform.Find("OtherInGameMenus").Find("Left").Find("Army").gameObject.activeInHierarchy)
                    CloseAllInGameOtherUI();
            }
        }
    }
    private void OpenUnitHoverInNeed(RaycastHit hit)
    {
        if (!_SelectedUnits.Contains(hit.collider.gameObject.transform.parent.gameObject) && (_lastHoveringObj == null || _lastHoveringObj != hit.collider.gameObject))
        {
            CloseUnitHover();
            _lastHoveringObj = hit.collider.gameObject;
            _lastHoveringObj.transform.parent.Find("UnitUI").Find("SelectedUI").GetComponent<Image>().color = _HoverUnitColor;
        }
    }
    private void CloseUnitHover()
    {
        if (_lastHoveringObj != null && _lastHoveringObj.layer == LayerMask.NameToLayer("Unit"))
        {
            _lastHoveringObj.transform.parent.Find("UnitUI").Find("SelectedUI").GetComponent<Image>().color = _NotSelectedUnitColor;
            _lastHoveringObj = null;
        }
    }

    public GameObject[] GetNearestShips(Transform checkerTransform)
    {
        Collider[] hits = Physics.OverlapSphere(checkerTransform.position, 25f);

        List<(float value, GameObject obj)> shipsAndDistances = new List<(float, GameObject)>();
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("NavalUnit"))
            {
                float distance = Vector3.Distance(checkerTransform.position, hit.transform.position);
                shipsAndDistances.Add((distance, hit.gameObject));
            }
        }

        GameObject[] sortedShips = shipsAndDistances
       .OrderBy(o => o.value)
       .Select(o => o.obj)
       .ToArray();

        return sortedShips;
    }
    public GameObject[] GetNearestTrains(Transform checkerTransform)
    {
        Collider[] hits = Physics.OverlapSphere(checkerTransform.position, 25f);

        List<(float value, GameObject obj)> trainsAndDistances = new List<(float, GameObject)>();
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("LandUnit") && hit.gameObject.GetComponent<Unit>()._IsTrain)
            {
                float distance = Vector3.Distance(checkerTransform.position, hit.transform.position);
                trainsAndDistances.Add((distance, hit.gameObject));
            }
        }

        GameObject[] sortedTrains = trainsAndDistances
        .OrderBy(o => o.value)
        .Select(o => o.obj)
        .ToArray();

        return sortedTrains;
    }
    public GameObject[] GetNearestCargoPlanes(Transform checkerTransform)
    {
        Collider[] hits = Physics.OverlapSphere(checkerTransform.position, 25f);

        List<(float value, GameObject obj)> cargoPlanesAndDistances = new List<(float, GameObject)>();
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("LandUnit") && hit.GetComponent<Unit>()._Squads.GetSquadThisType<CargoPlane>() != null)
            {
                float distance = Vector3.Distance(checkerTransform.position, hit.transform.position);
                cargoPlanesAndDistances.Add((distance, hit.gameObject));
            }
        }

        GameObject[] sortedCargoPlanes = cargoPlanesAndDistances
       .OrderBy(o => o.value)
       .Select(o => o.obj)
       .ToArray();

        return sortedCargoPlanes;
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

    private bool IsGetOnTruckPossible(Squad squad)
    {
        if (!(squad is Infantry) && !(squad is Artillery) && !(squad is AntiTank) && !(squad is AntiAir)) return false;
        Truck truck = squad._AttachedUnit._Squads.GetSquadThisType<Truck>();
        if (truck == null) return false;
        return (squad is Infantry) ? truck._ManCarryLimit * truck._Amount - truck._CurrentManCarry >= squad._Weight : truck._Amount - truck._CurrentTow >= squad._Weight;
    }
    private bool IsGetOnShipPossible(GameObject executerObject)
    {
        if (executerObject.GetComponent<Unit>()._AttachedToUnitObject != null || executerObject.GetComponent<Unit>()._IsAir || executerObject.GetComponent<Unit>()._IsNaval || executerObject.GetComponent<Unit>()._IsTrain)
            return false;

        GameObject[] nearestShips = GameInputController._Instance.GetNearestShips(executerObject.transform);
        foreach (var nearestShip in nearestShips)
        {
            TransportShip transportShipSquad = nearestShip.GetComponent<Unit>()._Squads.GetSquadThisType<TransportShip>();
            bool canSquadsGetOnShip = executerObject.GetComponent<Unit>()._CanGetOnAnotherUnit;
            bool canSquadsFit = transportShipSquad != null ? (transportShipSquad._CarryLimit * transportShipSquad._Amount - transportShipSquad._CurrentCarry) >= executerObject.GetComponent<Unit>()._CarryWeight : false;
            if (nearestShip != null && transportShipSquad != null && canSquadsGetOnShip && canSquadsFit)
            {
                return true;
            }
        }
        return false;
    }
    private bool IsGetOnTrainPossible(GameObject executerObject)
    {
        if (executerObject.GetComponent<Unit>()._AttachedToUnitObject != null || executerObject.GetComponent<Unit>()._IsAir || executerObject.GetComponent<Unit>()._IsNaval || executerObject.GetComponent<Unit>()._IsTrain)
            return false;

        GameObject[] nearestTrains = GameInputController._Instance.GetNearestTrains(executerObject.transform);
        foreach (var nearestTrain in nearestTrains)
        {
            Train trainSquad = nearestTrain.GetComponent<Unit>()._Squads.GetSquadThisType<Train>();
            bool canSquadsGetOnTrain = executerObject.GetComponent<Unit>()._CanGetOnAnotherUnit;
            bool canSquadsFit = trainSquad != null ? (trainSquad._CarryLimit * trainSquad._Amount - trainSquad._CurrentCarry) >= executerObject.GetComponent<Unit>()._CarryWeight : false;
            if (nearestTrain != null && trainSquad != null && canSquadsGetOnTrain && canSquadsFit)
            {
                return true;
            }
        }
        return false;
    }
    private bool IsGetOnCargoPlanePossible(GameObject executerObject)
    {
        if (executerObject.GetComponent<Unit>()._AttachedToUnitObject != null || executerObject.GetComponent<Unit>()._IsAir || executerObject.GetComponent<Unit>()._IsNaval || executerObject.GetComponent<Unit>()._IsTrain)
            return false;

        GameObject[] nearestCargoPlanes = GameInputController._Instance.GetNearestCargoPlanes(executerObject.transform);
        foreach (var nearestCargoPlane in nearestCargoPlanes)
        {
            Train cargoPlaneSquad = nearestCargoPlane.GetComponent<Unit>()._Squads.GetSquadThisType<Train>();
            bool canSquadsGetOnCargoPlane = executerObject.GetComponent<Unit>()._CanGetOnAnotherUnit;
            bool canSquadsFit = cargoPlaneSquad != null ? (cargoPlaneSquad._CarryLimit * cargoPlaneSquad._Amount - cargoPlaneSquad._CurrentCarry) >= executerObject.GetComponent<Unit>()._CarryWeight : false;
            if (nearestCargoPlane != null && cargoPlaneSquad != null && canSquadsGetOnCargoPlane && canSquadsFit)
            {
                return true;
            }
        }
        return false;
    }
    private bool IsEvacuatePossible(Unit unit)
    {
        if (unit._IsNaval)
            return IsEvacuateShipPossible(unit.gameObject);
        else if (unit._IsTrain)
            return IsEvacuateTrainPossible(unit.gameObject);
        else if (unit._IsAir)
            return IsEvacuateCargoPlanePossible(unit.gameObject);
        return false;
    }
    private bool IsEvacuateShipPossible(GameObject executerObject)
    {
        if (executerObject.CompareTag("NavalUnit") && executerObject.transform.Find("CarryingUnits").childCount != 0)
        {
            TransportShip transportShipSquad = executerObject.GetComponent<Unit>()._Squads.GetSquadThisType<TransportShip>();
            if (transportShipSquad == null) return false;
            Transform[] childs = GameManager._Instance.GetNearChildTransforms(executerObject.transform.Find("CarryingUnits"));
            foreach (Transform carryingUnit in childs)
            {
                Vector3 landingPosition = GameInputController._Instance.GetNearestTerrainPosition(executerObject.transform.position);
                if ((landingPosition - executerObject.transform.position).magnitude > 25f) return false;

                if (carryingUnit.GetComponent<Unit>()._AttachedToUnitObject != null)
                {
                    return true;
                }
            }
        }
        return false;
    }
    private bool IsEvacuateTrainPossible(GameObject executerObject)
    {
        if (executerObject.CompareTag("LandUnit") && executerObject.transform.Find("CarryingUnits").childCount != 0)
        {
            Train trainSquad = executerObject.GetComponent<Unit>()._Squads.GetSquadThisType<Train>();
            if (trainSquad == null) return false;
            Transform[] childs = GameManager._Instance.GetNearChildTransforms(executerObject.transform.Find("CarryingUnits"));
            foreach (Transform carryingUnit in childs)
            {
                Vector3 landingPosition = GameInputController._Instance.GetNearestTerrainPosition(executerObject.transform.position);
                if ((landingPosition - executerObject.transform.position).magnitude > 25f) return false;

                if (carryingUnit.GetComponent<Unit>()._AttachedToUnitObject != null)
                {
                    return true;
                }
            }
        }
        return false;
    }
    private bool IsEvacuateCargoPlanePossible(GameObject executerObject)
    {
        if (executerObject.CompareTag("LandUnit") && executerObject.transform.Find("CarryingUnits").childCount != 0)
        {
            Train cargoPlaneSquad = executerObject.GetComponent<Unit>()._Squads.GetSquadThisType<Train>();
            if (cargoPlaneSquad == null) return false;
            Transform[] childs = GameManager._Instance.GetNearChildTransforms(executerObject.transform.Find("CarryingUnits"));
            foreach (Transform carryingUnit in childs)
            {
                Vector3 landingPosition = GameInputController._Instance.GetNearestTerrainPosition(executerObject.transform.position);
                if ((landingPosition - executerObject.transform.position).magnitude > 25f) return false;

                if (carryingUnit.GetComponent<Unit>()._AttachedToUnitObject != null)
                {
                    return true;
                }
            }
        }
        return false;
    }
}