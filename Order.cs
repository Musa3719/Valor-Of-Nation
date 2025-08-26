using System.Collections.Generic;
using UnityEngine;

public abstract class Order
{
    public Vector3 _OrderPosition;
}
public class MoveOrder : Order
{
    public void ArrangeOrderGhostForPlayer(GameObject executerObject, Vector3? firstPos, Vector3? secondPos, bool isPressingShift, Vector3 groundDirection)
    {
        if (executerObject.GetComponent<Unit>() != null)
        {
            if (isPressingShift && executerObject.GetComponent<Unit>()._TargetPositions.Count != 0)
                firstPos = executerObject.GetComponent<Unit>()._TargetPositions[executerObject.GetComponent<Unit>()._TargetPositions.Count - 1];
            TerrainController._Instance.ArrangeMergingLineRenderer(executerObject.transform.Find("PotentialRouteGhost").GetComponent<LineRenderer>(), firstPos.Value, secondPos.Value, groundDirection);

            bool isNavalUnitTouchingTerrain = executerObject.GetComponent<Unit>()._IsNaval && TerrainController._Instance.IsRouteTouchingLandOrWater(executerObject.transform.Find("PotentialRouteGhost").gameObject, LayerMask.NameToLayer("Terrain"));
            bool isLandUnitTouchingWater = !executerObject.GetComponent<Unit>()._IsNaval && TerrainController._Instance.IsRouteTouchingLandOrWater(executerObject.transform.Find("PotentialRouteGhost").gameObject, LayerMask.NameToLayer("Water"));
            if ((isNavalUnitTouchingTerrain || isLandUnitTouchingWater) && !executerObject.GetComponent<Unit>()._IsAir)
                executerObject.transform.Find("PotentialRouteGhost").GetComponent<LineRenderer>().colorGradient = GameManager._Instance._RedGradientForPotentialRouteOrRoad;
            else
                executerObject.transform.Find("PotentialRouteGhost").GetComponent<LineRenderer>().colorGradient = GameManager._Instance._WhiteGradientForPotentialRouteOrRoad;
        }
    }
    public void ExecuteOrder(GameObject executerObject, bool isClearing)
    {
        if (executerObject.GetComponent<Unit>() != null)
        {
            if (executerObject.GetComponent<Unit>()._IsNaval && TerrainController._Instance.IsRouteTouchingLandOrWater(executerObject.transform.Find("PotentialRouteGhost").gameObject, LayerMask.NameToLayer("Terrain"))) return;
            if (!executerObject.GetComponent<Unit>()._IsNaval && !executerObject.GetComponent<Unit>()._IsAir && TerrainController._Instance.IsRouteTouchingLandOrWater(executerObject.transform.Find("PotentialRouteGhost").gameObject, LayerMask.NameToLayer("Water"))) return;

            if (isClearing)
                executerObject.GetComponent<Unit>()._TargetPositions.Clear();
            executerObject.GetComponent<Unit>()._TargetPositions.Add(_OrderPosition);
        }
    }

}


public class GetTowedOrder : Order
{
    public void ExecuteOrder(Squad squad, Truck truck)
    {
        float availableTow = truck._Amount - truck._CurrentTow;
        GameObject usedTruck = null;

        if (squad is Artillery && (squad as Artillery)._TowedTo == null && availableTow >= squad._Amount)
        {
            (squad as Artillery)._TowedTo = truck;
            truck._CurrentTow += squad._Amount;

            usedTruck = truck._AttachedUnit.gameObject;
            GameInputController._Instance.GetCarriedByAnotherUnit(squad._AttachedUnit, truck._AttachedUnit);
        }
        else if (squad is AntiTank && (squad as AntiTank)._TowedTo == null && availableTow >= squad._Amount)
        {
            (squad as AntiTank)._TowedTo = truck;
            truck._CurrentTow += squad._Amount;

            usedTruck = truck._AttachedUnit.gameObject;
            GameInputController._Instance.GetCarriedByAnotherUnit(squad._AttachedUnit, truck._AttachedUnit);
        }
        else if (squad is AntiAir && (squad as AntiAir)._TowedTo == null && availableTow >= squad._Amount)
        {
            (squad as AntiAir)._TowedTo = truck;
            truck._CurrentTow += squad._Amount;

            usedTruck = truck._AttachedUnit.gameObject;
            GameInputController._Instance.GetCarriedByAnotherUnit(squad._AttachedUnit, truck._AttachedUnit);
        }

        if (GameInputController._Instance._SelectedUnits.Count == 0 && usedTruck != null)
            GameInputController._Instance.SelectUnit(usedTruck);
    }
}
public class GetTowedOffOrder : Order
{
    public void ExecuteOrder(Squad squad)
    {
        if (squad is Artillery && (squad as Artillery)._TowedTo != null)
        {
            GameInputController._Instance.GetOutFromAnotherUnit(squad._AttachedUnit, (squad as Artillery)._TowedTo._AttachedUnit.transform.position, (squad as Artillery)._TowedTo._AttachedUnit.gameObject);
            (squad as Artillery)._TowedTo._CurrentTow -= squad._Amount;
            (squad as Artillery)._TowedTo = null;
            GameInputController._Instance.SelectUnit(squad._AttachedUnit.gameObject);
        }
        else if (squad is AntiTank && (squad as AntiTank)._TowedTo != null)
        {
            GameInputController._Instance.GetOutFromAnotherUnit(squad._AttachedUnit, (squad as AntiTank)._TowedTo._AttachedUnit.transform.position, (squad as AntiTank)._TowedTo._AttachedUnit.gameObject);
            (squad as AntiTank)._TowedTo._CurrentTow -= squad._Amount;
            (squad as AntiTank)._TowedTo = null;
            GameInputController._Instance.SelectUnit(squad._AttachedUnit.gameObject);
        }
        else if (squad is AntiAir && (squad as AntiAir)._TowedTo != null)
        {
            GameInputController._Instance.GetOutFromAnotherUnit(squad._AttachedUnit, (squad as AntiAir)._TowedTo._AttachedUnit.transform.position, (squad as AntiAir)._TowedTo._AttachedUnit.gameObject);
            (squad as AntiAir)._TowedTo._CurrentTow -= squad._Amount;
            (squad as AntiAir)._TowedTo = null;
            GameInputController._Instance.SelectUnit(squad._AttachedUnit.gameObject);
        }
    }
}
public class GetOnTruckOrder : Order
{
    public void ExecuteOrder(Squad squad, Truck truck)
    {
        int availableCarry = truck._ManCarryLimit * truck._Amount - truck._CurrentManCarry;
        GameObject usedTruck = null;
        if (squad is Infantry && (squad as Infantry)._AttachedTruck == null && availableCarry >= squad._Amount)
        {
            (squad as Infantry)._AttachedTruck = truck;
            truck._CurrentManCarry += squad._Amount;
            usedTruck = truck._AttachedUnit.gameObject;
            GameInputController._Instance.GetCarriedByAnotherUnit(squad._AttachedUnit, truck._AttachedUnit);
        }

        if (GameInputController._Instance._SelectedUnits.Count == 0 && usedTruck != null)
            GameInputController._Instance.SelectUnit(usedTruck);
    }
}
public class GetOffTruckOrder : Order
{
    public void ExecuteOrder(Squad squad)
    {
        if (squad is Infantry && (squad as Infantry)._AttachedTruck != null)
        {
            GameInputController._Instance.GetOutFromAnotherUnit(squad._AttachedUnit, (squad as Infantry)._AttachedTruck._AttachedUnit.transform.position, (squad as Infantry)._AttachedTruck._AttachedUnit.gameObject);
            (squad as Infantry)._AttachedTruck._CurrentManCarry -= squad._Amount;
            (squad as Infantry)._AttachedTruck = null;
            GameInputController._Instance.SelectUnit(squad._AttachedUnit.gameObject);
        }
    }
}
public class GetOnShipOrder : Order
{
    public void ExecuteOrder(GameObject executerObject)
    {
        if (executerObject.GetComponent<Unit>()._AttachedToUnitObject != null || executerObject.GetComponent<Unit>()._IsAir || executerObject.GetComponent<Unit>()._IsNaval || executerObject.GetComponent<Unit>()._IsTrain)
            return;

        GameObject[] nearestShips = GameInputController._Instance.GetNearestShips(executerObject.transform);
        GameObject usedShip = null;
        foreach (var nearestShip in nearestShips)
        {
            TransportShip transportShipSquad = nearestShip.GetComponent<Unit>()._Squad as TransportShip;
            bool canSquadsGetOnShip = executerObject.GetComponent<Unit>()._CanGetOnAnotherUnit;
            bool canSquadsFit = transportShipSquad != null ? (transportShipSquad._CarryLimit * transportShipSquad._Amount - transportShipSquad._CurrentCarry) >= executerObject.GetComponent<Unit>()._CarryWeight : false;
            if (nearestShip != null && transportShipSquad != null && canSquadsGetOnShip && canSquadsFit)
            {
                GameObject oldCarryingObject = nearestShip.transform.Find("CarryingUnits").childCount > 0 ? nearestShip.transform.Find("CarryingUnits").GetChild(0).gameObject : null;
                usedShip = nearestShip;
                transportShipSquad._CurrentCarry += executerObject.GetComponent<Unit>()._CarryWeight;
                GameInputController._Instance.GetCarriedByAnotherUnit(executerObject.GetComponent<Unit>(), nearestShip.GetComponent<Unit>());
                break;
            }
        }

        if (GameInputController._Instance._SelectedUnits.Count == 0 && usedShip != null)
            GameInputController._Instance.SelectUnit(usedShip);
    }
}
public class EvacuateShipOrder : Order
{
    public void ExecuteOrder(GameObject executerObject)
    {
        List<GameObject> evacuatedUnits = new List<GameObject>();

        if (executerObject.CompareTag("NavalUnit") && executerObject.transform.Find("CarryingUnits").childCount != 0 && executerObject.GetComponent<Unit>()._Squad is TransportShip)
        {
            TransportShip transportShipSquad = executerObject.GetComponent<Unit>()._Squad as TransportShip;
            if (transportShipSquad == null) return;
            Transform[] childs = GameManager._Instance.GetNearChildTransforms(executerObject.transform.Find("CarryingUnits"));
            foreach (Transform carryingUnit in childs)
            {
                Vector3 landingPosition = GameInputController._Instance.GetNearestTerrainPosition(executerObject.transform.position);
                if ((landingPosition - executerObject.transform.position).magnitude > 25f) return;

                if (carryingUnit.GetComponent<Unit>()._AttachedToUnitObject != null)
                {
                    evacuatedUnits.Add(carryingUnit.gameObject);
                    transportShipSquad._CurrentCarry -= carryingUnit.GetComponent<Unit>()._CarryWeight;
                    GameInputController._Instance.GetOutFromAnotherUnit(carryingUnit.GetComponent<Unit>(), landingPosition, executerObject);
                }
            }
        }

        if (GameInputController._Instance._SelectedUnits.Count == 0)
        {
            foreach (var evacUnit in evacuatedUnits)
            {
                GameInputController._Instance.SelectUnit(evacUnit);
            }
        }

    }
}
public class GetOnTrainOrder : Order
{
    public void ExecuteOrder(GameObject executerObject)
    {
        if (executerObject.GetComponent<Unit>()._AttachedToUnitObject != null || executerObject.GetComponent<Unit>()._IsAir || executerObject.GetComponent<Unit>()._IsNaval || executerObject.GetComponent<Unit>()._IsTrain)
            return;

        GameObject usedTrain = null;
        GameObject[] nearestTrains = GameInputController._Instance.GetNearestTrains(executerObject.transform);
        foreach (var nearestTrain in nearestTrains)
        {
            Train trainSquad = nearestTrain.GetComponent<Unit>()._Squad as Train;
            bool canSquadsGetOnTrain = executerObject.GetComponent<Unit>()._CanGetOnAnotherUnit;
            bool canSquadsFit = trainSquad != null ? (trainSquad._CarryLimit * trainSquad._Amount - trainSquad._CurrentCarry) >= executerObject.GetComponent<Unit>()._CarryWeight : false;
            if (nearestTrain != null && trainSquad != null && canSquadsGetOnTrain && canSquadsFit)
            {
                GameObject oldCarryingObject = nearestTrain.transform.Find("CarryingUnits").childCount > 0 ? nearestTrain.transform.Find("CarryingUnits").GetChild(0).gameObject : null;
                usedTrain = nearestTrain;
                trainSquad._CurrentCarry += executerObject.GetComponent<Unit>()._CarryWeight;
                GameInputController._Instance.GetCarriedByAnotherUnit(executerObject.GetComponent<Unit>(), nearestTrain.GetComponent<Unit>());
                break;
            }
        }

        if (GameInputController._Instance._SelectedUnits.Count == 0 && usedTrain != null)
            GameInputController._Instance.SelectUnit(usedTrain);
    }
}
public class EvacuateTrainOrder : Order
{
    public void ExecuteOrder(GameObject executerObject)
    {
        List<GameObject> evacuatedUnits = new List<GameObject>();

        if (executerObject.CompareTag("LandUnit") && executerObject.transform.Find("CarryingUnits").childCount != 0 && executerObject.GetComponent<Unit>()._IsTrain)
        {
            Train trainSquad = executerObject.GetComponent<Unit>()._Squad as Train;
            if (trainSquad == null) return;
            Transform[] childs = GameManager._Instance.GetNearChildTransforms(executerObject.transform.Find("CarryingUnits"));
            foreach (Transform carryingUnit in childs)
            {
                Vector3 landingPosition = GameInputController._Instance.GetNearestTerrainPosition(executerObject.transform.position);
                if ((landingPosition - executerObject.transform.position).magnitude > 25f) return;

                if (carryingUnit.GetComponent<Unit>()._AttachedToUnitObject != null)
                {
                    evacuatedUnits.Add(carryingUnit.gameObject);
                    trainSquad._CurrentCarry -= carryingUnit.GetComponent<Unit>()._CarryWeight;
                    GameInputController._Instance.GetOutFromAnotherUnit(carryingUnit.GetComponent<Unit>(), landingPosition, executerObject);
                }
            }
        }

        if (GameInputController._Instance._SelectedUnits.Count == 0)
        {
            foreach (var evacUnit in evacuatedUnits)
            {
                GameInputController._Instance.SelectUnit(evacUnit);
            }
        }
    }
}

public class GetOnCargoPlaneOrder : Order
{
    public void ExecuteOrder(GameObject executerObject)
    {
        if (executerObject.GetComponent<Unit>()._AttachedToUnitObject != null || executerObject.GetComponent<Unit>()._IsAir || executerObject.GetComponent<Unit>()._IsNaval || executerObject.GetComponent<Unit>()._IsTrain)
            return;

        GameObject usedTrain = null;
        GameObject[] nearestCargoPlanes = GameInputController._Instance.GetNearestCargoPlanes(executerObject.transform);
        foreach (var nearestCargoPlane in nearestCargoPlanes)
        {
            CargoPlane cargoPlaneSquad = nearestCargoPlane.GetComponent<Unit>()._Squad as CargoPlane;
            bool canSquadsGetOnCargoPlane = executerObject.GetComponent<Unit>()._CanGetOnAnotherUnit;
            bool canSquadsFit = cargoPlaneSquad != null ? (cargoPlaneSquad._CarryLimit * cargoPlaneSquad._Amount - cargoPlaneSquad._CurrentCarry) >= executerObject.GetComponent<Unit>()._CarryWeight : false;
            if (nearestCargoPlane != null && cargoPlaneSquad != null && canSquadsGetOnCargoPlane && canSquadsFit)
            {
                GameObject oldCarryingObject = nearestCargoPlane.transform.Find("CarryingUnits").childCount > 0 ? nearestCargoPlane.transform.Find("CarryingUnits").GetChild(0).gameObject : null;
                usedTrain = nearestCargoPlane;
                cargoPlaneSquad._CurrentCarry += executerObject.GetComponent<Unit>()._CarryWeight;
                GameInputController._Instance.GetCarriedByAnotherUnit(executerObject.GetComponent<Unit>(), nearestCargoPlane.GetComponent<Unit>());
                break;
            }
        }

        if (GameInputController._Instance._SelectedUnits.Count == 0 && usedTrain != null)
            GameInputController._Instance.SelectUnit(usedTrain);
    }
}
public class EvacuateCargoPlaneOrder : Order
{
    public void ExecuteOrder(GameObject executerObject)
    {
        List<GameObject> evacuatedUnits = new List<GameObject>();

        if (executerObject.CompareTag("LandUnit") && executerObject.transform.Find("CarryingUnits").childCount != 0 && executerObject.GetComponent<Unit>()._Squad is CargoPlane)
        {
            CargoPlane cargoPlaneSquad = executerObject.GetComponent<Unit>()._Squad as CargoPlane;
            if (cargoPlaneSquad == null) return;
            Transform[] childs = GameManager._Instance.GetNearChildTransforms(executerObject.transform.Find("CarryingUnits"));
            foreach (Transform carryingUnit in childs)
            {
                Vector3 landingPosition = GameInputController._Instance.GetNearestTerrainPosition(executerObject.transform.position);
                if ((landingPosition - executerObject.transform.position).magnitude > 25f) return;

                if (carryingUnit.GetComponent<Unit>()._AttachedToUnitObject != null)
                {
                    evacuatedUnits.Add(carryingUnit.gameObject);
                    cargoPlaneSquad._CurrentCarry -= carryingUnit.GetComponent<Unit>()._CarryWeight;
                    GameInputController._Instance.GetOutFromAnotherUnit(carryingUnit.GetComponent<Unit>(), landingPosition, executerObject);
                }
            }
        }

        if (GameInputController._Instance._SelectedUnits.Count == 0)
        {
            foreach (var evacUnit in evacuatedUnits)
            {
                GameInputController._Instance.SelectUnit(evacUnit);
            }
        }
    }
}