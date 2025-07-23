using System.Collections.Generic;
using UnityEngine;

public class BridgeUnitController : MonoBehaviour
{
    public List<Collider> _UnitColliders;

    private void Start()
    {
        _UnitColliders = new List<Collider>();
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider != null && collision.collider.gameObject.layer == LayerMask.NameToLayer("Unit") && !_UnitColliders.Contains(collision.collider))
        {
            _UnitColliders.Add(collision.collider);
        }
    }
    private void OnCollisionExit(Collision collision)
    {
        if (collision.collider != null && collision.collider.gameObject.layer == LayerMask.NameToLayer("Unit") && _UnitColliders.Contains(collision.collider))
        {
            _UnitColliders.Remove(collision.collider);
        }
    }
   

    public bool IsCarryingAnyUnit()
    {
        if (_UnitColliders.Count > 0) return true;
        return false;
    }
}
