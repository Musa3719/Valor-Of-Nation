using UnityEngine;

public abstract class Order
{
    public Vector3 _OrderPosition;
    public abstract void ExecuteOrder(GameObject executerObject, bool isClearingForMoveOrder = false);
    public abstract void ArrangeOrderGhostForPlayer(GameObject executerObject, Vector3? firstPosForMoveOrder = null, Vector3? secondPosForMoveOrder = null, bool isPressingShiftForMoveOrder = false);
}
public class MoveOrder : Order
{
    public override void ArrangeOrderGhostForPlayer(GameObject executerObject, Vector3? firstPos, Vector3? secondPos, bool isPressingShift)
    {
        if (executerObject.GetComponent<Unit>() != null)
        {
            if (!isPressingShift || executerObject.GetComponent<Unit>()._TargetPositions.Count == 0)
            {
                executerObject.transform.Find("PotentialRouteGhost").GetComponent<LineRenderer>().positionCount = 2;

                firstPos += Vector3.up * 5f;
                secondPos += Vector3.up * 10f;
                executerObject.transform.Find("PotentialRouteGhost").GetComponent<LineRenderer>().SetPosition(0, firstPos.Value);
                executerObject.transform.Find("PotentialRouteGhost").GetComponent<LineRenderer>().SetPosition(1, secondPos.Value);
            }
            else
            {
                executerObject.transform.Find("PotentialRouteGhost").GetComponent<LineRenderer>().positionCount = executerObject.transform.Find("CurrentRouteGhost").GetComponent<LineRenderer>().positionCount + 1;

                for (int i = 0; i < executerObject.transform.Find("CurrentRouteGhost").GetComponent<LineRenderer>().positionCount; i++)
                {
                    executerObject.transform.Find("PotentialRouteGhost").GetComponent<LineRenderer>().SetPosition(i, executerObject.transform.Find("CurrentRouteGhost").GetComponent<LineRenderer>().GetPosition(i));
                }
                executerObject.transform.Find("PotentialRouteGhost").GetComponent<LineRenderer>().SetPosition(executerObject.transform.Find("PotentialRouteGhost").GetComponent<LineRenderer>().positionCount - 1, secondPos.Value + Vector3.up * 10f);
            }

            if (executerObject.GetComponent<Unit>()._IsNaval && TerrainController._Instance.IsRouteTouchingLandOrWater(executerObject.transform.Find("PotentialRouteGhost").gameObject, LayerMask.NameToLayer("Terrain")))
                executerObject.transform.Find("PotentialRouteGhost").GetComponent<LineRenderer>().material = TerrainController._Instance._RoadRedGhostMat;
            else if (!executerObject.GetComponent<Unit>()._IsNaval && TerrainController._Instance.IsRouteTouchingLandOrWater(executerObject.transform.Find("PotentialRouteGhost").gameObject, LayerMask.NameToLayer("Water")))
                executerObject.transform.Find("PotentialRouteGhost").GetComponent<LineRenderer>().material = TerrainController._Instance._RoadRedGhostMat;
            else
                executerObject.transform.Find("PotentialRouteGhost").GetComponent<LineRenderer>().material = TerrainController._Instance._RoadWhiteGhostMat;
        }
    }
    public override void ExecuteOrder(GameObject executerObject, bool isClearing)
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

public class GetOnShipOrder : Order
{
    public override void ArrangeOrderGhostForPlayer(GameObject executerObject, Vector3? firstPosForMoveOrder = null, Vector3? secondPosForMoveOrder = null, bool isPressingShift = false)
    {

    }

    public override void ExecuteOrder(GameObject executerObject, bool isClearingForMoveOrder = false)
    {
        GameObject nearestShip = GameInputController._Instance.GetNearestShip(executerObject.transform);
        if (nearestShip != null)
        {
            executerObject.GetComponent<Unit>().AttachedShip = nearestShip;
            executerObject.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
            executerObject.transform.SetParent(nearestShip.transform);
            executerObject.transform.position = nearestShip.transform.position;
            executerObject.gameObject.SetActive(false);
            GameInputController._Instance._SelectedObjects.ClearSelected();
        }
    }
}
public class EvacuateShipOrder : Order
{
    public override void ArrangeOrderGhostForPlayer(GameObject executerObject, Vector3? firstPosForMoveOrder = null, Vector3? secondPosForMoveOrder = null, bool isPressingShift = false)
    {

    }

    public override void ExecuteOrder(GameObject executerObject, bool isClearingForMoveOrder = false)
    {
        Vector3 landingPosition = GameInputController._Instance.GetNearestTerrainPosition(executerObject.transform.position);
        if ((landingPosition - executerObject.transform.position).magnitude > 1000f) return;

        foreach (Transform item in executerObject.transform)
        {
            if (item.CompareTag("LandUnit") && item.GetComponent<Unit>().AttachedShip != null)
            {
                item.GetComponent<Unit>().AttachedShip = null;
                item.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotation;
                item.transform.parent = null;
                item.transform.position = landingPosition;
                item.gameObject.SetActive(true);
            }
        }
    }


}