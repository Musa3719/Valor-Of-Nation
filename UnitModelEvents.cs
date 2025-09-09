using UnityEngine;

public class UnitModelEvents : MonoBehaviour
{
    public void CreateFireSmoke()
    {
        ParticleSystem[] effectParents = GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem fx in effectParents)
        {
            if (fx.name.StartsWith("Muzzle"))
                GameManager._Instance.SpawnVFX(GameManager._Instance._FireSmokeVFXPrefab, fx.transform.position, fx.transform.up, transform.parent.parent.parent.GetComponent<Unit>()._IsNaval ? 0.25f : 1f);
        }
    }
}
