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
    public Order _CurrentPlayerOrder { get; set; }
    public Transform _ArmyUIContent { get; private set; }
    public Transform _OrderUITransform { get; private set; }

    public float _SplitPercent;

    private List<GameObject> _splitUnitSliders;
    private List<GameObject> _tempSelectedUnits;

    private GameObject _lastSelectedCityObj;
    private GameObject _lastHoveringObj;

    private float _updateUICounter;
    private bool _isDraggingSplitSlider;

    private void Awake()
    {
        _Instance = this;
        _splitUnitSliders = new List<GameObject>();
        _tempSelectedUnits = new List<GameObject>();
        _SelectedUnits = new List<GameObject>();
        _SelectedSquads = new List<Squad>();
        _CurrentPlayerOrder = new MoveOrder();
        _ArmyUIContent = GameObject.Find("InGameScreen").transform.Find("OtherInGameMenus").Find("Left").Find("Army").Find("ScrollView").Find("Viewport").Find("Content");
        _OrderUITransform = GameManager._Instance._InGameScreen.transform.Find("OtherInGameMenus").Find("Left").Find("Army").Find("OrderUI");
        _SplitPercent = 0.5f;
    }
    private void Update()
    {
        if (GameManager._Instance._IsGameStopped) return;

        ArrangeSelectedUnits();
        ArrangeRouteGhost();
        ArrangeCurrentOrderToSelectedUnits();

        ArrangeSquadSelectionUI();
        ArrangeCityUI();
        ArrangeOrderUIButtons();

        _updateUICounter += Time.deltaTime;
        if (_updateUICounter > 0.2f)
            UpdateUI();

        if (Mouse.current.leftButton.ReadValue() == 0f)
            _isDraggingSplitSlider = false;
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
    public void DeSelectUnitButtonClicked(GameObject deSelectingObj)
    {
        DeSelectUnit(deSelectingObj);
    }
    public void HideOtherUIButtonClicked()
    {
        if (_OrderUITransform.parent.parent.GetComponent<RectTransform>().anchoredPosition.x == 0f)
        {
            _OrderUITransform.parent.parent.GetComponent<RectTransform>().anchoredPosition = new Vector2(-500f, _OrderUITransform.parent.parent.GetComponent<RectTransform>().anchoredPosition.y);
            _OrderUITransform.parent.parent.parent.Find("Right").GetComponent<RectTransform>().anchoredPosition = new Vector2(500f, _OrderUITransform.parent.parent.GetComponent<RectTransform>().anchoredPosition.y);
            _OrderUITransform.parent.parent.Find("HideOtherUIButton").Find("Text (TMP)").GetComponent<TextMeshProUGUI>().text = ">\n>\n>\n>\n>\n>\n>\n>\n>\n>\n>\n>\n>\n>\n>\n>\n>\n>\n>";
        }
        else
        {
            _OrderUITransform.parent.parent.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, _OrderUITransform.parent.parent.GetComponent<RectTransform>().anchoredPosition.y);
            _OrderUITransform.parent.parent.parent.Find("Right").GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, _OrderUITransform.parent.parent.GetComponent<RectTransform>().anchoredPosition.y);
            _OrderUITransform.parent.parent.Find("HideOtherUIButton").Find("Text (TMP)").GetComponent<TextMeshProUGUI>().text = "<\n<\n<\n<\n<\n<\n<\n<\n<\n<\n<\n<\n<\n<\n<\n<\n<\n<\n<";
        }

    }
    public void ToNewUnitButtonClicked()
    {
        if (_SelectedSquads.Count <= 1) return;
        if (!IsAllSelectedSquadsCloseToEachother()) return;
        if (!_SelectedSquads.IsAllSquadsSameType()) return;

        List<Squad> squadsWillTryToAttachTruck = new List<Squad>();
        Unit newUnitComponent = GameManager._Instance.CreateUnit(_SelectedSquads[0]._AttachedUnit.transform.position + new Vector3(Random.Range(-0.01f, 0.01f), 0f, Random.Range(-0.01f, 0.01f)), _SelectedSquads[0]._AttachedUnit.GetComponent<Unit>()._IsNaval).GetComponent<Unit>();
        Squad newSquad = null;

        List<Squad> squadsWillBeUnited = new List<Squad>();
        foreach (var selectedSquad in _SelectedSquads)
        {
            squadsWillBeUnited.Add(selectedSquad);
        }
        foreach (var selectedSquad in squadsWillBeUnited)
        {

            if (newSquad == null)
                newSquad = selectedSquad.CreateNewSquadObject(newUnitComponent);

            if (selectedSquad is ICarryBigUnit)
            {
                List<Transform> carryingUnits = new List<Transform>();
                foreach (Transform item in selectedSquad._AttachedUnit.transform.Find("CarryingUnits"))
                {
                    carryingUnits.Add(item);
                }
                foreach (Transform carryingUnit in carryingUnits)
                {
                    carryingUnit.SetParent(newUnitComponent.transform.Find("CarryingUnits"));
                    carryingUnit.GetComponent<Unit>()._AttachedToUnitObject = newUnitComponent.gameObject;
                    carryingUnit.transform.position = newUnitComponent.transform.position;
                }
            }

            newUnitComponent._Storage.AddStorage(selectedSquad._AttachedUnit._Storage);

            ArrangeDetachFromTruck(selectedSquad, squadsWillTryToAttachTruck);

            newSquad.AddValues(selectedSquad);

            if (selectedSquad._AttachedUnit != null && selectedSquad._AttachedUnit._Squad == selectedSquad)
                selectedSquad._AttachedUnit.RemoveUnit();

            selectedSquad._AttachedUnit = newUnitComponent;
        }

        newUnitComponent.AddSquad(newSquad);

        SelectUnit(newUnitComponent.gameObject);

        foreach (Squad tryToAttachSquad in squadsWillTryToAttachTruck)
        {
            TryToAttachSquadToTruck(tryToAttachSquad);
        }

        _SelectedSquads.ClearSelected();
        SelectSquad(newUnitComponent._Squad);

        newUnitComponent.UpdateModel(newUnitComponent._Squad._Amount);
        UpdateUI();
    }

    public void SplitAllSquadsButtonClicked()
    {
        List<Unit> unitsWillBeSplit = new List<Unit>();
        foreach (var item in _SelectedSquads)
        {
            unitsWillBeSplit.Add(item._AttachedUnit);
        }

        foreach (var item in unitsWillBeSplit)
        {
            SplitUnitButtonClicked(item);
        }
    }
    public void SplitUnitButtonClicked(Unit attachedUnit)
    {
        if (attachedUnit._Squad is ICarryBigUnit && (attachedUnit._Squad as ICarryBigUnit)._CurrentCarry != 0) return;
        if (attachedUnit._Squad is Truck && attachedUnit._Squad._AttachedUnit.transform.Find("CarryingUnits").childCount != 0) return;
        int splitAmount = (int)(attachedUnit._Squad._Amount * _SplitPercent);
        if (attachedUnit._Squad._Amount <= 1 || splitAmount < 1 || splitAmount == attachedUnit._Squad._Amount) return;

        //List<Squad> squadsWillTryToAttachTruck = new List<Squad>();

        Unit newUnitComponent = GameManager._Instance.CreateUnit(attachedUnit.transform.position, attachedUnit.GetComponent<Unit>()._IsNaval).GetComponent<Unit>();
        Squad newSquad = attachedUnit._Squad.CreateNewSquadObject(newUnitComponent);

        newSquad._Amount = splitAmount;
        float actualPercentForThisSquad = (newSquad._Amount * 1f) / (attachedUnit._Squad._Amount * 1f);
        attachedUnit._Squad._Amount -= splitAmount;

        newUnitComponent._Storage.AddStorage(attachedUnit._Storage.SplitStorage(_SplitPercent));
        newUnitComponent._Name = "New :" + attachedUnit._Name;

        newUnitComponent.AddSquad(newSquad);

        //ArrangeCarryingWhenSplit(attachedUnit._Squad, newSquad, newUnitComponent, actualPercentForThisSquad);
        //ArrangeDetachFromTruck(attachedUnit._Squad, squadsWillTryToAttachTruck, newSquad);

        if (newUnitComponent._Squad != null && newUnitComponent._Squad._Amount > 0)
            SelectUnit(newUnitComponent.gameObject);
        else
            Destroy(newUnitComponent.gameObject);

        //foreach (Squad tryToAttachSquad in squadsWillTryToAttachTruck)
        //{
        //TryToAttachSquadToTruck(tryToAttachSquad);
        //}

        attachedUnit.UpdateModel(attachedUnit._Squad._Amount);
        newUnitComponent.UpdateModel(newUnitComponent._Squad._Amount);
        UpdateUI();
    }
    private void ArrangeCarryingWhenSplit(Squad selectedSquad, Squad newSquad, Unit newUnitComponent, float splitPercent)
    {
        if (newSquad == null) return;
        if (newSquad._Amount > 0 && selectedSquad is ICarryBigUnit && (selectedSquad as ICarryBigUnit)._CurrentCarry != 0)
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
                GetCarriedByAnotherUnit(carryingNewUnitComponent, newUnitComponent);

                Squad carryingUnitSquad = carryingUnit.GetComponent<Unit>()._Squad;
                Squad newSquadForCarry = carryingUnitSquad.CreateNewSquadObject(carryingNewUnitComponent);

                newSquadForCarry._Amount = (int)(carryingUnitSquad._Amount * splitPercent);
                carryingUnitSquad._Amount -= newSquadForCarry._Amount;

                if (newSquadForCarry._Amount > 0)
                    carryingNewUnitComponent.AddSquad(newSquadForCarry);

                carryTransferAmount += newSquadForCarry._Weight;
            }

            (selectedSquad as ICarryBigUnit)._CurrentCarry -= carryTransferAmount;
            (newSquad as ICarryBigUnit)._CurrentCarry += carryTransferAmount;
        }
    }
    private void ArrangeDetachFromTruck(Squad selectedSquad, List<Squad> squadsWillTryToAttachTruck, Squad newSquad = null)
    {
        if (selectedSquad is Truck && selectedSquad._AttachedUnit.transform.Find("CarryingUnits").childCount != 0)
        {
            List<Transform> carryingUnitTransforms = new List<Transform>();

            foreach (Transform carrying in selectedSquad._AttachedUnit.transform.Find("CarryingUnits"))
            {
                carryingUnitTransforms.Add(carrying);
            }

            for (int i = 0; i < carryingUnitTransforms.Count; i++)
            {
                Squad squad = carryingUnitTransforms[i].GetComponent<Unit>()._Squad;

                if (squad is Infantry && (squad as Infantry)._AttachedTruck != null)
                {
                    if (newSquad != null)
                    {
                        squadsWillTryToAttachTruck?.Add(newSquad);
                        (selectedSquad as Truck)._CurrentManCarry -= newSquad._Weight;
                    }

                    squadsWillTryToAttachTruck?.Add(squad);
                    (selectedSquad as Truck)._CurrentManCarry -= squad._Weight;
                    (squad as Infantry)._AttachedTruck = null;
                    GetOutFromAnotherUnit(squad._AttachedUnit, selectedSquad._AttachedUnit.transform.position, selectedSquad._AttachedUnit.gameObject, isDeselecting: false);
                }
                else if (squad is Artillery && (squad as Artillery)._TowedTo != null)
                {
                    if (newSquad != null)
                    {
                        squadsWillTryToAttachTruck?.Add(newSquad);
                        (selectedSquad as Truck)._CurrentManCarry -= newSquad._Weight;
                    }

                    squadsWillTryToAttachTruck?.Add(squad);
                    (selectedSquad as Truck)._CurrentManCarry -= squad._Weight;
                    (squad as Artillery)._TowedTo = null;
                    GetOutFromAnotherUnit(squad._AttachedUnit, selectedSquad._AttachedUnit.transform.position, selectedSquad._AttachedUnit.gameObject, isDeselecting: false);
                }
                else if (squad is AntiTank && (squad as AntiTank)._TowedTo != null)
                {
                    if (newSquad != null)
                    {
                        squadsWillTryToAttachTruck?.Add(newSquad);
                        (selectedSquad as Truck)._CurrentManCarry -= newSquad._Weight;
                    }

                    squadsWillTryToAttachTruck?.Add(squad);
                    (selectedSquad as Truck)._CurrentManCarry -= squad._Weight;
                    (squad as AntiTank)._TowedTo = null;
                    GetOutFromAnotherUnit(squad._AttachedUnit, selectedSquad._AttachedUnit.transform.position, selectedSquad._AttachedUnit.gameObject, isDeselecting: false);
                }
                else if (squad is AntiAir && (squad as AntiAir)._TowedTo != null)
                {
                    if (newSquad != null)
                    {
                        squadsWillTryToAttachTruck?.Add(newSquad);
                        (selectedSquad as Truck)._CurrentManCarry -= newSquad._Weight;
                    }

                    squadsWillTryToAttachTruck?.Add(squad);
                    (selectedSquad as Truck)._CurrentManCarry -= squad._Weight;
                    (squad as AntiAir)._TowedTo = null;
                    GetOutFromAnotherUnit(squad._AttachedUnit, selectedSquad._AttachedUnit.transform.position, selectedSquad._AttachedUnit.gameObject, isDeselecting: false);
                }
            }
        }
    }
    public void TryToAttachSquadToTruck(Squad squad)
    {
        if (squad == null || squad._AttachedUnit == null) return;

        GameObject[] nearestTrucks = GetNearestTrucks(squad._AttachedUnit.transform);
        foreach (var nearestTruck in nearestTrucks)
        {
            if (!IsGetOnTruckPossibleForOneTruck(squad, nearestTruck.GetComponent<Unit>())) continue;

            if (squad is Infantry)
                new GetOnTruckOrder().ExecuteOrder(squad, nearestTruck.GetComponent<Unit>()._Squad as Truck);
            else if (squad is Artillery || squad is AntiTank || squad is AntiAir)
                new GetTowedOrder().ExecuteOrder(squad, nearestTruck.GetComponent<Unit>()._Squad as Truck);

            break;
        }
    }

    public void StopOrderButtonClicked()
    {
        foreach (var obj in _SelectedUnits)
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
    public void GetOnTruckButtonClicked(Unit unit)
    {
        TryToAttachSquadToTruck(unit._Squad);
    }
    public void GetOnShipButtonClicked(Unit unit)
    {
        if (IsGetOnShipPossible(unit.gameObject))
            new GetOnShipOrder().ExecuteOrder(unit.gameObject);
    }
    public void GetOnTrainButtonClicked(Unit unit)
    {
        if (IsGetOnTrainPossible(unit.gameObject))
            new GetOnTrainOrder().ExecuteOrder(unit.gameObject);
    }
    public void GetOnCargoPlaneButtonClicked(Unit unit)
    {
        if (IsGetOnCargoPlanePossible(unit.gameObject))
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
        else if (unit._Squad is Truck)
            EvacuateTruck(unit);
    }
    private void EvacuateTruck(Unit unit)
    {
        List<Transform> carryingTransforms = new List<Transform>();
        for (int i = 0; i < unit.transform.Find("CarryingUnits").childCount; i++)
        {
            carryingTransforms.Add(unit.transform.Find("CarryingUnits").GetChild(i));
        }
        for (int i = 0; i < carryingTransforms.Count; i++)
        {
            Unit carryingUnit = carryingTransforms[i].GetComponent<Unit>();

            if (carryingUnit._Squad is Infantry)
                new GetOffTruckOrder().ExecuteOrder(carryingUnit._Squad);
            else if ((carryingUnit._Squad is Artillery) || (carryingUnit._Squad is AntiTank) || (carryingUnit._Squad is AntiAir))
                new GetTowedOffOrder().ExecuteOrder(carryingUnit._Squad);
        }
    }

    public void SelectUnit(GameObject unit)
    {
        if (_SelectedUnits.Contains(unit)) return;

        _lastHoveringObj = null;
        unit.transform.Find("Model").Find("UnitUI").Find("SelectedUI").GetComponent<Image>().color = _SelectedUnitColor;
        unit.transform.Find("Model").Find("UnitUI").Find("NameAmountText").GetComponent<TextMeshProUGUI>().text = unit.GetComponent<Unit>()._Name + "\nx" + unit.GetComponent<Unit>()._Squad._Amount;
        unit.transform.Find("Model").Find("UnitUI").Find("NameAmountText").GetComponent<TextMeshProUGUI>().color = new Color(130 / 255f, 190f / 255f, 110f / 255f, 1f);
        unit.transform.Find("Model").Find("UnitUI").Find("NameAmountText").GetComponent<TextMeshProUGUI>().fontSize = 1.6f;

        if (unit.transform.Find("CurrentRouteGhost").GetComponent<LineRenderer>().colorGradient != GameManager._Instance._SelectedGradientForCurrentRoute)
            unit.transform.Find("CurrentRouteGhost").GetComponent<LineRenderer>().colorGradient = GameManager._Instance._SelectedGradientForCurrentRoute;

        _SelectedUnits.Add(unit);

        if (!GameManager._Instance._InGameScreen.transform.Find("OtherInGameMenus").Find("Left").Find("Army").gameObject.activeSelf)
            OpenArmyScreen();
        else
            UpdateUI();
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

        unit.transform.Find("Model").Find("UnitUI").Find("SelectedUI").GetComponent<Image>().color = _NotSelectedUnitColor;
        unit.transform.Find("Model").Find("UnitUI").Find("NameAmountText").GetComponent<TextMeshProUGUI>().text = "x" + unit.GetComponent<Unit>()._Squad._Amount;
        unit.transform.Find("Model").Find("UnitUI").Find("NameAmountText").GetComponent<TextMeshProUGUI>().color = new Color(130 / 255f, 190f / 255f, 110f / 255f, 0.35f);
        unit.transform.Find("Model").Find("UnitUI").Find("NameAmountText").GetComponent<TextMeshProUGUI>().fontSize = 1.2f;
        unit.transform.Find("PotentialRouteGhost").gameObject.SetActive(false);

        if (unit.transform.Find("CurrentRouteGhost").GetComponent<LineRenderer>().colorGradient != GameManager._Instance._NotSelectedGradientForCurrentRoute)
            unit.transform.Find("CurrentRouteGhost").GetComponent<LineRenderer>().colorGradient = GameManager._Instance._NotSelectedGradientForCurrentRoute;
        //unit.transform.Find("CurrentRouteGhost").gameObject.SetActive(false);

        _SelectedUnits.Remove(unit);

        UpdateUI();
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

    public void GetCarriedByAnotherUnit(Unit unit, Unit carrierUnit, bool isNestedCarry = false)
    {
        if (isNestedCarry)
            unit._WasInNestedTruck = true;
        if (unit._IsCarryingWithTruck)
        {
            List<Unit> tempList = new List<Unit>();
            foreach (Transform carryingWithTruck in unit.transform.Find("CarryingUnits"))
            {
                tempList.Add(carryingWithTruck.GetComponent<Unit>());
            }
            ArrangeDetachFromTruck(unit.GetComponent<Unit>()._Squad, null);
            foreach (Unit carryingWithTruck in tempList)
            {
                GetCarriedByAnotherUnit(carryingWithTruck.GetComponent<Unit>(), carrierUnit, isNestedCarry: true);
            }
        }

        unit._AttachedToUnitObject = carrierUnit.gameObject;
        unit.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
        unit.transform.SetParent(carrierUnit.transform.Find("CarryingUnits"));
        unit.transform.position = carrierUnit.transform.position;
        unit.gameObject.SetActive(false);
        DeSelectUnit(unit.gameObject);
    }
    public void GetOutFromAnotherUnit(Unit unit, Vector3 landingPosition, GameObject executerObject, bool isDeselecting = true, List<Unit> nestedTuckCarryUnitsWillTry = null)
    {
        unit._AttachedToUnitObject = null;
        unit.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotation;
        unit.transform.parent = null;
        unit.transform.position = landingPosition;
        unit.gameObject.SetActive(true);
        if (isDeselecting)
            DeSelectUnit(executerObject);

        if (nestedTuckCarryUnitsWillTry != null && unit._WasInNestedTruck)
            nestedTuckCarryUnitsWillTry.Add(unit);

        unit._WasInNestedTruck = false;
    }

    private void ArrangeOrderUIButtons()
    {
        if (_SelectedSquads.Count > 1 && _SelectedSquads.IsAllSquadsSameType() && IsAllSelectedSquadsCloseToEachother())
        {
            if (!_OrderUITransform.Find("ToNewUnitButton").GetComponent<Button>().interactable)
                _OrderUITransform.Find("ToNewUnitButton").GetComponent<Button>().interactable = true;
        }
        else
        {
            if (_OrderUITransform.Find("ToNewUnitButton").GetComponent<Button>().interactable)
                _OrderUITransform.Find("ToNewUnitButton").GetComponent<Button>().interactable = false;
        }

        if (_SelectedSquads.Count > 0 && _SelectedSquads.IsSplitPossibleOnAny())
        {
            if (!_OrderUITransform.Find("SplitAllSquadsButton").GetComponent<Button>().interactable)
                _OrderUITransform.Find("SplitAllSquadsButton").GetComponent<Button>().interactable = true;
        }
        else
        {
            if (_OrderUITransform.Find("SplitAllSquadsButton").GetComponent<Button>().interactable)
                _OrderUITransform.Find("SplitAllSquadsButton").GetComponent<Button>().interactable = false;
        }


        if (_SelectedUnits.Count > 0)
        {
            if (!_OrderUITransform.Find("StopOrderButton").GetComponent<Button>().interactable)
                _OrderUITransform.Find("StopOrderButton").GetComponent<Button>().interactable = true;
        }
        else
        {
            if (_OrderUITransform.Find("StopOrderButton").GetComponent<Button>().interactable)
                _OrderUITransform.Find("StopOrderButton").GetComponent<Button>().interactable = false;
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
            unitUI.transform.Find("SelfContent").Find("CarryCapacity").GetComponent<TextMeshProUGUI>().text = refUnit._CanCarryAnotherBigUnit ? refUnit.GetCurrentCarry().ToString() + "/" + refUnit.GetCarryingCapacity().ToString() : "";

            List<Squad> carryingSquads = new List<Squad>();
            for (int t = 0; t < refUnit.transform.Find("CarryingUnits").childCount; t++)
            {
                carryingSquads.Add(refUnit.transform.Find("CarryingUnits").GetChild(t).GetComponent<Unit>()._Squad);
            }
            UpdateSquadUI(unitUI, refUnit, carryingSquads);
            int squadCount = 1 + carryingSquads.Count;
            unitUI.GetComponent<RectTransform>().sizeDelta = new Vector2(unitUI.GetComponent<RectTransform>().sizeDelta.x, 50f + squadCount * 40f);

            unitUI.transform.Find("SelfContent").Find("DeSelectUnitButton").GetComponent<Button>().onClick.RemoveAllListeners();
            unitUI.transform.Find("SelfContent").Find("DeSelectUnitButton").GetComponent<Button>().onClick.AddListener(SoundManager._Instance.PlayButtonSound);
            unitUI.transform.Find("SelfContent").Find("DeSelectUnitButton").GetComponent<Button>().onClick.AddListener(() => DeSelectUnitButtonClicked(refUnit.gameObject));

            unitUI.transform.Find("SelfContent").Find("SplitUnitButton").GetComponent<Button>().interactable = IsSplitPossible(refUnit);
            unitUI.transform.Find("SelfContent").Find("SplitUnitButton").GetComponent<Button>().onClick.RemoveAllListeners();
            unitUI.transform.Find("SelfContent").Find("SplitUnitButton").GetComponent<Button>().onClick.AddListener(SoundManager._Instance.PlayButtonSound);
            unitUI.transform.Find("SelfContent").Find("SplitUnitButton").GetComponent<Button>().onClick.AddListener(() => SplitUnitButtonClicked(refUnit));

            unitUI.transform.Find("SelfContent").Find("SplitUnitSlider").GetComponent<Slider>().value = _SplitPercent;
            unitUI.transform.Find("SelfContent").Find("SplitUnitSlider").GetComponent<Slider>().onValueChanged.RemoveAllListeners();
            unitUI.transform.Find("SelfContent").Find("SplitUnitSlider").GetComponent<Slider>().onValueChanged.AddListener((float newValue) => { _SplitPercent = newValue; _isDraggingSplitSlider = true; UpdateUI(); });
            _splitUnitSliders.Add(unitUI.transform.Find("SelfContent").Find("SplitUnitSlider").gameObject);

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
        int allSquadCount = 1 + carryingSquads.Count;
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
            Squad squad = (1 > i - indexForDestroyingUI) ? refUnit._Squad : carryingSquads[i - 1 - indexForDestroyingUI];

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

            if (IsHoveringSplit())
                squadUI.Find("Amount").GetComponent<TextMeshProUGUI>().text = squad._Amount.ToString() + " (-" + ((int)(squad._Amount * _SplitPercent)).ToString() + ")";

            if (1 <= i - indexForDestroyingUI)
            {
                //carrying ui setup
                squadUI.GetComponent<Image>().color = new Color(0.45f, 0.45f, 1f);
                squadUI.Find("Icon").GetComponent<Button>().interactable = false;
            }
            else
            {
                squadUI.Find("Icon").GetComponent<Button>().interactable = true;
            }

        }
    }
    private bool IsHoveringSplit()
    {
        bool isHovering = false;
        List<GameObject> slidersWillBeRemoved = new List<GameObject>();
        foreach (GameObject splitUnitSlider in _splitUnitSliders)
        {
            if (splitUnitSlider == null)
            {
                slidersWillBeRemoved.Add(splitUnitSlider);
                continue;
            }
            bool isMouseOverSplitButton = RectTransformUtility.RectangleContainsScreenPoint(splitUnitSlider.transform.parent.Find("SplitUnitButton").GetComponent<RectTransform>(), Input.mousePosition, null);
            bool isMouseOverSplitSlider = RectTransformUtility.RectangleContainsScreenPoint(splitUnitSlider.GetComponent<RectTransform>(), Input.mousePosition, null);
            if (isMouseOverSplitButton || isMouseOverSplitSlider || _isDraggingSplitSlider)
                isHovering = true;
        }

        foreach (var item in slidersWillBeRemoved)
        {
            _splitUnitSliders.Remove(item);
        }

        return isHovering;
    }
    private void ArrangeCarryButtonsUI(Transform unitUI, Unit refUnit)
    {
        if (refUnit._CanCarryAnotherBigUnit)
            unitUI.transform.Find("SelfContent").Find("EvacuateButton").GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, unitUI.transform.Find("SelfContent").Find("EvacuateButton").GetComponent<RectTransform>().anchoredPosition.y);
        else
            unitUI.transform.Find("SelfContent").Find("EvacuateButton").GetComponent<RectTransform>().anchoredPosition = new Vector2(-50f, unitUI.transform.Find("SelfContent").Find("EvacuateButton").GetComponent<RectTransform>().anchoredPosition.y);

        if (refUnit._Squad is Truck)
        {
            if (unitUI.transform.Find("SelfContent").Find("GetOnTruckButton").gameObject.activeSelf)
                unitUI.transform.Find("SelfContent").Find("GetOnTruckButton").gameObject.SetActive(false);
            if (!unitUI.transform.Find("SelfContent").Find("GetOnShipButton").gameObject.activeSelf)
                unitUI.transform.Find("SelfContent").Find("GetOnShipButton").gameObject.SetActive(true);
            if (!unitUI.transform.Find("SelfContent").Find("GetOnTrainButton").gameObject.activeSelf)
                unitUI.transform.Find("SelfContent").Find("GetOnTrainButton").gameObject.SetActive(true);
            if (!unitUI.transform.Find("SelfContent").Find("GetOnCargoPlaneButton").gameObject.activeSelf)
                unitUI.transform.Find("SelfContent").Find("GetOnCargoPlaneButton").gameObject.SetActive(true);
            if (!unitUI.transform.Find("SelfContent").Find("EvacuateButton").gameObject.activeSelf)
                unitUI.transform.Find("SelfContent").Find("EvacuateButton").gameObject.SetActive(true);

            unitUI.transform.Find("SelfContent").Find("EvacuateButton").GetComponent<Button>().interactable = IsEvacuatePossible(refUnit);
            unitUI.transform.Find("SelfContent").Find("EvacuateButton").GetComponent<Button>().onClick.RemoveAllListeners();
            unitUI.transform.Find("SelfContent").Find("EvacuateButton").GetComponent<Button>().onClick.AddListener(SoundManager._Instance.PlayButtonSound);
            unitUI.transform.Find("SelfContent").Find("EvacuateButton").GetComponent<Button>().onClick.AddListener(() => EvacuateButtonClicked(refUnit));

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
        else if (refUnit._CanCarryAnotherBigUnit)
        {
            if (unitUI.transform.Find("SelfContent").Find("GetOnTruckButton").gameObject.activeSelf)
                unitUI.transform.Find("SelfContent").Find("GetOnTruckButton").gameObject.SetActive(false);
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
        else if (refUnit._CanGetOnAnotherUnit)
        {
            if (!unitUI.transform.Find("SelfContent").Find("GetOnTruckButton").gameObject.activeSelf)
                unitUI.transform.Find("SelfContent").Find("GetOnTruckButton").gameObject.SetActive(true);
            if (!unitUI.transform.Find("SelfContent").Find("GetOnShipButton").gameObject.activeSelf)
                unitUI.transform.Find("SelfContent").Find("GetOnShipButton").gameObject.SetActive(true);
            if (!unitUI.transform.Find("SelfContent").Find("GetOnTrainButton").gameObject.activeSelf)
                unitUI.transform.Find("SelfContent").Find("GetOnTrainButton").gameObject.SetActive(true);
            if (!unitUI.transform.Find("SelfContent").Find("GetOnCargoPlaneButton").gameObject.activeSelf)
                unitUI.transform.Find("SelfContent").Find("GetOnCargoPlaneButton").gameObject.SetActive(true);
            if (unitUI.transform.Find("SelfContent").Find("EvacuateButton").gameObject.activeSelf)
                unitUI.transform.Find("SelfContent").Find("EvacuateButton").gameObject.SetActive(false);

            unitUI.transform.Find("SelfContent").Find("GetOnTruckButton").GetComponent<Button>().interactable = IsGetOnTruckPossible(refUnit);
            unitUI.transform.Find("SelfContent").Find("GetOnTruckButton").GetComponent<Button>().onClick.RemoveAllListeners();
            unitUI.transform.Find("SelfContent").Find("GetOnTruckButton").GetComponent<Button>().onClick.AddListener(SoundManager._Instance.PlayButtonSound);
            unitUI.transform.Find("SelfContent").Find("GetOnTruckButton").GetComponent<Button>().onClick.AddListener(() => GetOnTruckButtonClicked(refUnit));

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
        else
        {
            if (unitUI.transform.Find("SelfContent").Find("GetOnTruckButton").gameObject.activeSelf)
                unitUI.transform.Find("SelfContent").Find("GetOnTruckButton").gameObject.SetActive(false);
            if (unitUI.transform.Find("SelfContent").Find("GetOnShipButton").gameObject.activeSelf)
                unitUI.transform.Find("SelfContent").Find("GetOnShipButton").gameObject.SetActive(false);
            if (unitUI.transform.Find("SelfContent").Find("GetOnTrainButton").gameObject.activeSelf)
                unitUI.transform.Find("SelfContent").Find("GetOnTrainButton").gameObject.SetActive(false);
            if (unitUI.transform.Find("SelfContent").Find("GetOnCargoPlaneButton").gameObject.activeSelf)
                unitUI.transform.Find("SelfContent").Find("GetOnCargoPlaneButton").gameObject.SetActive(false);
            if (unitUI.transform.Find("SelfContent").Find("EvacuateButton").gameObject.activeSelf)
                unitUI.transform.Find("SelfContent").Find("EvacuateButton").gameObject.SetActive(false);
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

            if (Mouse.current.leftButton.wasReleasedThisFrame && _lastHoveringObj == hit.collider.gameObject && _SelectedUnits.Count == 0)
            {
                if (!GameManager._Instance._InGameScreen.transform.Find("OtherInGameMenus").Find("Left").Find("City").gameObject.activeInHierarchy)
                    OpenCityScreen(hit.collider.gameObject);
                else
                    UpdateUI();
            }

        }
        else
        {
            CloseCityHover();

            if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                if (GameManager._Instance._InGameScreen.transform.Find("OtherInGameMenus").Find("Left").Find("City").gameObject.activeInHierarchy)
                    CloseAllInGameOtherUI();
            }
        }
    }
    private void OpenCityHoverInNeed(RaycastHit hit)
    {
        if (_lastSelectedCityObj == null && _lastHoveringObj == null && !IsMouseOverSelected() && !SelectionBox._Instance._IsDragging)
        {
            CloseCityHover();
            _lastHoveringObj = hit.collider.gameObject;
            _lastHoveringObj.transform.Find("CityUI").Find("SelectedUI").GetComponent<Image>().color = _HoverCityColor;
        }
    }
    public void CloseCityHover()
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

        _OrderUITransform.parent.parent.Find("HideOtherUIButton").gameObject.SetActive(true);
        _OrderUITransform.parent.parent.Find("HideOtherUIButton").Find("Text (TMP)").GetComponent<TextMeshProUGUI>().text = "<\n<\n<\n<\n<\n<\n<\n<\n<\n<\n<\n<\n<\n<\n<\n<\n<\n<\n<";
        _OrderUITransform.parent.parent.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, _OrderUITransform.parent.parent.GetComponent<RectTransform>().anchoredPosition.y);
        _OrderUITransform.parent.parent.parent.Find("Right").GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, _OrderUITransform.parent.parent.GetComponent<RectTransform>().anchoredPosition.y);

        GameManager._Instance._InGameScreen.transform.Find("OtherInGameMenus").gameObject.SetActive(true);
        GameManager._Instance._InGameScreen.transform.Find("OtherInGameMenus").Find("Left").Find("Army").gameObject.SetActive(true);
        GameManager._Instance._InGameScreen.transform.Find("OtherInGameMenus").Find("Right").Find("ArmyDetails").gameObject.SetActive(true);

        UpdateUI();
    }
    public void OpenCityScreen(GameObject cityObject)
    {
        if (GameManager._Instance._ConstructionScreen.activeInHierarchy) return;

        CloseAllInGameOtherUI();

        _OrderUITransform.parent.parent.Find("HideOtherUIButton").gameObject.SetActive(true);
        _OrderUITransform.parent.parent.Find("HideOtherUIButton").Find("Text (TMP)").GetComponent<TextMeshProUGUI>().text = "<\n<\n<\n<\n<\n<\n<\n<\n<\n<\n<\n<\n<\n<\n<\n<\n<\n<\n<";
        _OrderUITransform.parent.parent.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, _OrderUITransform.parent.parent.GetComponent<RectTransform>().anchoredPosition.y);
        _OrderUITransform.parent.parent.parent.Find("Right").GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, _OrderUITransform.parent.parent.GetComponent<RectTransform>().anchoredPosition.y);

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
            _lastHoveringObj.transform.parent.Find("Model").Find("UnitUI").Find("SelectedUI").GetComponent<Image>().color = _NotSelectedUnitColor;

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
    private bool IsMouseOverSelected()
    {
        if (GameManager._Instance._ConstructionScreen.activeInHierarchy) return false;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return false;

        Physics.Raycast(Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue()), out RaycastHit hit, 30000f, LayerMask.GetMask("Unit"));
        if (hit.collider != null && hit.collider.transform.parent.GetComponent<Unit>() != null && hit.collider.transform.parent.GetComponent<Unit>().IsSelected())
            return true;
        return false;
    }

    private void ArrangeRouteGhost()
    {
        foreach (var obj in _SelectedUnits)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                obj.transform.Find("PotentialRouteGhost").gameObject.SetActive(false);
            else if (_CurrentPlayerOrder is MoveOrder)
                obj.transform.Find("PotentialRouteGhost").gameObject.SetActive(true);
            else
                obj.transform.Find("PotentialRouteGhost").gameObject.SetActive(false);

            if (_CurrentPlayerOrder is MoveOrder moveOrder)
            {
                obj.GetComponent<Unit>()._PotentialRoutePoints.Clear();
                obj.GetComponent<Unit>()._PotentialRoutePoints.Add(moveOrder._OrderPosition);
                moveOrder.ArrangeOrderGhostForPlayer(obj, obj.transform.position, obj.GetComponent<Unit>()._PotentialRoutePoints, GameManager._Instance._InputActions.FindAction("Sprint").ReadValue<float>() != 0f || GameManager._Instance._InputActions.FindAction("Alt").ReadValue<float>() != 0f, -obj.transform.up.normalized);
            }
        }
    }
    private void ArrangeCurrentOrderToSelectedUnits()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Physics.Raycast(Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue()), out RaycastHit hit, 30000f, GameManager._Instance._TerrainAndWaterLayers);
        if (hit.collider != null)
            _CurrentPlayerOrder._OrderPosition = hit.point;
        //_CurrentPlayerOrder._OrderPosition = GameManager._Instance._InputActions.FindAction("Alt").ReadValue<float>() == 0 ? hit.point : TerrainController._Instance.GetClosestRoadKnot(TerrainController._Instance.GetTerrainPointFromMouse()._Position);
        else
            return;

        if (!Mouse.current.rightButton.wasPressedThisFrame) return;

        foreach (var obj in _SelectedUnits)
        {
            if (_CurrentPlayerOrder is MoveOrder)
                (_CurrentPlayerOrder as MoveOrder).ExecuteOrder(obj, GameManager._Instance._InputActions.FindAction("Sprint").ReadValue<float>() == 0f && GameManager._Instance._InputActions.FindAction("Alt").ReadValue<float>() == 0f);
        }
    }
    private void ArrangeSelectedUnits()
    {
        _tempSelectedUnits.Clear();
        foreach (var item in _SelectedUnits)
        {
            _tempSelectedUnits.Add(item);
        }
        foreach (var item in _tempSelectedUnits)
        {
            if (item == null)
                _SelectedUnits.Remove(item);
        }

        if (GameManager._Instance._ConstructionScreen.activeInHierarchy)
        {
            if (_SelectedUnits.Count > 0)
                _SelectedUnits.ClearSelected();
            return;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Physics.Raycast(Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue()), out RaycastHit hit, 30000f, LayerMask.GetMask("Unit"));
        if (hit.collider != null && hit.collider.transform.parent != null && hit.collider.transform.parent.GetComponent<Unit>() != null && !hit.collider.transform.parent.GetComponent<Unit>()._IsDead && !hit.collider.transform.parent.GetComponent<Unit>()._IsEnemy)
        {
            OpenUnitHoverInNeed(hit);

            if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                if (GameManager._Instance._InputActions.FindAction("Sprint").ReadValue<float>() == 0f)// || (_SelectedUnits.Count > 0 && _SelectedUnits[0].GetComponent<Unit>()._IsNaval != hit.collider.transform.parent.GetComponent<Unit>()._IsNaval)
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
                }
            }
        }
        else
        {
            CloseUnitHover();

            if (Mouse.current.leftButton.wasReleasedThisFrame && GameManager._Instance._InputActions.FindAction("Sprint").ReadValue<float>() == 0f)
            {
                _SelectedUnits.ClearSelected();
                if (GameManager._Instance._InGameScreen.transform.Find("OtherInGameMenus").Find("Left").Find("Army").gameObject.activeInHierarchy)
                    CloseAllInGameOtherUI();
            }
        }
    }
    private void OpenUnitHoverInNeed(RaycastHit hit)
    {
        if (!SelectionBox._Instance._IsDragging && !_SelectedUnits.Contains(hit.collider.gameObject.transform.parent.gameObject) && (_lastHoveringObj == null || _lastHoveringObj != hit.collider.gameObject))
        {
            CloseUnitHover();
            CloseCityHover();
            OpenUnitHover(hit.collider.gameObject);
        }
    }
    public void OpenUnitHover(GameObject unitObj, bool isLastHoverObj = true)
    {
        if (isLastHoverObj)
        {
            _lastHoveringObj = unitObj;
            _lastHoveringObj.transform.parent.Find("Model").Find("UnitUI").Find("SelectedUI").GetComponent<Image>().color = _HoverUnitColor;
        }
        else
        {
            unitObj.transform.Find("Model").Find("UnitUI").Find("SelectedUI").GetComponent<Image>().color = _HoverUnitColor;
        }

    }
    public void CloseUnitHover(GameObject obj = null)
    {
        if (obj == null)
        {
            if (_lastHoveringObj != null && _lastHoveringObj.layer == LayerMask.NameToLayer("Unit"))
            {
                _lastHoveringObj.transform.parent.Find("Model").Find("UnitUI").Find("SelectedUI").GetComponent<Image>().color = _NotSelectedUnitColor;
                _lastHoveringObj = null;
            }
        }
        else
        {
            obj.transform.Find("Model").Find("UnitUI").Find("SelectedUI").GetComponent<Image>().color = _NotSelectedUnitColor;
        }

    }

    public GameObject[] GetNearestTrucks(Transform checkerTransform)
    {
        Collider[] hits = Physics.OverlapSphere(checkerTransform.position, 25f);
        List<(float value, GameObject obj)> trucksAndDistances = new List<(float, GameObject)>();
        foreach (Collider hit in hits)
        {
            if (hit.transform.parent != null && hit.transform.parent.CompareTag("LandUnit") && hit.transform.parent.gameObject.GetComponent<Unit>()._Squad is Truck)
            {
                float distance = Vector3.Distance(checkerTransform.position, hit.transform.parent.transform.position);
                trucksAndDistances.Add((distance, hit.transform.parent.gameObject));
            }
        }

        GameObject[] sortedTrucks = trucksAndDistances
       .OrderBy(o => o.value)
       .Select(o => o.obj)
       .ToArray();

        return sortedTrucks;
    }
    public GameObject[] GetNearestShips(Transform checkerTransform)
    {
        Collider[] hits = Physics.OverlapSphere(checkerTransform.position, 25f);

        List<(float value, GameObject obj)> shipsAndDistances = new List<(float, GameObject)>();
        foreach (Collider hit in hits)
        {
            if (hit.transform.parent != null && hit.transform.parent.CompareTag("NavalUnit") && hit.transform.parent.GetComponent<Unit>()._Squad is TransportShip)
            {
                float distance = Vector3.Distance(checkerTransform.position, hit.transform.parent.transform.position);
                shipsAndDistances.Add((distance, hit.transform.parent.gameObject));
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
            if (hit.transform.parent != null && hit.transform.parent.CompareTag("LandUnit") && hit.transform.parent.gameObject.GetComponent<Unit>()._IsTrain)
            {
                float distance = Vector3.Distance(checkerTransform.position, hit.transform.parent.transform.position);
                trainsAndDistances.Add((distance, hit.transform.parent.gameObject));
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
            if (hit.transform.parent != null && hit.transform.parent.CompareTag("LandUnit") && hit.transform.parent.GetComponent<Unit>()._Squad is CargoPlane)
            {
                float distance = Vector3.Distance(checkerTransform.position, hit.transform.parent.transform.position);
                cargoPlanesAndDistances.Add((distance, hit.transform.parent.gameObject));
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

    public bool IsSplitPossible(Unit unit)
    {
        if (unit._Squad is ICarryBigUnit && (unit._Squad as ICarryBigUnit)._CurrentCarry != 0) return false;
        if (unit._Squad is Truck && unit._Squad._AttachedUnit.transform.Find("CarryingUnits").childCount != 0) return false;
        int splitAmount = (int)(unit._Squad._Amount * _SplitPercent);
        if (unit._Squad._Amount <= 1 || splitAmount < 1 || splitAmount == unit._Squad._Amount) return false;

        return true;
    }
    private bool IsGetOnTruckPossible(Unit unit)
    {
        Squad squad = unit.GetComponent<Unit>()._Squad;
        GameObject[] nearestTrucks = GetNearestTrucks(squad._AttachedUnit.transform);

        for (int i = 0; i < nearestTrucks.Length; i++)
        {
            //Debug.Log((squad as Infantry)._AttachedTruck);
            //Debug.Log(IsGetOnTruckPossibleForOneTruck(squad, nearestTrucks[i].GetComponent<Unit>()));
            if (squad is Infantry && (squad as Infantry)._AttachedTruck == null && IsGetOnTruckPossibleForOneTruck(squad, nearestTrucks[i].GetComponent<Unit>()))
                return true;
            if (squad is Artillery && (squad as Artillery)._TowedTo == null && IsGetOnTruckPossibleForOneTruck(squad, nearestTrucks[i].GetComponent<Unit>()))
                return true;
            if (squad is AntiTank && (squad as AntiTank)._TowedTo == null && IsGetOnTruckPossibleForOneTruck(squad, nearestTrucks[i].GetComponent<Unit>()))
                return true;
            if (squad is AntiAir && (squad as AntiAir)._TowedTo == null && IsGetOnTruckPossibleForOneTruck(squad, nearestTrucks[i].GetComponent<Unit>()))
                return true;
        }
        return false;
    }
    private bool IsGetOnTruckPossibleForOneTruck(Squad squad, Unit truckUnit)
    {
        if (!(truckUnit._Squad is Truck)) return false;
        if (!(squad is Infantry) && !(squad is Artillery) && !(squad is AntiTank) && !(squad is AntiAir)) return false;

        Truck truck = truckUnit._Squad as Truck;
        return (squad is Infantry) ? truck._ManCarryLimit * truck._Amount - truck._CurrentManCarry >= squad._Weight : truck._Amount - truck._CurrentTow >= squad._Weight;
    }

    private bool IsGetOnShipPossible(GameObject executerObject)
    {
        if (executerObject.GetComponent<Unit>()._AttachedToUnitObject != null || executerObject.GetComponent<Unit>()._IsAir || executerObject.GetComponent<Unit>()._IsNaval || executerObject.GetComponent<Unit>()._IsTrain)
            return false;

        GameObject[] nearestShips = GameInputController._Instance.GetNearestShips(executerObject.transform);
        foreach (var nearestShip in nearestShips)
        {
            TransportShip transportShipSquad = nearestShip.GetComponent<Unit>()._Squad as TransportShip;
            bool canSquadsGetOnShip = executerObject.GetComponent<Unit>()._CanGetOnAnotherUnit;

            int realWeight = executerObject.GetComponent<Unit>()._CarryWeight;
            if (executerObject.GetComponent<Unit>()._IsCarryingWithTruck)
            {
                if (!IsEvacuatePossible(executerObject.GetComponent<Unit>())) return false;
                foreach (Transform carryingUnit in executerObject.transform.Find("CarryingUnits"))
                {
                    realWeight += carryingUnit.GetComponent<Unit>()._CarryWeight;
                }
            }
            bool canSquadsFit = transportShipSquad != null ? (transportShipSquad._CarryLimit * transportShipSquad._Amount - transportShipSquad._CurrentCarry) >= realWeight : false;

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
            Train trainSquad = nearestTrain.GetComponent<Unit>()._Squad as Train;
            bool canSquadsGetOnTrain = executerObject.GetComponent<Unit>()._CanGetOnAnotherUnit;

            int realWeight = executerObject.GetComponent<Unit>()._CarryWeight;
            if (executerObject.GetComponent<Unit>()._IsCarryingWithTruck)
            {
                if (!IsEvacuatePossible(executerObject.GetComponent<Unit>())) return false;
                foreach (Transform carryingUnit in executerObject.transform.Find("CarryingUnits"))
                {
                    realWeight += carryingUnit.GetComponent<Unit>()._CarryWeight;
                }
            }
            bool canSquadsFit = trainSquad != null ? (trainSquad._CarryLimit * trainSquad._Amount - trainSquad._CurrentCarry) >= realWeight : false;

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
            CargoPlane cargoPlaneSquad = nearestCargoPlane.GetComponent<Unit>()._Squad as CargoPlane;
            bool canSquadsGetOnCargoPlane = executerObject.GetComponent<Unit>()._CanGetOnAnotherUnit;

            int realWeight = executerObject.GetComponent<Unit>()._CarryWeight;
            if (executerObject.GetComponent<Unit>()._IsCarryingWithTruck)
            {
                if (!IsEvacuatePossible(executerObject.GetComponent<Unit>())) return false;
                foreach (Transform carryingUnit in executerObject.transform.Find("CarryingUnits"))
                {
                    realWeight += carryingUnit.GetComponent<Unit>()._CarryWeight;
                }
            }
            bool canSquadsFit = cargoPlaneSquad != null ? (cargoPlaneSquad._CarryLimit * cargoPlaneSquad._Amount - cargoPlaneSquad._CurrentCarry) >= realWeight : false;

            if (nearestCargoPlane != null && cargoPlaneSquad != null && canSquadsGetOnCargoPlane && canSquadsFit)
            {
                return true;
            }
        }
        return false;
    }
    public bool IsEvacuatePossible(Unit unit)
    {
        if (unit._IsNaval)
            return IsEvacuateShipPossible(unit.gameObject);
        else if (unit._IsTrain)
            return IsEvacuateTrainPossible(unit.gameObject);
        else if (unit._IsAir)
            return IsEvacuateCargoPlanePossible(unit.gameObject);
        else if (unit._Squad is Truck)
            return IsEvacuateTruckPossible(unit);
        return false;
    }

    private bool IsEvacuateTruckPossible(Unit unit)
    {
        foreach (Transform carrying in unit.transform.Find("CarryingUnits"))
        {
            Squad carryingSquad = carrying.GetComponent<Unit>()._Squad;

            if (carryingSquad is Infantry && (carryingSquad as Infantry)._AttachedTruck != null)
                return true;
            if (carryingSquad is Artillery && (carryingSquad as Artillery)._TowedTo != null)
                return true;
            if (carryingSquad is AntiTank && (carryingSquad as AntiTank)._TowedTo != null)
                return true;
            if (carryingSquad is AntiAir && (carryingSquad as AntiAir)._TowedTo != null)
                return true;

        }

        return false;
    }
    private bool IsEvacuateShipPossible(GameObject executerObject)
    {
        if (executerObject.CompareTag("NavalUnit") && executerObject.transform.Find("CarryingUnits").childCount != 0 && executerObject.GetComponent<Unit>()._Squad is TransportShip)
        {
            TransportShip transportShipSquad = executerObject.GetComponent<Unit>()._Squad as TransportShip;
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
        if (executerObject.CompareTag("LandUnit") && executerObject.transform.Find("CarryingUnits").childCount != 0 && executerObject.GetComponent<Unit>()._Squad is Train)
        {
            Train trainSquad = executerObject.GetComponent<Unit>()._Squad as Train;
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
        if (executerObject.CompareTag("LandUnit") && executerObject.transform.Find("CarryingUnits").childCount != 0 && executerObject.GetComponent<Unit>()._Squad is CargoPlane)
        {
            CargoPlane cargoPlaneSquad = executerObject.GetComponent<Unit>()._Squad as CargoPlane;
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