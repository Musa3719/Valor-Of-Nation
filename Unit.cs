using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Unit : MonoBehaviour
{
    public string _Name;
    public int _UnitIndex;
    public bool _IsEnemy;

    public Storage _Storage;
    public List<Squad> _Squads;

    public ICarryUnit _CarryUnitSquad => _Squads.FirstOrDefault(s => s is ICarryUnit) as ICarryUnit;
    public bool _CanCarryAnotherUnit => _Squads.Any(s => s is ICarryUnit);
    public bool _CanGetOnAnotherUnit => _Squads.All(s => s is ICarriableUnit);
    public int _CarryWeight => _Squads.Sum(s => s._Weight);
    public bool _IsCivilian => _Squads.All(s => s._IsCivilian);
    public bool _IsNaval => gameObject.CompareTag("NavalUnit");
    public bool _IsTrain => _Squads.All(s => s is ITrain);
    public bool _IsAir => _Squads.All(s => s is IAir);
    public float _Speed => GetLowestSquadSpeed();

    public Rigidbody _Rigidbody { get; private set; }
    public List<Vector3> _TargetPositions { get; private set; }
    public GameObject _AttachedToUnitObject { get; set; }

    public bool _IsAttacking { get; set; }// attack type, attack position or transform, other attacking orders and save


    private List<Vector3> _currentRoutePoints;
    private float _updateModelsCounter;

    private void Awake()
    {
        _UnitIndex = GameManager._Instance._LastCreatedUnitIndex++;

        _Rigidbody = GetComponent<Rigidbody>();
        _TargetPositions = new List<Vector3>();
        _Squads = new List<Squad>();
        _Storage = new Storage();

        _currentRoutePoints = new List<Vector3>();

        //testing
        if (Time.frameCount == 0)
        {
            Squad newSquad;
            if (_IsNaval)
            {
                newSquad = new Cruiser(this);
                newSquad._Amount = 1;
                AddSquad(newSquad);
                newSquad = new Destroyer(this);
                newSquad._Amount = 3;
                AddSquad(newSquad);
                newSquad = new TransportShip(this);
                newSquad._Amount = 5;
                AddSquad(newSquad);
            }
            else
            {
                newSquad = new Infantry(this);
                newSquad._Amount = 100;
                AddSquad(newSquad);
                newSquad = new Tank(this);
                newSquad._Amount = 20;
                AddSquad(newSquad);
                newSquad = new Infantry(this);
                newSquad._Amount = 250;
                AddSquad(newSquad);
                newSquad = new Truck(this);
                newSquad._Amount = 50;
                AddSquad(newSquad);
            }
        }

    }
    private void Start()
    {
        int i = 0;
        _updateModelsCounter = 1;
        while (_updateModelsCounter > 0.4f && i < 3)
        {
            i++;
            ArrangeModels();
        }
    }

    private void Update()
    {
        if (GameManager._Instance._IsGameStopped) return;

        ArrangeCurrentRouteGhost();

        if (_Squads.Count == 0 || _CarryWeight == 0)
        {
            GameInputController._Instance.DeSelectUnit(gameObject);
            Destroy(gameObject);
            return;
        }
        foreach (Squad squad in _Squads)
        {
            if (squad._Amount <= 0)
            {
                GameInputController._Instance.DeSelectSquad(squad);

                squad._AttachedUnit = null;
                RemoveSquad(squad);
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

        int i = 0;
        _updateModelsCounter += Time.deltaTime;
        while (_updateModelsCounter > 0.4f && i < 3)
        {
            i++;
            ArrangeModels();
        }
    }
    private void ArrangeModels()
    {
        _updateModelsCounter = 0f;
        GameObject squadPrefab;
        bool isModelExist;
        Vector3[] points = GetPointsInCircle(_Squads.Count, 4f);

        List<Transform> existingModels = new List<Transform>();
        for (int t = 0; t < transform.Find("Models").childCount; t++)
        {
            existingModels.Add(transform.Find("Models").GetChild(t));
        }

        Physics.Raycast(transform.position + Vector3.up * 100f, -Vector3.up, out RaycastHit hit, 200f, GameManager._Instance._TerrainAndWaterLayers);

        for (int i = 0; i < _Squads.Count; i++)
        {
            Squad squad = _Squads[i];
            squadPrefab = GameManager._Instance.GetUnitModelPrefabFromSquad(squad);
            isModelExist = false;

            foreach (Transform model in existingModels)
            {
                if (model.name.StartsWith(squadPrefab.name))
                {
                    float yOffset = model.transform.position.y - model.GetComponent<UnitModel>()._BottomTransform.position.y;
                    float terrainHeight = hit.point.y;

                    if (model.position.y != terrainHeight + yOffset)
                        model.position = new Vector3(model.position.x, terrainHeight + yOffset, model.position.z);
                    isModelExist = true;
                }
            }

            if (!isModelExist)
            {
                int digitCount = 0;
                int tempForDigit = squad._Amount;
                while (tempForDigit > 0)
                {
                    tempForDigit /= 10;
                    digitCount++;
                }

                GameObject newObj = Instantiate(GameManager._Instance.GetUnitModelPrefabFromSquad(squad), transform.Find("Models"));
                newObj.GetComponent<UnitModel>()._SquadType = squad.GetType();
                float colorValue = 0.25f + digitCount * 0.15f;
                ArrangeModelMaterials(newObj, new Color(colorValue, colorValue, colorValue));

                float yOffset = newObj.transform.position.y - newObj.GetComponent<UnitModel>()._BottomTransform.position.y;
                float terrainHeight = hit.point.y;

                float scale = 2f - digitCount * 0.25f;
                scale /= 2f;
                newObj.transform.localScale = scale * Vector3.one;
                newObj.transform.position = new Vector3((transform.position + points[i]).x, terrainHeight + yOffset, (transform.position + points[i]).z);

                _updateModelsCounter = 1f;
            }

        }

        bool isDestroyed = false;
        for (int i = 0; i < existingModels.Count; i++)
        {
            bool hasType = false;
            foreach (Squad squad in _Squads)
            {
                if (existingModels[i].GetComponent<UnitModel>()._SquadType == squad.GetType())
                    hasType = true;
            }

            if (!hasType)
            {
                Destroy(existingModels[i].gameObject);
                isDestroyed = true;
            }
        }
        if (isDestroyed)
            SetModelPositions(points, hit.point.y);
    }
    private void SetModelPositions(Vector3[] points, float rayYPoint)
    {
        Debug.Log(points.Length);
        Debug.Log(transform.Find("Models").childCount);
        for (int i = 0; i < transform.Find("Models").childCount; i++)
        {
            Transform model = transform.Find("Models").GetChild(i);
            float yOffset = model.position.y - model.GetComponent<UnitModel>()._BottomTransform.position.y;
            model.position = new Vector3((transform.position + points[i]).x, rayYPoint + yOffset, (transform.position + points[i]).z);
        }
    }
    private void ArrangeModelMaterials(GameObject newObj, Color tint)
    {
        var renderers = newObj.GetComponentsInChildren<Renderer>();
        var block = new MaterialPropertyBlock();

        foreach (var rend in renderers)
        {
            rend.GetPropertyBlock(block);
            block.SetColor("_BaseColor", tint);
            rend.SetPropertyBlock(block);
        }
    }
    private Vector3[] GetPointsInCircle(int pointCount, float radius)
    {
        Vector3[] points = new Vector3[pointCount];

        if (pointCount <= 0)
            return points;

        if (pointCount == 1)
        {
            points[0] = Vector3.zero;
            return points;
        }

        // 2 ve üzeri -> eþit açý aralýklarýyla yerleþtirme
        float angleStep = 360f / pointCount;

        for (int i = 0; i < pointCount; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;

            points[i] = new Vector3(x, 0, z);
        }

        return points;
    }

    public void AddSquad(Squad squad)
    {
        foreach (var oldSquad in _Squads)
        {
            if (oldSquad.GetType() == squad.GetType())
            {
                oldSquad.AddValues(squad);
                return;
            }
        }
        _Squads.Add(squad);
    }
    public void RemoveSquad(Squad squad)
    {
        if (_Squads.Contains(squad))
            _Squads.Remove(squad);
    }
    public void MergeWithAnotherUnit(Unit anotherUnit)
    {
        if (_IsEnemy != anotherUnit._IsEnemy) return;

        foreach (Squad anotherSquad in anotherUnit._Squads)
        {
            AddSquad(anotherSquad);
        }
        AddStorage(anotherUnit._Storage);
        Destroy(anotherUnit.gameObject);
    }
    public Storage SplitStorage(float percent)
    {
        Storage newStorage = new Storage();
        foreach (var good in _Storage._StoredGoods)
        {
            Goods newGood = new Goods();
            newGood._Type = good._Type;
            int amount = (int)(good._Amount * percent);
            newGood._Amount = amount;
            good._Amount -= amount;
            if (amount > 0)
                newStorage.GainGoods(good);
        }
        return newStorage;
    }
    public void AddStorage(Storage newStorage)
    {
        foreach (var good in newStorage._StoredGoods)
        {
            _Storage.GainGoods(good);
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

    public int GetCarryingCapacity()
    {
        foreach (Squad squad in _Squads)
        {
            if (squad is ICarryUnit)
            {
                return (squad as ICarryUnit)._CarryLimit * squad._Amount;
            }
        }
        return 0;
    }
    public int GetCurrentCarry()
    {
        foreach (Squad squad in _Squads)
        {
            if (squad is ICarryUnit)
            {
                return (squad as ICarryUnit)._CurrentCarry;
            }
        }
        return 0;
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
        if (_IsEnemy) return;

        if (_TargetPositions.Count == 0)
        {
            transform.Find("CurrentRouteGhost").gameObject.SetActive(false);
            return;
        }

        if (!transform.Find("CurrentRouteGhost").gameObject.activeSelf)
            transform.Find("CurrentRouteGhost").gameObject.SetActive(true);

        _currentRoutePoints.Clear();
        _currentRoutePoints.Add(transform.position);
        for (int i = 0; i < _TargetPositions.Count; i++)
        {
            _currentRoutePoints.Add(_TargetPositions[i]);
        }
        TerrainController._Instance.ArrangeMergingLineRenderer(transform.Find("CurrentRouteGhost").GetComponent<LineRenderer>(), _currentRoutePoints);
    }

    private void MoveToPosition(Vector3 pos)
    {
        Vector3 direction = (pos - transform.position).normalized;
        //direction.y = 0f;
        TerrainPoint point = TerrainController._Instance.GetTerrainPointFromObject(transform);
        Vector3 targetVel = Vector3.Lerp(_Rigidbody.linearVelocity, direction * GetUnitSpeed(point), Time.deltaTime * 15f);
        _Rigidbody.linearVelocity = new Vector3(targetVel.x, 0f, targetVel.z);
        float yPosition = point._BridgeHitPosition != Vector3.zero ? point._BridgeHitPosition.y + 10f : (point._RoadHitPosition != Vector3.zero ? point._RoadHitPosition.y + 3f : point._Position.y + 12f);
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
            _Name = _Name,
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
        _Name = data._Name;
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
    public string _Name;
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
public interface ICarryUnit : ISquadInterfaces
{
    public int _CarryLimit { get; set; }
    public int _CurrentCarry { get; set; }
}
public interface ICarriableUnit : ISquadInterfaces
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

    public abstract int _Weight { get; }
    public int _Amount;
    public float _Morale;
    public bool _IsSquadSelected;
    public Unit _AttachedUnit;

    public Squad(Unit unit)
    {
        _AttachedUnit = unit;
    }

    public abstract Squad CreateNewSquadObject(Unit attachedUnit);
    public abstract void AddValues(Squad squadValuesToAdd);
}
public class Infantry : Squad, ICarriableUnit
{
    public override string _DisplayName => Localization._Instance._UI[24];
    public override Sprite _Icon => GameManager._Instance._UnitTypeIcons[0];
    public override int _Weight => _Amount;

    public Truck _AttachedTruck;
    public Infantry(Unit unit) : base(unit)
    {

    }

    public override Squad CreateNewSquadObject(Unit attachedUnit)
    {
        return new Infantry(attachedUnit);
    }

    public override void AddValues(Squad squadValuesToAdd)
    {
        Infantry inf = squadValuesToAdd as Infantry;
        _Amount += squadValuesToAdd._Amount;
    }
}
public class Truck : Squad, IWheel, ICarriableUnit
{
    public override string _DisplayName => Localization._Instance._UI[25];
    public override Sprite _Icon => GameManager._Instance._UnitTypeIcons[1];
    public override int _Weight => _Amount * 10;


    public int _CurrentTow;//limit is 1
    public int _CurrentManCarry;
    public int _ManCarryLimit = 10;
    public Truck(Unit unit) : base(unit)
    {

    }
    public override Squad CreateNewSquadObject(Unit attachedUnit)
    {
        return new Truck(attachedUnit);
    }
    public override void AddValues(Squad squadValuesToAdd)
    {
        Truck tru = squadValuesToAdd as Truck;
        _Amount += squadValuesToAdd._Amount;
    }
}
public class Train : Squad, ITrain, ICarryUnit
{
    public override string _DisplayName => Localization._Instance._UI[26];
    public override Sprite _Icon => GameManager._Instance._UnitTypeIcons[2];
    public override int _Weight => _Amount * 20;

    public int _CarryLimit { get => _carryLimit; set => _carryLimit = value; }
    public int _CurrentCarry { get => _currentCarry; set => _currentCarry = value; }

    private int _currentCarry;
    private int _carryLimit;

    public Train(Unit unit) : base(unit)
    {
        _CarryLimit = 20;
    }
    public override Squad CreateNewSquadObject(Unit attachedUnit)
    {
        return new Train(attachedUnit);
    }
    public override void AddValues(Squad squadValuesToAdd)
    {
        Train tra = squadValuesToAdd as Train;
        _Amount += squadValuesToAdd._Amount;
        _CurrentCarry += tra._CurrentCarry;
    }
}
public class Tank : Squad, IPalette, IArmored, ICarriableUnit
{
    public override string _DisplayName => Localization._Instance._UI[27];
    public override Sprite _Icon => GameManager._Instance._UnitTypeIcons[3];
    public override int _Weight => _Amount * 10;

    public Tank(Unit unit) : base(unit)
    {

    }
    public override Squad CreateNewSquadObject(Unit attachedUnit)
    {
        return new Tank(attachedUnit);
    }
    public override void AddValues(Squad squadValuesToAdd)
    {
        Tank tra = squadValuesToAdd as Tank;
        _Amount += squadValuesToAdd._Amount;
    }
}
public class APC : Squad, IWheel, IArmored, ICarriableUnit
{
    public override string _DisplayName => Localization._Instance._UI[28];
    public override Sprite _Icon => GameManager._Instance._UnitTypeIcons[4];
    public override int _Weight => _Amount * 10;

    public APC(Unit unit) : base(unit)
    {

    }
    public override Squad CreateNewSquadObject(Unit attachedUnit)
    {
        return new APC(attachedUnit);
    }
    public override void AddValues(Squad squadValuesToAdd)
    {
        APC apc = squadValuesToAdd as APC;
        _Amount += squadValuesToAdd._Amount;
    }
}
public class Artillery : Squad, IBombFromAway, ICarriableUnit
{
    public override string _DisplayName => Localization._Instance._UI[29];
    public override Sprite _Icon => GameManager._Instance._UnitTypeIcons[5];
    public override int _Weight => _Amount * 10;

    public Truck _TowedTo;
    public Artillery(Unit unit) : base(unit)
    {

    }
    public override Squad CreateNewSquadObject(Unit attachedUnit)
    {
        return new Artillery(attachedUnit);
    }
    public override void AddValues(Squad squadValuesToAdd)
    {
        Artillery art = squadValuesToAdd as Artillery;
        _Amount += squadValuesToAdd._Amount;
    }
}
public class RocketArtillery : Squad, IBombFromAway, IWheel, ICarriableUnit
{
    public override string _DisplayName => Localization._Instance._UI[30];
    public override Sprite _Icon => GameManager._Instance._UnitTypeIcons[6];
    public override int _Weight => _Amount * 10;

    public RocketArtillery(Unit unit) : base(unit)
    {

    }
    public override Squad CreateNewSquadObject(Unit attachedUnit)
    {
        return new RocketArtillery(attachedUnit);
    }
    public override void AddValues(Squad squadValuesToAdd)
    {
        RocketArtillery roc = squadValuesToAdd as RocketArtillery;
        _Amount += squadValuesToAdd._Amount;
    }
}

public class AntiTank : Squad, ICarriableUnit
{
    public override string _DisplayName => Localization._Instance._UI[33];
    public override Sprite _Icon => GameManager._Instance._UnitTypeIcons[7];
    public override int _Weight => _Amount * 10;

    public Truck _TowedTo;
    public AntiTank(Unit unit) : base(unit)
    {

    }
    public override Squad CreateNewSquadObject(Unit attachedUnit)
    {
        return new AntiTank(attachedUnit);
    }
    public override void AddValues(Squad squadValuesToAdd)
    {
        AntiTank at = squadValuesToAdd as AntiTank;
        _Amount += squadValuesToAdd._Amount;
    }
}
public class AntiAir : Squad, ICarriableUnit
{
    public override string _DisplayName => Localization._Instance._UI[34];
    public override Sprite _Icon => GameManager._Instance._UnitTypeIcons[8];
    public override int _Weight => _Amount * 10;

    public Truck _TowedTo;
    public AntiAir(Unit unit) : base(unit)
    {

    }
    public override Squad CreateNewSquadObject(Unit attachedUnit)
    {
        return new AntiAir(attachedUnit);
    }
    public override void AddValues(Squad squadValuesToAdd)
    {
        AntiAir aa = squadValuesToAdd as AntiAir;
        _Amount += squadValuesToAdd._Amount;
    }
}
public class Jet : Squad, IAir
{
    public override string _DisplayName => Localization._Instance._UI[36];
    public override Sprite _Icon => GameManager._Instance._UnitTypeIcons[9];
    public override int _Weight => _Amount * 20;

    public Jet(Unit unit) : base(unit)
    {

    }
    public override Squad CreateNewSquadObject(Unit attachedUnit)
    {
        return new Jet(attachedUnit);
    }
    public override void AddValues(Squad squadValuesToAdd)
    {
        Jet jet = squadValuesToAdd as Jet;
        _Amount += squadValuesToAdd._Amount;
    }
}
public class Helicopter : Squad, IAir
{
    public override string _DisplayName => Localization._Instance._UI[37];
    public override Sprite _Icon => GameManager._Instance._UnitTypeIcons[10];
    public override int _Weight => _Amount * 20;

    public Helicopter(Unit unit) : base(unit)
    {

    }
    public override Squad CreateNewSquadObject(Unit attachedUnit)
    {
        return new Helicopter(attachedUnit);
    }
    public override void AddValues(Squad squadValuesToAdd)
    {
        Helicopter hel = squadValuesToAdd as Helicopter;
        _Amount += squadValuesToAdd._Amount;
    }
}
public class CargoPlane : Squad, IAir, ICarryUnit
{
    public override string _DisplayName => Localization._Instance._UI[38];
    public override Sprite _Icon => GameManager._Instance._UnitTypeIcons[11];
    public override int _Weight => _Amount * 50;

    public int _CarryLimit { get => _carryLimit; set => _carryLimit = value; }
    public int _CurrentCarry { get => _currentCarry; set => _currentCarry = value; }

    private int _currentCarry;
    private int _carryLimit;

    public CargoPlane(Unit unit) : base(unit)
    {
        _CarryLimit = 50;
    }
    public override Squad CreateNewSquadObject(Unit attachedUnit)
    {
        return new CargoPlane(attachedUnit);
    }
    public override void AddValues(Squad squadValuesToAdd)
    {
        CargoPlane carg = squadValuesToAdd as CargoPlane;
        _Amount += squadValuesToAdd._Amount;
        _CurrentCarry += carg._CurrentCarry;
    }
}
public class Cruiser : Squad, INavy
{
    public override string _DisplayName => Localization._Instance._UI[39];
    public override Sprite _Icon => GameManager._Instance._UnitTypeIcons[12];
    public override int _Weight => _Amount * 100;

    public Cruiser(Unit unit) : base(unit)
    {

    }
    public override Squad CreateNewSquadObject(Unit attachedUnit)
    {
        return new Cruiser(attachedUnit);
    }
    public override void AddValues(Squad squadValuesToAdd)
    {
        Cruiser cru = squadValuesToAdd as Cruiser;
        _Amount += squadValuesToAdd._Amount;
    }
}
public class Destroyer : Squad, INavy
{
    public override string _DisplayName => Localization._Instance._UI[40];
    public override Sprite _Icon => GameManager._Instance._UnitTypeIcons[13];
    public override int _Weight => _Amount * 50;

    public Destroyer(Unit unit) : base(unit)
    {

    }
    public override Squad CreateNewSquadObject(Unit attachedUnit)
    {
        return new Destroyer(attachedUnit);
    }
    public override void AddValues(Squad squadValuesToAdd)
    {
        Destroyer des = squadValuesToAdd as Destroyer;
        _Amount += squadValuesToAdd._Amount;
    }
}
public class Corvette : Squad, INavy
{
    public override string _DisplayName => Localization._Instance._UI[41];
    public override Sprite _Icon => GameManager._Instance._UnitTypeIcons[14];
    public override int _Weight => _Amount * 20;

    public Corvette(Unit unit) : base(unit)
    {

    }
    public override Squad CreateNewSquadObject(Unit attachedUnit)
    {
        return new Corvette(attachedUnit);
    }
    public override void AddValues(Squad squadValuesToAdd)
    {
        Corvette cor = squadValuesToAdd as Corvette;
        _Amount += squadValuesToAdd._Amount;
    }
}

public class TransportShip : Squad, INavy, ICarryUnit
{
    public override string _DisplayName => Localization._Instance._UI[31];
    public override Sprite _Icon => GameManager._Instance._UnitTypeIcons[15];
    public override int _Weight => _Amount * 100;

    public int _CarryLimit { get => _carryLimit; set => _carryLimit = value; }
    public int _CurrentCarry { get => _currentCarry; set => _currentCarry = value; }

    private int _currentCarry;
    private int _carryLimit;

    public TransportShip(Unit unit) : base(unit)
    {
        _CarryLimit = 100;
    }
    public override Squad CreateNewSquadObject(Unit attachedUnit)
    {
        return new TransportShip(attachedUnit);
    }
    public override void AddValues(Squad squadValuesToAdd)
    {
        TransportShip tra = squadValuesToAdd as TransportShip;
        _Amount += squadValuesToAdd._Amount;
        _CurrentCarry += tra._CurrentCarry;
    }
}
