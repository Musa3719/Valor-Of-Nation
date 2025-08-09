using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Unit : MonoBehaviour
{
    public int _UnitIndex;
    public bool _IsEnemy;

    public Storage _Storage;
    public List<Squad> _Squads;

    public bool _IsCivilian => _Squads.All(s => s._IsCivilian);
    public bool _IsNaval => gameObject.CompareTag("NavalUnit");
    public bool _IsTrain => _Squads.All(s => s is ITrain);
    public bool _IsAir => _Squads.All(s => s is IAir);
    public float _Speed => GetLowestSquadSpeed();

    public Rigidbody _Rigidbody { get; private set; }
    public List<Vector3> _TargetPositions { get; private set; }
    public GameObject _AttachedToUnitObject { get; set; }

    public bool _IsAttacking { get; set; }// attack type, attack position or transform, other attacking orders and save

    private void Awake()
    {
        _UnitIndex = GameManager._Instance._LastCreatedUnitIndex++;

        _Rigidbody = GetComponent<Rigidbody>();
        _TargetPositions = new List<Vector3>();
        _Squads = new List<Squad>();
        _Storage = new Storage();

        //testing
        if (Time.realtimeSinceStartup < 1f)
        {
            if (_IsNaval)
            {
                _Squads.Add(new Cruiser(this));
                _Squads[0]._Amount = 1;
                _Squads.Add(new Submarine(this));
                _Squads[1]._Amount = 2;
            }
            else
            {
                _Squads.Add(new Infantry(this));
                _Squads[0]._Amount = 100;
                _Squads.Add(new Tank(this));
                _Squads[1]._Amount = 20;
                _Squads.Add(new Infantry(this));
                _Squads[2]._Amount = 250;
                _Squads.Add(new Truck(this));
                _Squads[3]._Amount = 50;
            }
        }
       

    }

    private void Update()
    {
        if (GameManager._Instance._IsGameStopped) return;

        if (_Squads.Count == 0)
        {
            if (GameInputController._Instance._SelectedUnits.Contains(gameObject))
                GameInputController._Instance.RemoveSelectedUnit(gameObject);
            Destroy(gameObject);
        }
        foreach (Squad squad in _Squads)
        {
            if (squad._Amount <= 0)
            {
                GameInputController._Instance.DeSelectSquad(squad);

                squad._AttachedUnit = null;
                _Squads.Remove(squad);
            }
        }

        if (_TargetPositions.Count > 0)
        {
            Vector3 targetPos = _TargetPositions[0];
            MoveToPosition(targetPos);

            if ((new Vector3(targetPos.x, 0f, targetPos.z) - new Vector3(transform.position.x, 0f, transform.position.z)).magnitude < 4f)
            {
                _TargetPositions.RemoveAt(0);
            }
        }
        else
        {
            MoveToPosition(transform.position);
        }
    }
    private float GetLowestSquadSpeed()
    {
        float lowest = float.MaxValue;
        foreach (var squad in _Squads)
        {
            if (squad._Speed < lowest)
                lowest = squad._Speed;
        }
        return lowest;
    }

    public int GetTotalInfantryAmount()
    {
        return GetTotalAmountCommon(true);
    }
    public int GetTotalNonInfantryAmount()
    {
        return GetTotalAmountCommon(false);
    }
    private int GetTotalAmountCommon(bool isInfantryAmount)
    {
        int sum = 0;
        foreach (var squad in _Squads)
        {
            bool checkingConditions = isInfantryAmount ? squad is Infantry : !(squad is Infantry);
            if (checkingConditions)
                sum += squad._Amount;
        }
        return sum;
    }

    public bool IsOnlyThisSquadType<T>() where T : ISquadInterfaces
    {
        foreach (var squad in _Squads)
        {
            if (squad is T)
                continue;
            else
                return false;
        }
        return true;
    }

    public List<Squad> GetAllSquadsCombined()
    {
        List<Squad> groupedSquads = new List<Squad>();

        foreach (var squad in _Squads)
        {
            bool merged = false;

            foreach (var groupedSquad in groupedSquads)
            {
                if (groupedSquad.GetType() == squad.GetType())
                {
                    groupedSquad._Amount += squad._Amount;
                    merged = true;
                    break;
                }
            }

            if (!merged)
            {
                Squad clone = (Squad)System.Activator.CreateInstance(squad.GetType());
                clone._Amount = squad._Amount;
                groupedSquads.Add(clone);
            }
        }

        return groupedSquads;
    }
    private int GetTotalAmountOfType<T>() where T : Squad
    {
        return _Squads
            .OfType<T>()
            .Sum(s => s._Amount);
    }

    public void ArrangeCurrentRouteGhost()
    {
        if (_TargetPositions.Count == 0)
        {
            transform.Find("CurrentRouteGhost").gameObject.SetActive(false);
            return;
        }

        transform.Find("CurrentRouteGhost").gameObject.SetActive(true);
        transform.Find("CurrentRouteGhost").GetComponent<LineRenderer>().positionCount = _TargetPositions.Count + 1;

        transform.Find("CurrentRouteGhost").GetComponent<LineRenderer>().SetPosition(0, transform.position + Vector3.up * 12f);
        for (int i = 0; i < _TargetPositions.Count; i++)
        {
            transform.Find("CurrentRouteGhost").GetComponent<LineRenderer>().SetPosition(i + 1, _TargetPositions[i] + Vector3.up * 12f);
        }
    }

    private void MoveToPosition(Vector3 pos)
    {
        Vector3 direction = (pos - transform.position).normalized;
        //direction.y = 0f;
        TerrainPoint point = TerrainController._Instance.GetTerrainPointFromObject(transform);
        Vector3 targetVel = Vector3.Lerp(_Rigidbody.linearVelocity, direction * GetUnitSpeed(point), Time.deltaTime * 15f);
        _Rigidbody.linearVelocity = new Vector3(targetVel.x, 0f, targetVel.z);
        float yPosition = point._BridgeHitPosition != Vector3.zero ? point._BridgeHitPosition.y + 10f : (point._RoadHitPosition != Vector3.zero ? point._RoadHitPosition.y + 3f : point._Position.y + 10f);
        _Rigidbody.position = new Vector3(_Rigidbody.position.x, yPosition, _Rigidbody.position.z);
        if (_Rigidbody.linearVelocity.sqrMagnitude > 0.001f)
        {
            Quaternion lookRot = Quaternion.LookRotation(_Rigidbody.linearVelocity.normalized, point._Normal);
            transform.rotation = Quaternion.Lerp(transform.rotation, lookRot, Time.deltaTime * 9f);
        }
        //transform.up = point._Normal;

    }
    private float GetUnitSpeed(TerrainPoint terrainPoint)
    {
        if (terrainPoint._TerrainUpperType == TerrainUpperType.DirtRoad) return IsOnlyThisSquadType<IWheel>() ? _Speed * 2f : _Speed * 1.75f;
        else if (terrainPoint._TerrainUpperType == TerrainUpperType.AsphaltRoad) return IsOnlyThisSquadType<IWheel>() ? _Speed * 3.25f : _Speed * 2.2f;
        else if (terrainPoint._TerrainUpperType == TerrainUpperType.Bridge) return _Speed * 1.25f;
        else if (terrainPoint._TerrainUpperType == TerrainUpperType.City) return _Speed * 0.6f;
        else if (terrainPoint._TerrainUpperType == TerrainUpperType.Forest) return IsOnlyThisSquadType<IWheel>() ? _Speed * 0.35f : _Speed * 0.5f;
        else return _Speed;
    }

    public void RemoveOrders()
    {
        _TargetPositions.Clear();
        //remove attack or any orders
    }


    public UnitData ToData()
    {
        return new UnitData
        {
            _StoredGoods = _Storage._StoredGoods,
            _IsEnemy = _IsEnemy,
            _Squads = _Squads,
            _IsNaval = _IsNaval,
            _Position = transform.localPosition,
            _UnitIndex = _UnitIndex,
            _AttachedToUnitObjectIndex = _AttachedToUnitObject == null ? -1 : _AttachedToUnitObject.GetComponent<Unit>()._UnitIndex
        };
    }
    public void LoadFromData(UnitData data)
    {
        _Storage._StoredGoods = data._StoredGoods;
        _Squads = data._Squads;
        _IsEnemy = data._IsEnemy;
        _UnitIndex = data._UnitIndex;
        transform.localPosition = data._Position;
    }
    public void LoadAttachedUnitsFromData(UnitData data)
    {
        if (data._AttachedToUnitObjectIndex != -1)
        {
            _AttachedToUnitObject = GetUnitObjectFromIndex(data._AttachedToUnitObjectIndex);
            GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
            transform.SetParent(_AttachedToUnitObject.transform.Find("CarryingUnits"));
            transform.position = _AttachedToUnitObject.transform.position;
            gameObject.SetActive(false);
        }
    }
    private GameObject GetUnitObjectFromIndex(int unitIndex)
    {
        foreach (var landUnit in GameObject.FindGameObjectsWithTag("LandUnit"))
        {
            if (landUnit.GetComponent<Unit>()._UnitIndex == unitIndex)
                return landUnit;
        }
        foreach (var navalUnit in GameObject.FindGameObjectsWithTag("NavalUnit"))
        {
            if (navalUnit.GetComponent<Unit>()._UnitIndex == unitIndex)
                return navalUnit;
        }
        return null;
    }
}
[System.Serializable]
public class UnitData
{
    public int _UnitIndex;

    [SerializeReference]
    public List<Squad> _Squads;
    public List<Goods> _StoredGoods;

    public bool _IsEnemy;

    //extras for data
    public int _AttachedToUnitObjectIndex;

    public bool _IsNaval;
    public Vector3 _Position;
    //move and attack orders//////////////////////////////////////////////////////////
}

public interface ISquadInterfaces
{

}
public interface ICarriableByTrain : ISquadInterfaces
{

}
public interface IAir : ISquadInterfaces
{

}
public interface INavy : ISquadInterfaces
{

}
public interface IBombFromAway : ISquadInterfaces
{

}
public interface IArmored : ISquadInterfaces
{

}
public interface IWheel : ISquadInterfaces
{

}
public interface IPalette : ISquadInterfaces
{

}
public interface ITrain : ISquadInterfaces
{

}


[System.Serializable]
public abstract class Squad
{
    public abstract string _DisplayName { get; }
    public abstract Sprite _Icon { get; }
    public float _Speed = 10f;
    public bool _IsCivilian;

    public int _Amount;
    public float _Morale;
    public bool _IsSquadSelected;
    public Unit _AttachedUnit;

    public Squad(Unit unit)
    {
        _AttachedUnit = unit;
    }

    public abstract Squad CreateNewSquadObject(Unit attachedUnit);
}
public class Infantry : Squad, ICarriableByTrain
{
    public override string _DisplayName => Localization._Instance._UI[24];
    public override Sprite _Icon => GameManager._Instance._UnitTypeIcons[0];

    public bool _IsUsingTrucks;

    public Infantry(Unit unit):base(unit)
    {
        
    }

    public override Squad CreateNewSquadObject(Unit attachedUnit)
    {
        return new Infantry(attachedUnit);
    }
}
public class Truck : Squad, IWheel, ICarriableByTrain
{
    public override string _DisplayName => Localization._Instance._UI[25];
    public override Sprite _Icon => GameManager._Instance._UnitTypeIcons[1];

    public float _CarryLimit = 10f;
    public float _CurrentCarry = 0f;

    public Truck(Unit unit) : base(unit)
    {

    }
    public override Squad CreateNewSquadObject(Unit attachedUnit)
    {
        return new Truck(attachedUnit);
    }
}
public class Train : Squad, ITrain
{
    public override string _DisplayName => Localization._Instance._UI[26];
    public override Sprite _Icon => GameManager._Instance._UnitTypeIcons[2];

    public Train(Unit unit) : base(unit)
    {

    }
    public override Squad CreateNewSquadObject(Unit attachedUnit)
    {
        return new Train(attachedUnit);
    }
}
public class Tank : Squad, IPalette, IArmored, ICarriableByTrain
{
    public override string _DisplayName => Localization._Instance._UI[27];
    public override Sprite _Icon => GameManager._Instance._UnitTypeIcons[3];

    public Tank(Unit unit) : base(unit)
    {

    }
    public override Squad CreateNewSquadObject(Unit attachedUnit)
    {
        return new Tank(attachedUnit);
    }
}
public class APC : Squad, IWheel, IArmored, ICarriableByTrain
{
    public override string _DisplayName => Localization._Instance._UI[28];
    public override Sprite _Icon => GameManager._Instance._UnitTypeIcons[4];

    public APC(Unit unit) : base(unit)
    {

    }
    public override Squad CreateNewSquadObject(Unit attachedUnit)
    {
        return new APC(attachedUnit);
    }
}
public class Artillery : Squad, IBombFromAway, ICarriableByTrain
{
    public override string _DisplayName => Localization._Instance._UI[29];
    public override Sprite _Icon => GameManager._Instance._UnitTypeIcons[5];

    public Artillery(Unit unit) : base(unit)
    {

    }
    public override Squad CreateNewSquadObject(Unit attachedUnit)
    {
        return new Artillery(attachedUnit);
    }
}
public class RocketArtillery : Squad, IBombFromAway, IWheel, ICarriableByTrain
{
    public override string _DisplayName => Localization._Instance._UI[30];
    public override Sprite _Icon => GameManager._Instance._UnitTypeIcons[6];

    public RocketArtillery(Unit unit) : base(unit)
    {

    }
    public override Squad CreateNewSquadObject(Unit attachedUnit)
    {
        return new RocketArtillery(attachedUnit);
    }
}
public class Mortar : Squad, IBombFromAway, ICarriableByTrain
{
    public override string _DisplayName => Localization._Instance._UI[31];
    public override Sprite _Icon => GameManager._Instance._UnitTypeIcons[7];

    public Mortar(Unit unit) : base(unit)
    {

    }
    public override Squad CreateNewSquadObject(Unit attachedUnit)
    {
        return new Mortar(attachedUnit);
    }
}
public class MachineGun : Squad, ICarriableByTrain
{
    public override string _DisplayName => Localization._Instance._UI[32];
    public override Sprite _Icon => GameManager._Instance._UnitTypeIcons[8];

    public MachineGun(Unit unit) : base(unit)
    {

    }
    public override Squad CreateNewSquadObject(Unit attachedUnit)
    {
        return new MachineGun(attachedUnit);
    }
}
public class AntiTank : Squad, ICarriableByTrain
{
    public override string _DisplayName => Localization._Instance._UI[33];
    public override Sprite _Icon => GameManager._Instance._UnitTypeIcons[9];

    public AntiTank(Unit unit) : base(unit)
    {

    }
    public override Squad CreateNewSquadObject(Unit attachedUnit)
    {
        return new AntiTank(attachedUnit);
    }
}
public class AntiAir : Squad, ICarriableByTrain
{
    public override string _DisplayName => Localization._Instance._UI[34];
    public override Sprite _Icon => GameManager._Instance._UnitTypeIcons[10];

    public AntiAir(Unit unit) : base(unit)
    {

    }
    public override Squad CreateNewSquadObject(Unit attachedUnit)
    {
        return new AntiAir(attachedUnit);
    }
}
public class PropellerPlane : Squad, IAir
{
    public override string _DisplayName => Localization._Instance._UI[35];
    public override Sprite _Icon => GameManager._Instance._UnitTypeIcons[11];

    public PropellerPlane(Unit unit) : base(unit)
    {

    }
    public override Squad CreateNewSquadObject(Unit attachedUnit)
    {
        return new PropellerPlane(attachedUnit);
    }
}
public class JetPlane : Squad, IAir
{
    public override string _DisplayName => Localization._Instance._UI[36];
    public override Sprite _Icon => GameManager._Instance._UnitTypeIcons[12];

    public JetPlane(Unit unit) : base(unit)
    {

    }
    public override Squad CreateNewSquadObject(Unit attachedUnit)
    {
        return new JetPlane(attachedUnit);
    }
}
public class Helicopter : Squad, IAir
{
    public override string _DisplayName => Localization._Instance._UI[37];
    public override Sprite _Icon => GameManager._Instance._UnitTypeIcons[13];

    public Helicopter(Unit unit) : base(unit)
    {

    }
    public override Squad CreateNewSquadObject(Unit attachedUnit)
    {
        return new Helicopter(attachedUnit);
    }
}
public class CargoPlane : Squad, IAir
{
    public override string _DisplayName => Localization._Instance._UI[38];
    public override Sprite _Icon => GameManager._Instance._UnitTypeIcons[14];

    public CargoPlane(Unit unit) : base(unit)
    {

    }
    public override Squad CreateNewSquadObject(Unit attachedUnit)
    {
        return new CargoPlane(attachedUnit);
    }
}
public class Cruiser : Squad, INavy
{
    public override string _DisplayName => Localization._Instance._UI[39];
    public override Sprite _Icon => GameManager._Instance._UnitTypeIcons[15];

    public Cruiser(Unit unit) : base(unit)
    {

    }
    public override Squad CreateNewSquadObject(Unit attachedUnit)
    {
        return new Cruiser(attachedUnit);
    }
}
public class Destroyer : Squad, INavy
{
    public override string _DisplayName => Localization._Instance._UI[40];
    public override Sprite _Icon => GameManager._Instance._UnitTypeIcons[16];

    public Destroyer(Unit unit) : base(unit)
    {

    }
    public override Squad CreateNewSquadObject(Unit attachedUnit)
    {
        return new Destroyer(attachedUnit);
    }
}
public class Corvette : Squad, INavy
{
    public override string _DisplayName => Localization._Instance._UI[41];
    public override Sprite _Icon => GameManager._Instance._UnitTypeIcons[17];

    public Corvette(Unit unit) : base(unit)
    {

    }
    public override Squad CreateNewSquadObject(Unit attachedUnit)
    {
        return new Corvette(attachedUnit);
    }
}
public class Submarine : Squad, INavy
{
    public override string _DisplayName => Localization._Instance._UI[42];
    public override Sprite _Icon => GameManager._Instance._UnitTypeIcons[18];

    public Submarine(Unit unit) : base(unit)
    {

    }
    public override Squad CreateNewSquadObject(Unit attachedUnit)
    {
        return new Submarine(attachedUnit);
    }
}
