using System.Collections.Generic;
using UnityEngine;

public class BridgeEdgeDetection : MonoBehaviour
{
    public List<Collider> _Colliders;
    private void Awake()
    {
        _Colliders = new List<Collider>();
    }
    private void OnTriggerEnter(Collider other)
    {
        if(other!=null && other.gameObject.layer == LayerMask.NameToLayer("Water") && !_Colliders.Contains(other))
        {
            _Colliders.Add(other);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other != null && other.gameObject.layer == LayerMask.NameToLayer("Water") && _Colliders.Contains(other))
        {
            _Colliders.Remove(other);
        }
    }
}
