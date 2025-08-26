using UnityEngine;
using System.Collections;

public class AirUnitColliderAfterDeath : MonoBehaviour
{
    public Animator _Animator;
    private bool _isCollided;
    private float _speed;
    private void OnCollisionEnter(Collision collision)
    {
        if (!_isCollided && collision != null && collision.collider != null && (collision.collider.name.StartsWith("Terrain") || collision.collider.gameObject.layer == LayerMask.NameToLayer("Water")))
        {
            _isCollided = true;

            GameManager._Instance.SpawnVFX(GameManager._Instance._GroundExplosionVFXPrefab, transform.Find("Model").GetComponentInChildren<UnitModel>().transform.position, TerrainController._Instance.GetTerrainPointFromObject(transform)._Normal, scale: 4.5f);

            _Animator.SetBool("IsMoving", false);
            StartCoroutine(MoveSpeedToZero());
        }
    }
    public IEnumerator MoveSpeedToZero()
    {
        _speed = _Animator.GetFloat("Speed");
        while (_speed > 0.0001f)
        {
            _Animator.SetFloat("Speed", _speed);
            _speed = Mathf.Lerp(_speed, 0f, Time.deltaTime * 2f);
            yield return null;
        }
    }
}
