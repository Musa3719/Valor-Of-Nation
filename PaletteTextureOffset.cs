using UnityEngine;

public class PaletteTextureOffset : MonoBehaviour
{
    private Renderer _renderer;
    private Rigidbody _rb;
    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _rb = transform.parent.parent.parent.parent.parent.GetComponent<Rigidbody>();
    }
    void Update()
    {
        Vector2 offset = _renderer.material.mainTextureOffset;
        offset.x -= _rb.linearVelocity.magnitude * 0.32f * Time.deltaTime;
        _renderer.material.mainTextureOffset = offset;
    }
}
