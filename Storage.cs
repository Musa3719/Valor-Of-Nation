using System.Collections.Generic;
using UnityEngine;


public class Storage
{
    public List<Goods> _StoredGoods;

    public Storage()
    {
        _StoredGoods = new List<Goods>();
    }

    public void GainGoods(Goods goods)
    {
        Goods existing = GetGoodsTypeIfAnyExist(goods);
        if (existing == null)
            _StoredGoods.Add(goods);
        else
            existing._Amount += goods._Amount;
    }
    public void LoseGoods(Goods goods)
    {
        Goods existing = GetGoodsTypeIfAnyExist(goods);
        if (existing != null && goods._Amount <= existing._Amount)
        {
            existing._Amount -= goods._Amount;
            if (existing._Amount == 0)
                _StoredGoods.Remove(existing);
        }
    }

    public Goods GetGoodsTypeIfAnyExist(Goods goods)
    {
        foreach (var item in _StoredGoods)
        {
            if (item._Type == goods._Type)
                return item;
        }
        return null;
    }

}
[System.Serializable]
public class Goods
{
    public GoodsType _Type;
    public int _Amount;
}
[System.Serializable]
public enum GoodsType
{
    Coal,
    Oil,
    NaturalGas,
    Marble,
    Iron,
    Copper,
    Aluminum,
    Rubber,
    Gold,
    Silver,
    Jewelry,
    Uranium,
    Sulfur,
    Water,
    Timber,
    Food,
    Rations,
    Clothing,
    Camouflage,
    Coat,
    Ammunition,
    Explosives,
    OldRifle,
    AutoRifle,
    SniperRifle,
    Grenade,
    Mine,
    GasMask,
    MedKit,
    Tools,
    Steel,
    Asphalt,
    Circuit,
    MachineParts,
    Engine,
    Cannon,
    Glass,
    Wheel,
    Palette,
    Cable,
    Pipe,

}