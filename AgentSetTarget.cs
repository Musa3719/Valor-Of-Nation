using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class AgentSetTarget : MonoBehaviour
{
    [SerializeField] private Transform _targetTransform;

    private NavMeshAgent _agent;
    private Coroutine _moveToCoroutine;
    private Vector3 _lastViablePosition;
    private Terrain _targetTerrain;
    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
    }
    private void Update()
    {

        if (IsInWrongNavmesh())
        {
            _agent.enabled = false;
            transform.position = _lastViablePosition;
            _agent.enabled = true;
        }
        else
        {
            _lastViablePosition = transform.position;
            MoveToPosition(_targetTransform.position);
        }
    }
    private void MoveToPosition(Vector3 pos)
    {
        _agent.SetDestination(pos);
    }
    private bool IsInWrongNavmesh()
    {
        if (!_agent.isOnNavMesh) return true;

        if (_agent.agentTypeID != 0)
        {
            Physics.Raycast(new Vector3(transform.position.x, 30000f, transform.position.z), -Vector3.up, out RaycastHit hit, 40000f, 1 << LayerMask.NameToLayer("Terrain"));
            if (hit.collider != null && transform.position.y < hit.collider.GetComponent<Terrain>().SampleHeight(transform.position))
            {
                Debug.Log("Wrong Terrain!");
                return true;
            }
        }
        else if (_agent.agentTypeID == 0)
        {
            if (transform.position.y < 359f)
            {
                Debug.Log("Wrong Terrain!");
                return true;
            }
        }

        return false;
    }
}
