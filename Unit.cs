using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    public bool _IsNaval;
    public bool _IsTrain;
    public bool _IsWheel;
    public bool _IsEnemy { get; private set; }
    public Rigidbody _Rigidbody { get; private set; }
    public List<Vector3> _TargetPositions { get; private set; }
    public float _Speed { get; private set; }

    public GameObject AttachedShip { get; set; }

    private void Awake()
    {
        _Rigidbody = GetComponent<Rigidbody>();
        _TargetPositions = new List<Vector3>();
        _Speed = 50f;
    }
    private void Update()
    {
        if (GameManager._Instance._IsGameStopped) return;

        if (_TargetPositions.Count > 0)
        {
            Vector3 targetPos = _TargetPositions[0];
            MoveToPosition(targetPos);

            if ((new Vector3(targetPos.x, 0f, targetPos.z) - new Vector3(transform.position.x, 0f, transform.position.z)).magnitude < 15f)
            {
                _TargetPositions.RemoveAt(0);
            }
        }
        else
        {
            MoveToPosition(transform.position);
        }
    }

    public void ArrangeCurrentRouteGhost()
    {
        if (_TargetPositions.Count == 0)
        {
            transform.Find("CurrentRouteGhost").gameObject.SetActive(false);
            return;
        }

        transform.Find("CurrentRouteGhost").gameObject.SetActive(true);
        transform.Find("CurrentRouteGhost").GetComponent<LineRenderer>().positionCount = _TargetPositions.Count + 1;

        transform.Find("CurrentRouteGhost").GetComponent<LineRenderer>().SetPosition(0, transform.position + Vector3.up * 10f);
        for (int i = 0; i < _TargetPositions.Count; i++)
        {
            transform.Find("CurrentRouteGhost").GetComponent<LineRenderer>().SetPosition(i + 1, _TargetPositions[i] + Vector3.up * 10f);
        }
    }

    private void MoveToPosition(Vector3 pos)
    {
        Vector3 direction = (pos - transform.position).normalized;
        direction.y = 0f;
        TerrainPoint point = TerrainController._Instance.GetTerrainPointFromObject(transform);
        Vector3 targetVel = Vector3.Lerp(_Rigidbody.linearVelocity, direction * GetUnitSpeed(point), Time.deltaTime * 3f);
        _Rigidbody.linearVelocity = new Vector3(targetVel.x, 0f, targetVel.z);
        _Rigidbody.position = new Vector3(_Rigidbody.position.x, point._Position.y + 10f, _Rigidbody.position.z);
        if (_Rigidbody.linearVelocity.sqrMagnitude > 0.001f)
        {
            Quaternion lookRot = Quaternion.LookRotation(_Rigidbody.linearVelocity.normalized, point._Normal);
            transform.rotation = Quaternion.Lerp(transform.rotation, lookRot, Time.deltaTime * 9f);
        }
        //transform.up = point._Normal;

    }
    private float GetUnitSpeed(TerrainPoint terrainPoint)
    {
        if (terrainPoint._TerrainUpperType == TerrainUpperType.DirtRoad && _IsWheel) return _Speed * 2f;
        else if (terrainPoint._TerrainUpperType == TerrainUpperType.AsphaltRoad && _IsWheel) return _Speed * 4f;
        else if (terrainPoint._TerrainUpperType == TerrainUpperType.RailRoad && _IsTrain) return _Speed * 4f;
        else if (terrainPoint._TerrainUpperType == TerrainUpperType.Bridge) return _Speed * 1.25f;
        else if (terrainPoint._TerrainUpperType == TerrainUpperType.City) return _Speed * 0.7f;
        else if (terrainPoint._TerrainUpperType == TerrainUpperType.Forest) return _Speed * 0.4f;
        else return _Speed;
    }

    public void RemoveOrders()
    {
        _TargetPositions.Clear();
        //remove attack or any orders
    }
}
