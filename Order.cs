using System.Collections.Generic;
using UnityEngine;

public abstract class Order
{
    public Vector3 _OrderPosition;
}
public class MoveOrder : Order
{
    public void ArrangeOrderGhostForPlayer(GameObject executerObject, Vector3? firstPos, Vector3? secondPos, bool isPressingShift)
    {
        if (executerObject.GetComponent<Unit>() != null)
        {
            if (isPressingShift && executerObject.GetComponent<Unit>()._TargetPositions.Count != 0)
                firstPos = executerObject.GetComponent<Unit>()._TargetPositions[executerObject.GetComponent<Unit>()._TargetPositions.Count - 1];
            TerrainController._Instance.ArrangeMergingLineRenderer(executerObject.transform.Find("PotentialRouteGhost").GetComponent<LineRenderer>(), firstPos.Value, secondPos.Value, 1f);

            bool isNavalUnitTouchingTerrain = executerObject.GetComponent<Unit>()._IsNaval && TerrainController._Instance.IsRouteTouchingLandOrWater(executerObject.transform.Find("PotentialRouteGhost").gameObject, LayerMask.NameToLayer("Terrain"));
            bool isLandUnitTouchingWater = !executerObject.GetComponent<Unit>()._IsNaval && TerrainController._Instance.IsRouteTouchingLandOrWater(executerObject.transform.Find("PotentialRouteGhost").gameObject, LayerMask.NameToLayer("Water"));
            if (isNavalUnitTouchingTerrain || isLandUnitTouchingWater)
                executerObject.transform.Find("PotentialRouteGhost").GetComponent<LineRenderer>().material = TerrainController._Instance._RoadRedGhostMat;
            else
                executerObject.transform.Find("PotentialRouteGhost").GetComponent<LineRenderer>().material = TerrainController._Instance._RoadWhiteGhostMat;
        }
    }
    public void ExecuteOrder(GameObject executerObject, bool isClearing)
    {
        if (executerObject.GetComponent<Unit>() != null)
        {
            if (executerObject.GetComponent<Unit>()._IsNaval && TerrainController._Instance.IsRouteTouchingLandOrWater(executerObject.transform.Find("PotentialRouteGhost").gameObject, LayerMask.NameToLayer("Terrain"))) return;
            if (!executerObject.GetComponent<Unit>()._IsNaval && TerrainController._Instance.IsRouteTouchingLandOrWater(executerObject.transform.Find("PotentialRouteGhost").gameObject, LayerMask.NameToLayer("Water"))) return;

            if (isClearing)
                executerObject.GetComponent<Unit>()._TargetPositions.Clear();
            executerObject.GetComponent<Unit>()._TargetPositions.Add(_OrderPosition);
        }
    }

}


public class GetTowedOrder : Order
{
    public void ExecuteOrder(Squad squad)
    {
        Truck truck = GameManager._Instance.GetThisTypeOfSquad<Truck>(squad._AttachedUnit.gameObject);
        float availableTow = truck._Amount - truck._CurrentTow;
        availableTow = truck._Amount - truck._CurrentTow;
        if (squad is Artillery && (squad as Artillery)._TowedTo == null && availableTow >= squad._Amount)
        {
            (squad as Artillery)._TowedTo = truck;
            truck._CurrentTow += squad._Amount;
        }
        else if (squad is AntiTank && (squad as AntiTank)._TowedTo == null && availableTow >= squad._Amount)
        {
            (squad as AntiTank)._TowedTo = truck;
            truck._CurrentTow += squad._Amount;
        }
        else if (squad is AntiAir && (squad as AntiAir)._TowedTo == null && availableTow >= squad._Amount)
        {
            (squad as AntiAir)._TowedTo = truck;
            truck._CurrentTow += squad._Amount;
        }
    }
}
public class GetTowedOffOrder : Order
{
    public void ExecuteOrder(Squad squad)
    {
        Truck truck = GameManager._Instance.GetThisTypeOfSquad<Truck>(squad._AttachedUnit.gameObject);
        if (squad is Artillery && (squad as Artillery)._TowedTo != null)
        {
            (squad as Artillery)._TowedTo = null;
            truck._CurrentTow -= squad._Amount;
        }
        else if (squad is AntiTank && (squad as AntiTank)._TowedTo != null)
        {
            (squad as AntiTank)._TowedTo = null;
            truck._CurrentTow -= squad._Amount;
        }
        else if (squad is AntiAir && (squad as AntiAir)._TowedTo != null)
        {
            (squad as AntiAir)._TowedTo = null;
            truck._CurrentTow -= squad._Amount;
        }
    }
}
public class GetOnTruckOrder : Order
{
    public void ExecuteOrder(Squad squad)
    {
        Truck truck = GameManager._Instance.GetThisTypeOfSquad<Truck>(squad._AttachedUnit.gameObject);
        int availableCarry = truck._ManCarryLimit * truck._Amount - truck._CurrentManCarry;
        if (squad is Infantry && (squad as Infantry)._AttachedTruck == null && availableCarry >= squad._Amount)
        {
            (squad as Infantry)._AttachedTruck = truck;
            truck._CurrentManCarry += squad._Amount;
        }
    }
}
public class GetOffTruckOrder : Order
{
    public void ExecuteOrder(Squad squad)
    {
        Truck truck = GameManager._Instance.GetThisTypeOfSquad<Truck>(squad._AttachedUnit.gameObject);
        if (squad is Infantry && (squad as Infantry)._AttachedTruck != null)
        {
            (squad as Infantry)._AttachedTruck = null;
            truck._CurrentManCarry -= squad._Amount;
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
            TransportShip transportShipSquad = nearestShip.GetComponent<Unit>()._Squads.GetSquadThisType<TransportShip>();
            bool canSquadsGetOnShip = executerObject.GetComponent<Unit>()._CanGetOnAnotherUnit;
            bool canSquadsFit = transportShipSquad != null ? (transportShipSquad._CarryLimit * transportShipSquad._Amount - transportShipSquad._CurrentCarry) >= executerObject.GetComponent<Unit>()._CarryWeight : false;
            if (nearestShip != null && transportShipSquad != null && canSquadsGetOnShip && canSquadsFit)
            {
                GameObject oldCarryingObject = nearestShip.transform.Find("CarryingUnits").childCount > 0 ? nearestShip.transform.Find("CarryingUnits").GetChild(0).gameObject : null;
                usedShip = nearestShip;
                transportShipSquad._CurrentCarry += executerObject.GetComponent<Unit>()._CarryWeight;
                executerObject.GetComponent<Unit>()._AttachedToUnitObject = nearestShip;
                executerObject.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
                executerObject.transform.SetParent(nearestShip.transform.Find("CarryingUnits"));
                executerObject.transform.position = nearestShip.transform.position;
                executerObject.gameObject.SetActive(false);
                GameInputController._Instance.DeSelectUnit(executerObject);

                if (oldCarryingObject != null && oldCarryingObject.GetComponent<Unit>() != null)
                    oldCarryingObject.GetComponent<Unit>().MergeWithAnotherUnit(executerObject.GetComponent<Unit>());

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

        if (executerObject.CompareTag("NavalUnit") && executerObject.transform.Find("CarryingUnits").childCount != 0)
        {
            TransportShip transportShipSquad = executerObject.GetComponent<Unit>()._Squads.GetSquadThisType<TransportShip>();
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
                    carryingUnit.GetComponent<Unit>()._AttachedToUnitObject = null;
                    carryingUnit.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotation;
                    carryingUnit.transform.parent = null;
                    carryingUnit.transform.position = landingPosition;
                    carryingUnit.gameObject.SetActive(true);
                    GameInputController._Instance.DeSelectUnit(executerObject);
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
            Train trainSquad = nearestTrain.GetComponent<Unit>()._Squads.GetSquadThisType<Train>();
            bool canSquadsGetOnTrain = executerObject.GetComponent<Unit>()._CanGetOnAnotherUnit;
            bool canSquadsFit = trainSquad != null ? (trainSquad._CarryLimit * trainSquad._Amount - trainSquad._CurrentCarry) >= executerObject.GetComponent<Unit>()._CarryWeight : false;
            if (nearestTrain != null && trainSquad != null && canSquadsGetOnTrain && canSquadsFit)
            {
                GameObject oldCarryingObject = nearestTrain.transform.Find("CarryingUnits").childCount > 0 ? nearestTrain.transform.Find("CarryingUnits").GetChild(0).gameObject : null;
                usedTrain = nearestTrain;
                trainSquad._CurrentCarry += executerObject.GetComponent<Unit>()._CarryWeight;
                executerObject.GetComponent<Unit>()._AttachedToUnitObject = nearestTrain;
                executerObject.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
                executerObject.transform.SetParent(nearestTrain.transform.Find("CarryingUnits"));
                executerObject.transform.position = nearestTrain.transform.position;
                executerObject.gameObject.SetActive(false);
                GameInputController._Instance.DeSelectUnit(executerObject);

                if (oldCarryingObject != null && oldCarryingObject.GetComponent<Unit>() != null)
                    oldCarryingObject.GetComponent<Unit>().MergeWithAnotherUnit(executerObject.GetComponent<Unit>());

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

        if (executerObject.CompareTag("LandUnit") && executerObject.transform.Find("CarryingUnits").childCount != 0)
        {
            Train trainSquad = executerObject.GetComponent<Unit>()._Squads.GetSquadThisType<Train>();
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
                    carryingUnit.GetComponent<Unit>()._AttachedToUnitObject = null;
                    carryingUnit.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotation;
                    carryingUnit.transform.parent = null;
                    carryingUnit.transform.position = landingPosition;
                    carryingUnit.gameObject.SetActive(true);
                    GameInputController._Instance.DeSelectUnit(executerObject);
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
            CargoPlane cargoPlaneSquad = nearestCargoPlane.GetComponent<Unit>()._Squads.GetSquadThisType<CargoPlane>();
            bool canSquadsGetOnCargoPlane = executerObject.GetComponent<Unit>()._CanGetOnAnotherUnit;
            bool canSquadsFit = cargoPlaneSquad != null ? (cargoPlaneSquad._CarryLimit * cargoPlaneSquad._Amount - cargoPlaneSquad._CurrentCarry) >= executerObject.GetComponent<Unit>()._CarryWeight : false;
            if (nearestCargoPlane != null && cargoPlaneSquad != null && canSquadsGetOnCargoPlane && canSquadsFit)
            {
                GameObject oldCarryingObject = nearestCargoPlane.transform.Find("CarryingUnits").childCount > 0 ? nearestCargoPlane.transform.Find("CarryingUnits").GetChild(0).gameObject : null;
                usedTrain = nearestCargoPlane;
                cargoPlaneSquad._CurrentCarry += executerObject.GetComponent<Unit>()._CarryWeight;
                executerObject.GetComponent<Unit>()._AttachedToUnitObject = nearestCargoPlane;
                executerObject.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
                executerObject.transform.SetParent(nearestCargoPlane.transform.Find("CarryingUnits"));
                executerObject.transform.position = nearestCargoPlane.transform.position;
                executerObject.gameObject.SetActive(false);
                GameInputController._Instance.DeSelectUnit(executerObject);

                if (oldCarryingObject != null && oldCarryingObject.GetComponent<Unit>() != null)
                    oldCarryingObject.GetComponent<Unit>().MergeWithAnotherUnit(executerObject.GetComponent<Unit>());

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

        if (executerObject.CompareTag("LandUnit") && executerObject.transform.Find("CarryingUnits").childCount != 0)
        {
            Train cargoPlaneSquad = executerObject.GetComponent<Unit>()._Squads.GetSquadThisType<Train>();
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
                    carryingUnit.GetComponent<Unit>()._AttachedToUnitObject = null;
                    carryingUnit.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotation;
                    carryingUnit.transform.parent = null;
                    carryingUnit.transform.position = landingPosition;
                    carryingUnit.gameObject.SetActive(true);
                    GameInputController._Instance.DeSelectUnit(executerObject);
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