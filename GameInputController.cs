using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameInputController : MonoBehaviour
{
    public static GameInputController _Instance;
    public List<GameObject> _SelectedObjects { get; private set; }
    public Order _CurrentPlayerOrder { get; private set; }


    private void Awake()
    {
        _Instance = this;
        _SelectedObjects = new List<GameObject>();
        _CurrentPlayerOrder = new MoveOrder();
    }
    private void Update()
    {
        if (GameManager._Instance._IsGameStopped) return;

        ArrangeRouteGhost();
        ArrangeSelectedObjects();
        ArrangeOrderToSelectedObjects();
    }

    private void ArrangeRouteGhost()
    {
        foreach (var obj in _SelectedObjects)
        {
            obj.GetComponent<Unit>().ArrangeCurrentRouteGhost();

            if (_CurrentPlayerOrder is MoveOrder)
                obj.transform.Find("PotentialRouteGhost").gameObject.SetActive(true);
            else
                obj.transform.Find("PotentialRouteGhost").gameObject.SetActive(false);

            _CurrentPlayerOrder.ArrangeOrderGhostForPlayer(obj, obj.transform.position, _CurrentPlayerOrder._OrderPosition, isPressingShiftForMoveOrder: GameManager._Instance._InputActions.FindAction("Sprint").ReadValue<float>() != 0f || GameManager._Instance._InputActions.FindAction("Alt").ReadValue<float>() != 0f);
        }
    }
    private void ArrangeOrderToSelectedObjects()
    {
        Physics.Raycast(Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue()), out RaycastHit hit, 30000f, GameManager._Instance._TerrainRayLayers);
        if (hit.collider != null)
            _CurrentPlayerOrder._OrderPosition = GameManager._Instance._InputActions.FindAction("Alt").ReadValue<float>() == 0 ? hit.point : TerrainController._Instance.GetClosestPointToRoad(TerrainController._Instance.GetTerrainPointFromMouse()._Position);
        else
            return;

        if (!Mouse.current.rightButton.wasPressedThisFrame) return;

        foreach (var obj in _SelectedObjects)
        {
            _CurrentPlayerOrder.ExecuteOrder(obj, isClearingForMoveOrder: GameManager._Instance._InputActions.FindAction("Sprint").ReadValue<float>() == 0f && GameManager._Instance._InputActions.FindAction("Alt").ReadValue<float>() == 0f);
        }
    }
    private void ArrangeSelectedObjects()
    {
        if (GameManager._Instance._ConstructionScreen.activeInHierarchy)
        {
            if (_SelectedObjects.Count > 0)
                _SelectedObjects.ClearSelected();
            return;
        }

        Physics.Raycast(Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue()), out RaycastHit hit, 30000f, LayerMask.GetMask("Unit"));
        if (hit.collider != null && hit.collider.GetComponent<Unit>() != null && !hit.collider.GetComponent<Unit>()._IsEnemy)
        {
            //hover
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (GameManager._Instance._InputActions.FindAction("Sprint").ReadValue<float>() == 0f || _SelectedObjects[0].GetComponent<Unit>()._IsNaval != hit.collider.GetComponent<Unit>()._IsNaval)
                    _SelectedObjects.ClearSelected();
                if (!_SelectedObjects.Contains(hit.collider.gameObject))
                    _SelectedObjects.Add(hit.collider.gameObject);
            }
        }
        else
        {
            //remove hover
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                _SelectedObjects.ClearSelected();
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

                Physics.Raycast(checkPos, Vector3.down, out RaycastHit hit, 200f * 2, GameManager._Instance._TerrainRayLayers);
                if (hit.collider != null && hit.collider.gameObject.layer == LayerMask.NameToLayer("Terrain"))
                    return hit.point;
            }
        }

        return Vector3.negativeInfinity;
    }
}