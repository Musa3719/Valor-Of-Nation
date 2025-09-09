using UnityEngine;
using UnityEngine.VFX;

public class PlayVFXOnEnable : MonoBehaviour
{
    private VisualEffect _vfx;
    private void OnEnable()
    {
        if (_vfx == null)
            _vfx = GetComponent<VisualEffect>();

        _vfx.Play();
    }
}
