using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;

public class Unit : MonoBehaviour
{
    public string _Name;
    public int _UnitIndex;
    public bool _IsEnemy;

    public Storage _Storage;
    public Squad _Squad;

    public bool _IsCarryingBigUnit => (_Squad is ICarryBigUnit) && (_Squad as ICarryBigUnit)._CurrentCarry != 0;
    public bool _IsCarryingWithTruck => (_Squad is Truck) && ((_Squad as Truck)._CurrentManCarry != 0 || (_Squad as Truck)._CurrentTow != 0);
    public bool _CanCarryAnotherBigUnit => _Squad is ICarryBigUnit;
    public bool _CanGetOnAnotherUnit => _Squad is ICarriableUnit;
    public int _CarryWeight => _Squad._Weight;
    public bool _IsCivilian => _Squad._IsCivilian;
    public bool _IsNaval => gameObject.CompareTag("NavalUnit");
    public bool _IsTrain => _Squad is Train;
    public bool _IsInfantry => _Squad is Infantry;
    public bool _IsAir => _Squad is IAir;
    public float _Speed => _Squad._Speed;

    public Rigidbody _Rigidbody { get; private set; }
    public Animator _Animator { get; set; }
    public List<Vector3> _TargetPositions { get; private set; }
    public GameObject _AttachedToUnitObject { get; set; }
    public bool _WasInNestedTruck { get; set; }

    public bool _IsDead { get; private set; }
    public List<Transform> _AttackModels { get; set; }
    public Transform _AttackTarget { get; set; }// attack type, other attacking orders and save

    public List<Vector3> _PotentialRoutePoints { get; private set; }
    private List<Vector3> _currentRoutePoints;

    private Coroutine _rocketCoroutine;

    private List<Vector3> _rocketsStartPositions;
    private List<Quaternion> _rocketsStartRotations;
    private List<Transform> _rockets;
    private Transform _rocketParent;

    private void Awake()
    {
        if (!_IsEnemy)
            GameManager._Instance._FriendlyUnitColliders.Add(transform.Find("RayCollider").GetComponent<Collider>());

        _Rigidbody = GetComponent<Rigidbody>();
        _AttackModels = new List<Transform>();
        _TargetPositions = new List<Vector3>();
        _Storage = new Storage();

        _PotentialRoutePoints = new List<Vector3>();
        _currentRoutePoints = new List<Vector3>();

        if (Time.frameCount == 0)
            CreateSquadForTesting();
    }
    private void CreateSquadForTesting()
    {
        Squad newSquad;
        if (_IsNaval)
        {
            int random = Random.Range(0, 3);
            if (random == 0)
            {
                newSquad = new Cruiser(this, 1);
                _Squad = newSquad;
            }
            else if (random == 1)
            {

                newSquad = new Corvette(this, 3);
                _Squad = newSquad;
            }
            else if (random == 2)
            {
                newSquad = new TransportShip(this, 5);
                _Squad = newSquad;
            }
        }
        else
        {
            int random = Random.Range(0, 2);
            if (random == 0)
            {
                newSquad = new Truck(this, 50);
                _Squad = newSquad;
            }
            else if (random == 1)
            {
                newSquad = new Infantry(this, 100);
                _Squad = newSquad;
            }
            else if (random == 2)
            {
                newSquad = new Train(this, 20);
                _Squad = newSquad;
            }
        }
    }
    private void LateUpdate()
    {
        ArrangeGunRotation();
    }
    private void Update()
    {
        if (!GameManager._Instance._IsGameStopped && _IsDead)
            _Rigidbody.linearVelocity = Vector3.Lerp(_Rigidbody.linearVelocity, Vector3.zero, Time.deltaTime * 0.8f);

        if (GameManager._Instance._IsGameStopped || _IsDead) return;

        if (Input.GetKeyDown(KeyCode.F))
            Fire();
        if (Input.GetKeyDown(KeyCode.X))
        {
            Damage damage = new Damage();
            damage._AttackerSquadReconEfficiency = 1f;
            damage._AttackerSquadType = _Squad;
            damage._AttackerSquadAmount = 1000;
            TakeHit(damage);
            //Die();
            return;
        }

        ArrangeCurrentRouteGhost();

        if (transform.Find("Model").GetComponentInChildren<UnitModel>()._MovementVFX != null)
        {
            if (_Rigidbody.linearVelocity.magnitude > 2f)
                transform.Find("Model").GetComponentInChildren<UnitModel>()._MovementVFX.SetBool("IsSpawning", true);
            else
                transform.Find("Model").GetComponentInChildren<UnitModel>()._MovementVFX.SetBool("IsSpawning", false);
        }


        _Animator.SetBool("IsMoving", _Rigidbody.linearVelocity.magnitude > 0.25f);
        float speed = (_IsAir && _Rigidbody.linearVelocity.magnitude <= 1f) ? (1f / 10f) : (_Rigidbody.linearVelocity.magnitude / 10f);
        _Animator.SetFloat("Speed", speed);

        if (_Squad._Amount <= 0)
        {
            Die();
            return;
        }

        if (_TargetPositions.Count > 0)
        {
            Vector3 targetPos = _TargetPositions[0];
            MoveToPosition(targetPos);

            if ((new Vector3(targetPos.x, 0f, targetPos.z) - new Vector3(transform.position.x, 0f, transform.position.z)).magnitude < 1f)
            {
                _TargetPositions.RemoveAt(0);
            }
        }
        else
        {
            MoveToPosition(transform.position);
        }

        UpdateWorldUI();
    }
    private void ArrangeGunRotation()
    {
        if (_AttackModels.Count == 0) return;

        if (_AttackTarget == null)
        {
            foreach (var model in _AttackModels)
            {
                model.transform.localEulerAngles = Vector3.zero;
            }
        }
        else
        {
            foreach (var model in _AttackModels)
            {
                model.transform.LookAt(_AttackTarget);
                model.transform.localEulerAngles = new Vector3(0f, model.transform.localEulerAngles.y, 0f);
            }
        }
    }
    public void UpdateWorldUI()
    {
        if (_IsEnemy)
        {
            if (transform.Find("Model").Find("UnitUI").Find("NameAmountText").GetComponent<TextMeshProUGUI>().color != GameManager._Instance._EnemyColor)
                transform.Find("Model").Find("UnitUI").Find("NameAmountText").GetComponent<TextMeshProUGUI>().color = GameManager._Instance._EnemyColor;
            if (transform.Find("Model").Find("UnitUI").Find("NameAmountText").GetComponent<TextMeshProUGUI>().text != _Name + "\nx" + _Squad._Amount.ToString())
                transform.Find("Model").Find("UnitUI").Find("NameAmountText").GetComponent<TextMeshProUGUI>().text = _Name + "\nx" + _Squad._Amount.ToString();
        }
        else
        {
            if (transform.Find("Model").Find("UnitUI").Find("NameAmountText").GetComponent<TextMeshProUGUI>().color != GameManager._Instance._FriendlyColor)
                transform.Find("Model").Find("UnitUI").Find("NameAmountText").GetComponent<TextMeshProUGUI>().color = GameManager._Instance._FriendlyColor;

            if (transform.Find("Model").Find("UnitUI").Find("NameAmountText").GetComponent<TextMeshProUGUI>().text.StartsWith("x"))
            {
                if (transform.Find("Model").Find("UnitUI").Find("NameAmountText").GetComponent<TextMeshProUGUI>().text != "x" + _Squad._Amount.ToString())
                    transform.Find("Model").Find("UnitUI").Find("NameAmountText").GetComponent<TextMeshProUGUI>().text = "x" + _Squad._Amount.ToString();
            }
            else
            {
                if (transform.Find("Model").Find("UnitUI").Find("NameAmountText").GetComponent<TextMeshProUGUI>().text != _Name + "\nx" + _Squad._Amount.ToString())
                    transform.Find("Model").Find("UnitUI").Find("NameAmountText").GetComponent<TextMeshProUGUI>().text = _Name + "\nx" + _Squad._Amount.ToString();
            }
        }
        

    }
    public void Fire()
    {
        if (_IsDead) return;

        _AttackTarget = GameObject.Find("Boxx").transform;///////////

        //combat logic

        if ((_Squad is RocketArtillery) || (_Squad is Jet))
            ManageRocketModels(_AttackTarget);
        _Animator.SetTrigger("Fire");
        //vfx and sfx
    }
    private void ManageRocketModels(Transform target)
    {
        if (_rocketsStartPositions == null)
        {
            _rockets = new List<Transform>();
            if (_Squad is RocketArtillery)
            {
                for (int i = 0; i < transform.Find("Model").Find("RocketArtyModel(Clone)").Find("Katyusha").Find("Rockets").childCount; i++)
                {
                    _rockets.Add(transform.Find("Model").Find("RocketArtyModel(Clone)").Find("Katyusha").Find("Rockets").GetChild(i));
                }
                _rocketParent = transform.Find("Model").Find("RocketArtyModel(Clone)").Find("Katyusha").Find("Rockets");
            }
            else if (_Squad is Jet)
            {
                for (int i = 0; i < transform.Find("Model").Find("JetModel(Clone)").Find("Jet").Find("Accesories").childCount; i++)
                {
                    _rockets.Add(transform.Find("Model").Find("JetModel(Clone)").Find("Jet").Find("Accesories").GetChild(i));
                }
                _rocketParent = transform.Find("Model").Find("JetModel(Clone)").Find("Jet").Find("Accesories");
            }

            _rocketsStartPositions = new List<Vector3>();
            _rocketsStartRotations = new List<Quaternion>();
            foreach (Transform rocket in _rockets)
            {
                _rocketsStartPositions.Add(rocket.localPosition);
                _rocketsStartRotations.Add(rocket.localRotation);
            }
        }

        GameManager._Instance.CoroutineCall(ref _rocketCoroutine, RocketCoroutine(target), GameManager._Instance);
    }
    private int ActiveRocketCount()
    {
        if (_rockets == null) return 0;
        int sum = 0;
        foreach (Transform rocket in _rockets)
        {
            if (rocket.gameObject.activeSelf)
                sum++;
        }
        return sum;
    }
    public IEnumerator RocketCoroutine(Transform target)
    {
        _Animator.enabled = false;

        for (int i = 0; i < _rockets.Count; i++)
        {
            _rockets[i].gameObject.SetActive(true);

            if (_rockets[i].parent == null)
            {
                _rockets[i].SetParent(_rocketParent);
                _rockets[i].localPosition = _rocketsStartPositions[i];
                _rockets[i].localRotation = _rocketsStartRotations[i];
            }
            _rockets[i].SetParent(null);

            if (_rockets[i].GetComponent<Rigidbody>() == null)
                _rockets[i].gameObject.AddComponent<Rigidbody>();
        }

        float speed = 60f;
        float timer = 0f;
        while (timer < 4f && ActiveRocketCount() > 0)
        {
            foreach (Transform rocket in _rockets)
            {
                if (rocket.gameObject.activeSelf && (target.position - rocket.position).magnitude < 1f)
                {
                    rocket.gameObject.SetActive(false);
                }

                if (rocket.gameObject.activeSelf)
                {
                    if (timer < 0.04f)
                        rocket.GetComponent<Rigidbody>().linearVelocity = rocket.forward.normalized * speed;
                    else
                        rocket.GetComponent<Rigidbody>().linearVelocity = Vector3.Lerp(rocket.GetComponent<Rigidbody>().linearVelocity, (target.position - rocket.position).normalized * speed, Time.deltaTime * GameManager._Instance._GameSpeed * 12f);
                    rocket.forward = rocket.GetComponent<Rigidbody>().linearVelocity.normalized;
                }
            }
            timer += Time.deltaTime * GameManager._Instance._GameSpeed;
            yield return null;
        }

        for (int i = 0; i < _rockets.Count; i++)
        {
            _rockets[i].gameObject.SetActive(true);
            _rockets[i].SetParent(_rocketParent);
            if (_rockets[i].gameObject.GetComponent<Rigidbody>() != null)
                Destroy(_rockets[i].gameObject.GetComponent<Rigidbody>());
            _rockets[i].localPosition = _rocketsStartPositions[i];
            _rockets[i].localRotation = _rocketsStartRotations[i];
        }

        _Animator.enabled = true;
    }

    public void TakeHit(Damage damage)
    {
        damage.TakeDamage(_Squad);

        if (_Squad._Amount <= 0)
        {
            Die();
            return;
        }
        UpdateWorldUI();
    }
    public void Die()
    {
        if (_IsDead) return;
        _Squad._Amount = 0;

        if (_IsCarryingBigUnit || _IsCarryingWithTruck)
            ArrangeCarryingsWhenDestroyed();

        _IsDead = true;
        _TargetPositions.Clear();
        RemoveRockets();
        _AttackTarget = null;
        transform.Find("CurrentRouteGhost").gameObject.SetActive(false);
        GameInputController._Instance.DeSelectUnit(gameObject);
        GameManager._Instance._FriendlyUnitColliders.Remove(transform.Find("RayCollider").GetComponent<Collider>());

        if (_IsAir)
        {
            TriggerAirUnitDeathFall();
            GameManager._Instance.SpawnVFX(GameManager._Instance._ExplosionVFXPrefab, transform.Find("Model").GetComponentInChildren<UnitModel>().transform.position, TerrainController._Instance.GetTerrainPointFromObject(transform)._Normal, scale: 4.5f);
        }
        else
        {
            _Animator.SetTrigger("Die");
            if (!_IsInfantry)
                GameManager._Instance.SpawnVFX(GameManager._Instance._GroundExplosionVFXPrefab, transform.Find("Model").GetComponentInChildren<UnitModel>().transform.position, TerrainController._Instance.GetTerrainPointFromObject(transform)._Normal, scale: 4.5f);
            else
                _Animator.applyRootMotion = true;
        }


        DropStorage();
        UpdateWorldUI();
        Destroy(gameObject, 20f);
    }
    private void TriggerAirUnitDeathFall()
    {
        _Rigidbody.useGravity = true;
        _Rigidbody.freezeRotation = false;
        _Rigidbody.angularVelocity = new Vector3(Random.Range(-4.5f, 4.5f), Random.Range(-8f, 8f), Random.Range(-2f, 2f));
        MeshCollider col = _Animator.GetComponentInChildren<MeshRenderer>().gameObject.AddComponent<MeshCollider>();
        col.convex = true;
        gameObject.AddComponent<AirUnitColliderAfterDeath>()._Animator = _Animator;
    }

    private void ArrangeCarryingsWhenDestroyed()
    {
        foreach (Transform carrying in transform.Find("CarryingUnits"))
        {
            carrying.GetComponent<Unit>().DropStorage();
        }
    }
    public void CreateModel(int squadAmount, Squad squad)
    {
        GameObject newModel = Instantiate(GameManager._Instance.GetUnitModelPrefabFromSquad(squad), transform.Find("Model"));
        _Animator = newModel.GetComponentInChildren<Animator>();
        UpdateModel(squadAmount);
    }
    public void UpdateModel(int squadAmount)
    {
        float scale = Mathf.Log10(squadAmount * 6.66f + 2f) * 2f / 6f;
        transform.Find("Model").GetComponentInChildren<UnitModel>().transform.localScale = scale * Vector3.one;
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

    public bool AddSquad(Squad squad)
    {
        if (_Squad == null)
        {
            _Squad = squad;
            return true;
        }

        if (_Squad.GetType() == squad.GetType())
        {
            _Squad.AddValues(squad);
            return true;
        }
        Debug.Log("Squad is not same type!");
        return false;
    }
    public void RemoveUnit()
    {
        GameManager._Instance._FriendlyUnitColliders.Remove(transform.Find("RayCollider").GetComponent<Collider>());
        GameInputController._Instance.DeSelectUnit(gameObject);
        RemoveRockets();
        Destroy(gameObject);
    }
    private void RemoveRockets()
    {
        if (_rockets == null) return;
        foreach (Transform rocket in _rockets)
        {
            Destroy(rocket.gameObject);
        }
    }

    public void DropStorage()
    {
        Vector3 pos = transform.position;
        pos.y = TerrainController._Instance.GetTerrainHeightAtPosition(transform.position, 500f);
        GameObject newStorageHolder = Instantiate(GameManager._Instance._StorageHolderPrefab, pos, Quaternion.identity);
        newStorageHolder.transform.up = transform.up;
        newStorageHolder.GetComponent<StorageHolder>()._Storage = new Storage();
        newStorageHolder.GetComponent<StorageHolder>()._Storage.AddStorage(_Storage);
    }

    public bool IsSelected()
    {
        return transform.Find("Model").Find("UnitUI").Find("SelectedUI").GetComponent<Image>().color == GameInputController._Instance._SelectedUnitColor;
    }
    public int GetCarryingCapacity()
    {
        if (_Squad is ICarryBigUnit)
        {
            return (_Squad as ICarryBigUnit)._CarryLimit * _Squad._Amount;
        }
        return 0;
    }
    public int GetCurrentCarry()
    {
        if (_Squad is ICarryBigUnit)
        {
            return (_Squad as ICarryBigUnit)._CurrentCarry;
        }
        return 0;
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
        TerrainController._Instance.ArrangeMergingLineRenderer(transform.Find("CurrentRouteGhost").GetComponent<LineRenderer>(), _currentRoutePoints, -transform.up.normalized);
    }

    private void MoveToPosition(Vector3 pos)
    {
        TerrainPoint point = TerrainController._Instance.GetTerrainPointFromObject(transform);
        ArrangeRotation(point._Normal);

        if (_AttackModels.Count == 0 && _AttackTarget != null)
        {
            _Rigidbody.linearVelocity = Vector3.zero;
            return;
        }

        Vector3 direction = (pos - transform.position).normalized;
        //direction.y = 0f;
        float lerpSpeed = 10f;

        Vector3 targetVel = Vector3.Lerp(_Rigidbody.linearVelocity, direction * GetUnitSpeed(point), lerpSpeed * Time.deltaTime);
        _Rigidbody.linearVelocity = new Vector3(targetVel.x, 0f, targetVel.z);

        float yPosition = (point._BridgeHitPosition != Vector3.zero) ? point._BridgeHitPosition.y + 7f : ((point._RoadHitPosition != Vector3.zero) ? point._RoadHitPosition.y + 3.2f : point._Position.y + 10f);
        if (_IsNaval)
            yPosition = TerrainController._Instance.GetSeaLevel() - 3.5f;
        else if (_IsAir)
            yPosition = TerrainController._Instance.GetSeaLevelOrTerrainHeight(transform.position) + 10f;

        _Rigidbody.position = new Vector3(_Rigidbody.position.x, yPosition, _Rigidbody.position.z);
    }
    private void ArrangeRotation(Vector3 normal)
    {
        Vector3 forward = _Rigidbody.linearVelocity.magnitude > 0.25f ? _Rigidbody.linearVelocity.normalized : transform.Find("Model").forward.normalized;
        if (_AttackModels.Count == 0 && _AttackTarget != null)
        {
            Vector3 dir = (_AttackTarget.transform.position - transform.position).normalized;
            dir.y = 0f;
            forward = dir;
        }
        Vector3 up = _IsNaval ? Vector3.up : normal.normalized;
        Vector3 right = Vector3.Cross(up, forward).normalized;
        forward = Vector3.Cross(right, up).normalized;
        Quaternion lookRot = Quaternion.LookRotation(forward, up);
        transform.Find("Model").rotation = Quaternion.Lerp(transform.Find("Model").rotation, lookRot, Time.deltaTime * 6f);
    }
    private bool IsOnWater()
    {
        Ray ray = new Ray(transform.position + Vector3.up * 2000f, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 3000f, GameManager._Instance._TerrainAndWaterLayers) && hit.collider != null && hit.collider.gameObject.layer == LayerMask.NameToLayer("Water")) return true;
        return false;
    }
    private bool IsOnCity()
    {
        Ray ray = new Ray(transform.position + Vector3.up * 2000f, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 3000f, LayerMask.GetMask("UpperTerrain")) && hit.collider != null && hit.collider.gameObject.CompareTag("City")) return true;
        return false;
    }

    private float GetUnitSpeed(TerrainPoint terrainPoint)
    {
        if (_IsAir || _IsNaval) return _Speed;
        else if (terrainPoint._TerrainUpperType == TerrainUpperType.DirtRoad) return (_Squad is IWheel) ? _Speed * 2f : _Speed * 1.5f;
        else if (terrainPoint._TerrainUpperType == TerrainUpperType.AsphaltRoad) return (_Squad is IWheel) ? _Speed * 3f : _Speed * 1.75f;
        else if (terrainPoint._TerrainUpperType == TerrainUpperType.Bridge) return _Speed * 0.8f;
        else if (terrainPoint._TerrainUpperType == TerrainUpperType.City) return _Speed * 0.8f;
        else if (terrainPoint._TerrainUpperType == TerrainUpperType.Forest) return (_Squad is IWheel) ? _Speed * 0.35f : _Speed * 0.5f;
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
            _WasInNestedTruck = _WasInNestedTruck,
            _TargetPositions = _TargetPositions.Copy(),
            _Velocity = _Rigidbody.linearVelocity,
            _StoredGoods = _Storage._StoredGoods.Copy(),
            _IsEnemy = _IsEnemy,
            _Squad = _Squad,
            _IsNaval = _IsNaval,
            _Position = transform.localPosition,
            _Name = _Name,
            _UnitIndex = _UnitIndex,
            _AttachedToUnitObjectIndex = _AttachedToUnitObject == null ? -1 : _AttachedToUnitObject.GetComponent<Unit>()._UnitIndex
        };
    }
    public void LoadFromData(UnitData data)
    {
        _WasInNestedTruck = data._WasInNestedTruck;
        _Rigidbody.linearVelocity = data._Velocity;
        _TargetPositions = data._TargetPositions;
        _Storage._StoredGoods = data._StoredGoods;
        _Squad = data._Squad;
        _Squad._AttachedUnit = this;
        _IsEnemy = data._IsEnemy;
        _UnitIndex = data._UnitIndex;
        _Name = data._Name;
        transform.localPosition = data._Position;
        _Squad.Constructor(this);
    }
    public void LoadAttachedUnitsFromData(UnitData data)
    {
        if (data._AttachedToUnitObjectIndex != -1)
        {
            _AttachedToUnitObject = GetUnitObjectFromIndex(data._AttachedToUnitObjectIndex);
            if (_AttachedToUnitObject.GetComponent<Unit>()._Squad is Truck)
            {
                if (_Squad is Infantry) (_Squad as Infantry)._AttachedTruck = _AttachedToUnitObject.GetComponent<Unit>()._Squad as Truck;
                else if (_Squad is AntiTank) (_Squad as AntiTank)._TowedTo = _AttachedToUnitObject.GetComponent<Unit>()._Squad as Truck;
                else if (_Squad is AntiAir) (_Squad as AntiAir)._TowedTo = _AttachedToUnitObject.GetComponent<Unit>()._Squad as Truck;
                else if (_Squad is Artillery) (_Squad as Artillery)._TowedTo = _AttachedToUnitObject.GetComponent<Unit>()._Squad as Truck;
            }
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
    public Squad _Squad;
    public List<Goods> _StoredGoods;

    public bool _IsEnemy;

    //extras for data
    public int _AttachedToUnitObjectIndex;
    public bool _WasInNestedTruck;

    public bool _IsNaval;

    public Vector3 _Position;
    public Vector3 _Velocity;
    public List<Vector3> _TargetPositions;
    //move and attack orders//////////////////////////////////////////////////////////
}

public interface ISquadInterfaces
{

}
public interface ICarryBigUnit : ISquadInterfaces
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
    public abstract int _HealthForOneAmount { get; }
    public float _Speed = 10f;
    public virtual bool _CanAttackWhileMoving { get; }
    public bool _IsCivilian;

    public abstract int _Weight { get; }
    public int _Amount;
    public float _Morale;
    public bool _IsSquadSelected;
    public Unit _AttachedUnit;

    public Squad(Unit unit, int amount)
    {
        _Amount = amount;
        _AttachedUnit = unit;
        GameManager._Instance.CallForAction(() => unit._Name = _DisplayName, 0.25f, true);
    }

    public abstract void Constructor(Unit unit);
    public abstract Squad CreateNewSquadObject(Unit attachedUnit);
    public abstract void AddValues(Squad squadValuesToAdd);
}
public class Infantry : Squad, ICarriableUnit
{
    public override string _DisplayName => Localization._Instance._UI[24];
    public override Sprite _Icon => GameManager._Instance._UnitTypeIcons[0];
    public override int _Weight => _Amount;
    public override int _HealthForOneAmount => 10;

    public Truck _AttachedTruck;
    public Infantry(Unit unit, int amount) : base(unit, amount)
    {
        Constructor(unit);
    }
    public override void Constructor(Unit unit)
    {
        if (unit._Animator == null)
            unit.CreateModel(_Amount, this);

        unit._Animator.speed = 2f;
        unit._Animator.SetLayerWeight(1, 0f);
        unit._Animator.SetLayerWeight(2, 1f);
        unit._Animator.SetBool("IsInf", true);

        AnimatorOverrideController overrideController = new AnimatorOverrideController(unit._Animator.runtimeAnimatorController);
        overrideController["Inf_Idle"] = GameManager._Instance._InfClips[0];
        overrideController["Inf_Moving"] = GameManager._Instance._InfClips[1];
        overrideController["Inf_Fire"] = GameManager._Instance._InfClips[2];
        overrideController["Inf_Die"] = GameManager._Instance._InfClips[3];
        unit._Animator.runtimeAnimatorController = overrideController;
    }

    public override Squad CreateNewSquadObject(Unit attachedUnit)
    {
        return new Infantry(attachedUnit, 0);
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
    public override int _HealthForOneAmount => 50;

    public int _CurrentTow;//limit is 1
    public int _CurrentManCarry;
    public int _ManCarryLimit = 10;
    public Truck(Unit unit, int amount) : base(unit, amount)
    {
        Constructor(unit);
    }
    public override void Constructor(Unit unit)
    {
        if (unit._Animator == null)
            unit.CreateModel(_Amount, this);

        AnimatorOverrideController overrideController = new AnimatorOverrideController(unit._Animator.runtimeAnimatorController);
        overrideController["Inf_Idle"] = GameManager._Instance._TruckClips[0];
        overrideController["Inf_Moving"] = GameManager._Instance._TruckClips[1];
        overrideController["Inf_Fire"] = GameManager._Instance._TruckClips[2];
        overrideController["Inf_Die"] = GameManager._Instance._TruckClips[3];
        unit._Animator.runtimeAnimatorController = overrideController;
    }

    public override Squad CreateNewSquadObject(Unit attachedUnit)
    {
        return new Truck(attachedUnit, 0);
    }
    public override void AddValues(Squad squadValuesToAdd)
    {
        Truck tru = squadValuesToAdd as Truck;
        _Amount += squadValuesToAdd._Amount;
    }
}
public class Train : Squad, ITrain, ICarryBigUnit
{
    public override string _DisplayName => Localization._Instance._UI[26];
    public override Sprite _Icon => GameManager._Instance._UnitTypeIcons[2];
    public override int _Weight => _Amount * 20;
    public override int _HealthForOneAmount => 50;
    public int _CarryLimit { get => _carryLimit; set => _carryLimit = value; }
    public int _CurrentCarry { get => _currentCarry; set => _currentCarry = value; }

    private int _currentCarry;
    private int _carryLimit;

    public Train(Unit unit, int amount) : base(unit, amount)
    {
        Constructor(unit);
    }
    public override void Constructor(Unit unit)
    {
        if (unit._Animator == null)
            unit.CreateModel(_Amount, this);

        AnimatorOverrideController overrideController = new AnimatorOverrideController(unit._Animator.runtimeAnimatorController);
        overrideController["Inf_Idle"] = GameManager._Instance._TrainClips[0];
        overrideController["Inf_Moving"] = GameManager._Instance._TrainClips[1];
        overrideController["Inf_Fire"] = GameManager._Instance._TrainClips[2];
        overrideController["Inf_Die"] = GameManager._Instance._TrainClips[3];
        unit._Animator.runtimeAnimatorController = overrideController;

        _CarryLimit = 20;
    }

    public override Squad CreateNewSquadObject(Unit attachedUnit)
    {
        return new Train(attachedUnit, 0);
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
    public override int _HealthForOneAmount => 120;
    public override bool _CanAttackWhileMoving => true;
    public Tank(Unit unit, int amount) : base(unit, amount)
    {
        Constructor(unit);
    }
    public override void Constructor(Unit unit)
    {
        if (unit._Animator == null)
            unit.CreateModel(_Amount, this);
        unit._AttackModels.Add(unit.transform.Find("Model").Find("TankModel(Clone)").Find("Tank").Find("TankGroupTower"));

        AnimatorOverrideController overrideController = new AnimatorOverrideController(unit._Animator.runtimeAnimatorController);
        overrideController["Inf_Idle"] = GameManager._Instance._TankClips[0];
        overrideController["Inf_Moving"] = GameManager._Instance._TankClips[1];
        overrideController["Inf_Fire"] = GameManager._Instance._TankClips[2];
        overrideController["Inf_Die"] = GameManager._Instance._TankClips[3];
        unit._Animator.runtimeAnimatorController = overrideController;
    }
    public override Squad CreateNewSquadObject(Unit attachedUnit)
    {
        return new Tank(attachedUnit, 0);
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
    public override bool _CanAttackWhileMoving => true;
    public override int _HealthForOneAmount => 75;
    public APC(Unit unit, int amount) : base(unit, amount)
    {
        Constructor(unit);
    }
    public override void Constructor(Unit unit)
    {
        if (unit._Animator == null)
            unit.CreateModel(_Amount, this);
        unit._AttackModels.Add(unit.transform.Find("Model").Find("ApcModel(Clone)").Find("Apc").Find("Turret"));

        AnimatorOverrideController overrideController = new AnimatorOverrideController(unit._Animator.runtimeAnimatorController);
        overrideController["Inf_Idle"] = GameManager._Instance._ApcClips[0];
        overrideController["Inf_Moving"] = GameManager._Instance._ApcClips[1];
        overrideController["Inf_Fire"] = GameManager._Instance._ApcClips[2];
        overrideController["Inf_Die"] = GameManager._Instance._ApcClips[3];
        unit._Animator.runtimeAnimatorController = overrideController;
    }
    public override Squad CreateNewSquadObject(Unit attachedUnit)
    {
        return new APC(attachedUnit, 0);
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
    public override int _HealthForOneAmount => 30;
    public Truck _TowedTo;
    public Artillery(Unit unit, int amount) : base(unit, amount)
    {
        Constructor(unit);
    }
    public override void Constructor(Unit unit)
    {
        if (unit._Animator == null)
            unit.CreateModel(_Amount, this);

        AnimatorOverrideController overrideController = new AnimatorOverrideController(unit._Animator.runtimeAnimatorController);
        overrideController["Inf_Idle"] = GameManager._Instance._ArtilleryClips[0];
        overrideController["Inf_Moving"] = GameManager._Instance._ArtilleryClips[1];
        overrideController["Inf_Fire"] = GameManager._Instance._ArtilleryClips[2];
        overrideController["Inf_Die"] = GameManager._Instance._ArtilleryClips[3];
        unit._Animator.runtimeAnimatorController = overrideController;
    }
    public override Squad CreateNewSquadObject(Unit attachedUnit)
    {
        return new Artillery(attachedUnit, 0);
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
    public override int _HealthForOneAmount => 40;
    public RocketArtillery(Unit unit, int amount) : base(unit, amount)
    {
        Constructor(unit);
    }
    public override void Constructor(Unit unit)
    {
        if (unit._Animator == null)
            unit.CreateModel(_Amount, this);

        AnimatorOverrideController overrideController = new AnimatorOverrideController(unit._Animator.runtimeAnimatorController);
        overrideController["Inf_Idle"] = GameManager._Instance._RocketArtilleryClips[0];
        overrideController["Inf_Moving"] = GameManager._Instance._RocketArtilleryClips[1];
        overrideController["Inf_Fire"] = GameManager._Instance._RocketArtilleryClips[2];
        overrideController["Inf_Die"] = GameManager._Instance._RocketArtilleryClips[3];
        unit._Animator.runtimeAnimatorController = overrideController;
    }
    public override Squad CreateNewSquadObject(Unit attachedUnit)
    {
        return new RocketArtillery(attachedUnit, 0);
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
    public override int _HealthForOneAmount => 40;
    public Truck _TowedTo;
    public AntiTank(Unit unit, int amount) : base(unit, amount)
    {
        Constructor(unit);
    }
    public override void Constructor(Unit unit)
    {
        if (unit._Animator == null)
            unit.CreateModel(_Amount, this);

        AnimatorOverrideController overrideController = new AnimatorOverrideController(unit._Animator.runtimeAnimatorController);
        overrideController["Inf_Idle"] = GameManager._Instance._ATClips[0];
        overrideController["Inf_Moving"] = GameManager._Instance._ATClips[1];
        overrideController["Inf_Fire"] = GameManager._Instance._ATClips[2];
        overrideController["Inf_Die"] = GameManager._Instance._ATClips[3];
        unit._Animator.runtimeAnimatorController = overrideController;
    }
    public override Squad CreateNewSquadObject(Unit attachedUnit)
    {
        return new AntiTank(attachedUnit, 0);
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
    public override int _HealthForOneAmount => 30;
    public Truck _TowedTo;
    public AntiAir(Unit unit, int amount) : base(unit, amount)
    {
        Constructor(unit);
    }
    public override void Constructor(Unit unit)
    {
        if (unit._Animator == null)
            unit.CreateModel(_Amount, this);

        AnimatorOverrideController overrideController = new AnimatorOverrideController(unit._Animator.runtimeAnimatorController);
        overrideController["Inf_Idle"] = GameManager._Instance._AAClips[0];
        overrideController["Inf_Moving"] = GameManager._Instance._AAClips[1];
        overrideController["Inf_Fire"] = GameManager._Instance._AAClips[2];
        overrideController["Inf_Die"] = GameManager._Instance._AAClips[3];
        unit._Animator.runtimeAnimatorController = overrideController;
    }

    public override Squad CreateNewSquadObject(Unit attachedUnit)
    {
        return new AntiAir(attachedUnit, 0);
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
    public override int _HealthForOneAmount => 50;
    public override bool _CanAttackWhileMoving => true;

    public Jet(Unit unit, int amount) : base(unit, amount)
    {
        Constructor(unit);
    }
    public override void Constructor(Unit unit)
    {
        unit.transform.Find("ExtraCollider").gameObject.SetActive(false);

        if (unit._Animator == null)
            unit.CreateModel(_Amount, this);

        AnimatorOverrideController overrideController = new AnimatorOverrideController(unit._Animator.runtimeAnimatorController);
        overrideController["Inf_Idle"] = GameManager._Instance._JetClips[0];
        overrideController["Inf_Moving"] = GameManager._Instance._JetClips[1];
        overrideController["Inf_Fire"] = GameManager._Instance._JetClips[2];
        overrideController["Inf_Die"] = GameManager._Instance._JetClips[3];
        unit._Animator.runtimeAnimatorController = overrideController;

        RectTransform rect = unit.transform.Find("Model").Find("UnitUI").Find("NameAmountText").GetComponent<RectTransform>();
        rect.localPosition = new Vector3(rect.localPosition.x, 4f, rect.localPosition.z);
    }
    public override Squad CreateNewSquadObject(Unit attachedUnit)
    {
        return new Jet(attachedUnit, 0);
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
    public override int _HealthForOneAmount => 50;
    public override bool _CanAttackWhileMoving => true;

    public Helicopter(Unit unit, int amount) : base(unit, amount)
    {
        Constructor(unit);
    }
    public override void Constructor(Unit unit)
    {
        unit.transform.Find("ExtraCollider").gameObject.SetActive(false);

        if (unit._Animator == null)
            unit.CreateModel(_Amount, this);
        unit._AttackModels.Add(unit.transform.Find("Model").Find("HelicopterModel(Clone)").Find("Helicopter").Find("Accesories").Find("Machine Gun Pivot"));

        AnimatorOverrideController overrideController = new AnimatorOverrideController(unit._Animator.runtimeAnimatorController);
        overrideController["Inf_Idle"] = GameManager._Instance._HeliClips[0];
        overrideController["Inf_Moving"] = GameManager._Instance._HeliClips[1];
        overrideController["Inf_Fire"] = GameManager._Instance._HeliClips[2];
        overrideController["Inf_Die"] = GameManager._Instance._HeliClips[3];
        unit._Animator.runtimeAnimatorController = overrideController;

        RectTransform rect = unit.transform.Find("Model").Find("UnitUI").Find("NameAmountText").GetComponent<RectTransform>();
        rect.localPosition = new Vector3(rect.localPosition.x, 4f, rect.localPosition.z);
    }
    public override Squad CreateNewSquadObject(Unit attachedUnit)
    {
        return new Helicopter(attachedUnit, 0);
    }
    public override void AddValues(Squad squadValuesToAdd)
    {
        Helicopter hel = squadValuesToAdd as Helicopter;
        _Amount += squadValuesToAdd._Amount;
    }
}
public class CargoPlane : Squad, IAir, ICarryBigUnit
{
    public override string _DisplayName => Localization._Instance._UI[38];
    public override Sprite _Icon => GameManager._Instance._UnitTypeIcons[11];
    public override int _Weight => _Amount * 50;
    public override int _HealthForOneAmount => 100;
    public int _CarryLimit { get => _carryLimit; set => _carryLimit = value; }
    public int _CurrentCarry { get => _currentCarry; set => _currentCarry = value; }

    private int _currentCarry;
    private int _carryLimit;

    public CargoPlane(Unit unit, int amount) : base(unit, amount)
    {
        Constructor(unit);
    }
    public override void Constructor(Unit unit)
    {
        unit.transform.Find("ExtraCollider").gameObject.SetActive(false);

        if (unit._Animator == null)
            unit.CreateModel(_Amount, this);

        AnimatorOverrideController overrideController = new AnimatorOverrideController(unit._Animator.runtimeAnimatorController);
        overrideController["Inf_Idle"] = GameManager._Instance._CargoPlaneClips[0];
        overrideController["Inf_Moving"] = GameManager._Instance._CargoPlaneClips[1];
        overrideController["Inf_Fire"] = GameManager._Instance._CargoPlaneClips[2];
        overrideController["Inf_Die"] = GameManager._Instance._CargoPlaneClips[3];
        unit._Animator.runtimeAnimatorController = overrideController;

        _CarryLimit = 50;

        RectTransform rect = unit.transform.Find("Model").Find("UnitUI").Find("NameAmountText").GetComponent<RectTransform>();
        rect.localPosition = new Vector3(rect.localPosition.x, 4f, rect.localPosition.z);
    }
    public override Squad CreateNewSquadObject(Unit attachedUnit)
    {
        return new CargoPlane(attachedUnit, 0);
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
    public override int _HealthForOneAmount => 1000;
    public override bool _CanAttackWhileMoving => true;


    public Cruiser(Unit unit, int amount) : base(unit, amount)
    {
        Constructor(unit);
    }
    public override void Constructor(Unit unit)
    {
        if (unit._Animator == null)
            unit.CreateModel(_Amount, this);
        for (int i = 0; i < unit.transform.Find("Model").Find("CruiserModel(Clone)").Find("Cruiser").Find("Weapons").childCount; i++)
        {
            unit._AttackModels.Add(unit.transform.Find("Model").Find("CruiserModel(Clone)").Find("Cruiser").Find("Weapons").GetChild(i));
        }

        AnimatorOverrideController overrideController = new AnimatorOverrideController(unit._Animator.runtimeAnimatorController);
        overrideController["Inf_Idle"] = GameManager._Instance._CruiserClips[0];
        overrideController["Inf_Moving"] = GameManager._Instance._CruiserClips[1];
        overrideController["Inf_Fire"] = GameManager._Instance._CruiserClips[2];
        overrideController["Inf_Die"] = GameManager._Instance._CruiserClips[3];
        unit._Animator.runtimeAnimatorController = overrideController;
    }
    public override Squad CreateNewSquadObject(Unit attachedUnit)
    {
        return new Cruiser(attachedUnit, 0);
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
    public override int _HealthForOneAmount => 300;
    public override bool _CanAttackWhileMoving => true;

    public Destroyer(Unit unit, int amount) : base(unit, amount)
    {
        Constructor(unit);
    }
    public override void Constructor(Unit unit)
    {
        if (unit._Animator == null)
            unit.CreateModel(_Amount, this);
        for (int i = 0; i < unit.transform.Find("Model").Find("DestroyerModel(Clone)").Find("Destroyer").Find("Weapons").childCount; i++)
        {
            unit._AttackModels.Add(unit.transform.Find("Model").Find("DestroyerModel(Clone)").Find("Destroyer").Find("Weapons").GetChild(i));
        }

        AnimatorOverrideController overrideController = new AnimatorOverrideController(unit._Animator.runtimeAnimatorController);
        overrideController["Inf_Idle"] = GameManager._Instance._DestroyerClips[0];
        overrideController["Inf_Moving"] = GameManager._Instance._DestroyerClips[1];
        overrideController["Inf_Fire"] = GameManager._Instance._DestroyerClips[2];
        overrideController["Inf_Die"] = GameManager._Instance._DestroyerClips[3];
        unit._Animator.runtimeAnimatorController = overrideController;
    }
    public override Squad CreateNewSquadObject(Unit attachedUnit)
    {
        return new Destroyer(attachedUnit, 0);
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
    public override int _HealthForOneAmount => 90;
    public override bool _CanAttackWhileMoving => true;

    public Corvette(Unit unit, int amount) : base(unit, amount)
    {
        Constructor(unit);
    }
    public override void Constructor(Unit unit)
    {
        if (unit._Animator == null)
            unit.CreateModel(_Amount, this);
        for (int i = 0; i < unit.transform.Find("Model").Find("CorvetteModel(Clone)").Find("Corvette").Find("Weapons").childCount; i++)
        {
            unit._AttackModels.Add(unit.transform.Find("Model").Find("CorvetteModel(Clone)").Find("Corvette").Find("Weapons").GetChild(i));
        }

        AnimatorOverrideController overrideController = new AnimatorOverrideController(unit._Animator.runtimeAnimatorController);
        overrideController["Inf_Idle"] = GameManager._Instance._CorvetteClips[0];
        overrideController["Inf_Moving"] = GameManager._Instance._CorvetteClips[1];
        overrideController["Inf_Fire"] = GameManager._Instance._CorvetteClips[2];
        overrideController["Inf_Die"] = GameManager._Instance._CorvetteClips[3];
        unit._Animator.runtimeAnimatorController = overrideController;
    }
    public override Squad CreateNewSquadObject(Unit attachedUnit)
    {
        return new Corvette(attachedUnit, 0);
    }
    public override void AddValues(Squad squadValuesToAdd)
    {
        Corvette cor = squadValuesToAdd as Corvette;
        _Amount += squadValuesToAdd._Amount;
    }
}

public class TransportShip : Squad, INavy, ICarryBigUnit
{
    public override string _DisplayName => Localization._Instance._UI[31];
    public override Sprite _Icon => GameManager._Instance._UnitTypeIcons[15];
    public override int _Weight => _Amount * 100;
    public override int _HealthForOneAmount => 50;

    public int _CarryLimit { get => _carryLimit; set => _carryLimit = value; }
    public int _CurrentCarry { get => _currentCarry; set => _currentCarry = value; }

    private int _currentCarry;
    private int _carryLimit;

    public TransportShip(Unit unit, int amount) : base(unit, amount)
    {
        Constructor(unit);
    }
    public override void Constructor(Unit unit)
    {
        if (unit._Animator == null)
            unit.CreateModel(_Amount, this);

        AnimatorOverrideController overrideController = new AnimatorOverrideController(unit._Animator.runtimeAnimatorController);
        overrideController["Inf_Idle"] = GameManager._Instance._TransportShipClips[0];
        overrideController["Inf_Moving"] = GameManager._Instance._TransportShipClips[1];
        overrideController["Inf_Fire"] = GameManager._Instance._TransportShipClips[2];
        overrideController["Inf_Die"] = GameManager._Instance._TransportShipClips[3];
        unit._Animator.runtimeAnimatorController = overrideController;

        _CarryLimit = 100;
    }
    public override Squad CreateNewSquadObject(Unit attachedUnit)
    {
        return new TransportShip(attachedUnit, 0);
    }
    public override void AddValues(Squad squadValuesToAdd)
    {
        TransportShip tra = squadValuesToAdd as TransportShip;
        _Amount += squadValuesToAdd._Amount;
        _CurrentCarry += tra._CurrentCarry;
    }
}
