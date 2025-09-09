using UnityEngine;

public class Damage : MonoBehaviour
{
    public Squad _AttackerSquadType;
    public int _AttackerSquadAmount;
    public float _AttackerSquadReconEfficiency;//0 to 1
    public void TakeDamage(Squad damageTakeSquad)
    {
        float damage = GameManager._Instance._DamageMatrix[SquadToIndex(_AttackerSquadType)][SquadToIndex(damageTakeSquad)] * _AttackerSquadAmount * _AttackerSquadReconEfficiency;
        damageTakeSquad._Amount -= (int)(damage / damageTakeSquad._HealthForOneAmount);
        if (Random.Range(0, 101) <= (damage % damageTakeSquad._HealthForOneAmount / damageTakeSquad._HealthForOneAmount * 100))
            damageTakeSquad._Amount -= 1;

        GameObject prefab = null;
        if (_AttackerSquadType is RocketArtillery || _AttackerSquadType is Jet)
            prefab = GameManager._Instance._BombExplosionVFXPrefab;
        else if (_AttackerSquadType is Artillery)
            prefab = GameManager._Instance._ArtilleryHitVFXPrefab;
        else if (_AttackerSquadType is Helicopter || _AttackerSquadType is Infantry || _AttackerSquadType is APC || _AttackerSquadType is AntiAir)
            prefab = GameManager._Instance._SparkVFXPrefab;
        else if (_AttackerSquadType is Tank || _AttackerSquadType is AntiTank)
            prefab = GameManager._Instance._ExplosionVFXPrefab;
        else
        {
            Debug.Log(_AttackerSquadType + " is using default prefab");
            prefab = GameManager._Instance._ExplosionVFXPrefab;
        }

        float newScale = Mathf.Log10(damage * 6.66f + 2f) * 2f / 5f;
        GameManager._Instance.SpawnVFX(prefab, damageTakeSquad._AttachedUnit.GetComponentInChildren<UnitModel>()._MidPoint.position, TerrainController._Instance.GetTerrainPointFromObject(damageTakeSquad._AttachedUnit.transform)._Normal, scale: newScale, isInfantry: damageTakeSquad._AttachedUnit._IsInfantry);
    }
    public int SquadToIndex(Squad squad)
    {
        switch (squad)
        {
            case Infantry t:
                return 0;
            case Truck t:
                return 1;
            case Train t:
                return 2;
            case Tank t:
                return 3;
            case APC t:
                return 4;
            case Artillery t:
                return 5;
            case RocketArtillery t:
                return 6;
            case AntiTank t:
                return 7;
            case AntiAir t:
                return 8;
            case Jet t:
                return 9;
            case Helicopter t:
                return 10;
            case CargoPlane t:
                return 11;
            case Cruiser t:
                return 12;
            case Destroyer t:
                return 13;
            case Corvette t:
                return 14;
            case TransportShip t:
                return 15;
            default:
                return 0;
        }
    }
}
