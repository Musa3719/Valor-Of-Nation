using UnityEngine;

public class ArrowTextureOffset : MonoBehaviour
{
    private LineRenderer _renderer;
    private Rigidbody _rb;

    private Vector2 offset;
    private void Awake()
    {
        _renderer = GetComponent<LineRenderer>();
        _rb = transform.parent.GetComponent<Rigidbody>();
    }
    void Update()
    {
        offset = _renderer.material.mainTextureOffset;
        offset.x -= (name.StartsWith("Potential") ? 0.125f : (0.04f *_rb.linearVelocity.magnitude)) * Time.deltaTime;
        _renderer.material.mainTextureOffset = offset;
    }
}
