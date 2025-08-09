using System.Collections.Generic;
using UnityEngine;

public class City : MonoBehaviour
{
    public string _Name;
    public int _Population;
    public int _InfrastructureLevel;
    public float _DamageLevel;

    public bool _IsEnemy;

    public Storage _Storage;
    public List<Building> _ConstructedBuildings;
    //stored vehicles
    //garrison

    private void Awake()
    {
        _ConstructedBuildings = new List<Building>();
        _Storage = new Storage();
    }

    public void EmigrateToAnotherCity(City anotherCity, int emigrateAmount)
    {
        if (emigrateAmount <= _Population)
        {
            _Population -= emigrateAmount;
            anotherCity._Population += emigrateAmount;
        }
    }
   

    public CityData ToData()
    {
        return new CityData
        {
            _Name = _Name,
            _Population = _Population,
            _InfrastructureLevel = _InfrastructureLevel,
            _DamageLevel = _DamageLevel,
            _IsEnemy = _IsEnemy,
            _ConstructedBuildings = _ConstructedBuildings,
            _StoredGoods = _Storage._StoredGoods
        };
    }
    public void LoadFromData(CityData data)
    {
        _Name = data._Name;
        _Population = data._Population;
        _InfrastructureLevel = data._InfrastructureLevel;
        _DamageLevel = data._DamageLevel;
        _IsEnemy = data._IsEnemy;
        _ConstructedBuildings = data._ConstructedBuildings;
        _Storage._StoredGoods = data._StoredGoods;
    }
}

[System.Serializable]
public class Building
{
    public string _Name;
}


[System.Serializable]
public class CityData
{
    public string _Name;
    public int _Population;
    public int _InfrastructureLevel;
    public float _DamageLevel;

    public bool _IsEnemy;

    public List<Building> _ConstructedBuildings;
    public List<Goods> _StoredGoods;
}