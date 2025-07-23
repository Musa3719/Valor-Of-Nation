using System.Collections.Generic;
using UnityEngine;

public class BridgeConstructionGhost : MonoBehaviour
{
    public List<Collider> _TouchingColliders;
    private void Awake()
    {
        _TouchingColliders = new List<Collider>();
        GetComponent<MeshRenderer>().enabled = false;
        transform.localScale *= 1.15f;
        transform.Find("RealPlaceMesh").localScale *= 0.85f;
        transform.Find("CannotPlaceMesh").localScale *= 0.85f;
        transform.Find("EdgeDetectCollider_1").localScale *= 0.85f;
        transform.Find("EdgeDetectCollider_1").localPosition *= 0.85f;
        transform.Find("EdgeDetectCollider_2").localScale *= 0.85f;
        transform.Find("EdgeDetectCollider_2").localPosition *= 0.85f;
        transform.Find("CannotPlaceMesh").gameObject.SetActive(true);
    }
    private void Update()
    {
        foreach (var collider in _TouchingColliders)
        {
            if (collider == null) _TouchingColliders.Remove(collider);
            break;
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other != null && !(other is TerrainCollider) && !_TouchingColliders.Contains(other))
            _TouchingColliders.Add(other);
    }
    private void OnTriggerExit(Collider other)
    {
        if (other != null && !(other is TerrainCollider) && _TouchingColliders.Contains(other))
            _TouchingColliders.Remove(other);
    }
}
