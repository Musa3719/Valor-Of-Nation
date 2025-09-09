using System.Collections.Generic;
using UnityEngine;

public class BridgeUnitController : MonoBehaviour
{
    public RiverController _AttachedRiver;
    public List<Collider> _UnitColliders;

    private void Start()
    {
        _UnitColliders = new List<Collider>();
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other != null && other.gameObject.layer == LayerMask.NameToLayer("Unit") && !_UnitColliders.Contains(other))
        {
            _UnitColliders.Add(other);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other != null && other.gameObject.layer == LayerMask.NameToLayer("Unit") && _UnitColliders.Contains(other))
        {
            _UnitColliders.Remove(other);
        }
    }

    public bool IsCarryingAnyUnit()
    {
        if (_UnitColliders.Count > 0) return true;
        return false;
    }
}
