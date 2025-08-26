using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public static CameraController _Instance;

    public float _MoveSpeedLimit { get; set; }
    public float _LerpSpeed { get; set; }

    public Vector3 _MoveInput { get; set; }
    public Vector3 _MoveSpeed { get; set; }
    public float _TargetSpeedMagnitude { get; set; }

    public Vector3 _ZoomInput { get; set; }
    public float _TargetZoom { get; set; }
    public float _TargetXAngle { get; set; }

    public Vector3 _TargetSpeed { get; set; }

    public float _InputXOffset { get; set; }

    public float _TargetYAngle;

    private float _baseYPos;

    private void Awake()
    {
        _Instance = this;
        _TargetYAngle = -80f;
    }

    private void Update()
    {
        if (GameManager._Instance._IsGameStopped) return;

        Physics.Raycast(transform.position + Vector3.up * 2000f, -Vector3.up, out RaycastHit hit, 20000f, GameManager._Instance._TerrainAndWaterLayers);
        _baseYPos = hit.point.y - 8f;
        transform.parent.transform.position = new Vector3(transform.parent.position.x, Mathf.Clamp(Mathf.Lerp(transform.parent.position.y, _baseYPos, Time.deltaTime * 5f), _baseYPos, float.MaxValue), transform.parent.position.z);

        if (GameManager._Instance._InputActions.FindAction("RotateCamera").ReadValue<float>() != 0)
        {
            _TargetYAngle += Mouse.current.delta.x.ReadValue() * Time.deltaTime * 12f;
            _InputXOffset -= Mouse.current.delta.y.ReadValue() * Time.deltaTime * 25f;
        }
        _InputXOffset = Mathf.Clamp(_InputXOffset, -40f, 5f);

        _TargetYAngle = _TargetYAngle % 360f;
        if (_TargetYAngle < 0)
            _TargetYAngle += 360f;
        transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, Mathf.LerpAngle(transform.localEulerAngles.y, _TargetYAngle, Time.deltaTime * 15), transform.localEulerAngles.z);
    }
}
public abstract class MapState
{
    public abstract void Enter(MapState oldState);
    public abstract void Exit(MapState newState);
    public abstract void Update();
    public abstract void LateUpdate();
}

public class TacticalMapState : MapState
{
    private float _zoomAmount;
    public GameObject _PanObj;
    private Vector3 _panOffset;
    private float _panDistance;

    public override void Enter(MapState oldState)
    {
        _zoomAmount = 70f;
        CameraController._Instance._TargetZoom = 500f;
        CameraController._Instance._TargetXAngle = 60f;
        CameraController._Instance._MoveSpeedLimit = 40f;
        CameraController._Instance._LerpSpeed = 3.5f;

        CameraController._Instance._InputXOffset -= 25f;

        _panDistance = 20f;
    }
    public override void Exit(MapState newState)
    {

    }
    public override void Update()
    {
        if (GameManager._Instance._IsGameStopped)
            return;

        if (GameManager._Instance._InputActions.FindAction("ChangeMapState").triggered)
        {
            GameManager._Instance.ChangeMapState(new StrategicMapState());
            return;
        }

        if (GameManager._Instance._InputActions.FindAction("PanToObj").triggered)
        {
            if (_PanObj == null && GameInputController._Instance._SelectedUnits.Count > 0)
            {
                _PanObj = GameInputController._Instance._SelectedUnits[GameInputController._Instance._SelectedUnits.Count - 1];
                _panOffset = new Vector3((_PanObj.transform.position - CameraController._Instance.transform.position).x, 0f, (_PanObj.transform.position - CameraController._Instance.transform.position).z).normalized;
                _panOffset.y = -0.3f;
            }
            else
            {
                _PanObj = null;
            }

        }

        if (_PanObj == null)
        {
            CameraController._Instance._MoveInput = GameManager._Instance._InputActions.FindAction("Move").ReadValue<Vector2>();
            CameraController._Instance._MoveInput = new Vector3(CameraController._Instance._MoveInput.x, 0f, CameraController._Instance._MoveInput.y);
            CameraController._Instance._MoveInput = Quaternion.Euler(0f, CameraController._Instance.transform.localEulerAngles.y, 0f) * CameraController._Instance._MoveInput;

            CameraController._Instance._TargetSpeedMagnitude = CameraController._Instance._MoveInput != Vector3.zero ? CameraController._Instance._MoveSpeedLimit : 0f;
            CameraController._Instance._TargetSpeed = CameraController._Instance._MoveInput.normalized * CameraController._Instance._TargetSpeedMagnitude * (GameManager._Instance._InputActions.FindAction("Sprint").ReadValue<float>() == 0 ? 1f : 3.2f);
            CameraController._Instance._MoveSpeed = Vector3.Lerp(CameraController._Instance._MoveSpeed, CameraController._Instance._TargetSpeed, Time.deltaTime *
                (CameraController._Instance._TargetSpeedMagnitude != 0f ? CameraController._Instance._LerpSpeed : CameraController._Instance._LerpSpeed * 1.5f));
        }
        else
        {
            CameraController._Instance._MoveSpeed = Vector3.zero;

            Vector2 input = GameManager._Instance._InputActions.FindAction("Move").ReadValue<Vector2>();
            CameraController._Instance._TargetYAngle -= input.x * Time.deltaTime * 60f * 2.315f;
            
            Quaternion rot = Quaternion.AngleAxis(-input.x * 140f * Time.deltaTime, Vector3.up);
            _panOffset = rot * _panOffset;
            _panOffset.y -= input.y * Time.deltaTime * 2.5f;
            _panOffset.y = Mathf.Clamp(_panOffset.y, -0.8f, 0.8f);
            _panOffset = _panOffset.normalized;
        }


        if (!(EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()))
        {
            CameraController._Instance._ZoomInput = GameManager._Instance._InputActions.FindAction("ScrollWheel").ReadValue<Vector2>();

            if (_PanObj == null)
            {
                if (CameraController._Instance._ZoomInput.y == 1f && CameraController._Instance._TargetZoom > 20f)
                {
                    CameraController._Instance._TargetZoom -= _zoomAmount * (Mathf.Clamp(CameraController._Instance._TargetZoom, 37.5f, 500f) / 400f);
                }
                else if (CameraController._Instance._ZoomInput.y == -1f && CameraController._Instance._TargetZoom < 900f)
                {
                    CameraController._Instance._TargetZoom += _zoomAmount * (Mathf.Clamp(CameraController._Instance._TargetZoom, 37.5f, 500f) / 400f);
                }
            }
            else
            {
                if (CameraController._Instance._ZoomInput.y == 1f)
                    _panDistance -= 2f;
                else if (CameraController._Instance._ZoomInput.y == -1f)
                    _panDistance += 2f;
                _panDistance = Mathf.Clamp(_panDistance, 10f, 30f);
            }

        }

    }
    public override void LateUpdate()
    {
        if (GameManager._Instance._IsGameStopped)
            return;

        if (_PanObj != null)
        {
            //CameraController._Instance.transform.position = Vector3.Lerp(CameraController._Instance.transform.position, _PanObj.transform.position - _panOffset * _panDistance, Time.deltaTime * 15f);
            CameraController._Instance.transform.position = _PanObj.transform.position - _panOffset * _panDistance;
        }

        float targetXAngle = CameraController._Instance._TargetXAngle + CameraController._Instance._InputXOffset;
        CameraController._Instance.transform.localPosition += Time.deltaTime * CameraController._Instance._MoveSpeed * 6f;
        if (_PanObj == null)
            CameraController._Instance.transform.localPosition = new Vector3(CameraController._Instance.transform.localPosition.x, Mathf.Lerp(CameraController._Instance.transform.localPosition.y, CameraController._Instance._TargetZoom, Time.deltaTime * 5f), CameraController._Instance.transform.localPosition.z);
        CameraController._Instance.transform.localEulerAngles = new Vector3(Mathf.Lerp(CameraController._Instance.transform.localEulerAngles.x, targetXAngle, Time.deltaTime * 5f), CameraController._Instance.transform.localEulerAngles.y, CameraController._Instance.transform.localEulerAngles.z);
        CameraController._Instance.transform.localPosition = new Vector3(Mathf.Clamp(CameraController._Instance.transform.localPosition.x, -16000f, 16000f), Mathf.Clamp(CameraController._Instance.transform.localPosition.y, 10f, float.MaxValue), Mathf.Clamp(CameraController._Instance.transform.localPosition.z, -16000f, 16000f));
    }
}
public class StrategicMapState : MapState
{
    private float _zoomAmount;

    public override void Enter(MapState oldState)
    {
        _zoomAmount = 300f;
        CameraController._Instance._TargetZoom = 3000f;
        CameraController._Instance._TargetXAngle = 60f;
        CameraController._Instance._MoveSpeedLimit = 80f;
        CameraController._Instance._LerpSpeed = 4f;

        CameraController._Instance._InputXOffset += 25f;

        var terrains = GameObject.FindObjectsByType<Terrain>(FindObjectsSortMode.None);
        foreach (var terrain in terrains)
        {
            terrain.drawTreesAndFoliage = false;
        }
    }
    public override void Exit(MapState newState)
    {
        var terrains = GameObject.FindObjectsByType<Terrain>(FindObjectsSortMode.None);
        foreach (var terrain in terrains)
        {
            terrain.drawTreesAndFoliage = true;
        }
    }
    public override void Update()
    {
        if (GameManager._Instance._IsGameStopped)
            return;

        if (GameManager._Instance._InputActions.FindAction("ChangeMapState").triggered)
        {
            GameManager._Instance.ChangeMapState(new TacticalMapState());
            return;
        }

        CameraController._Instance._MoveInput = GameManager._Instance._InputActions.FindAction("Move").ReadValue<Vector2>();
        CameraController._Instance._MoveInput = new Vector3(CameraController._Instance._MoveInput.x, 0f, CameraController._Instance._MoveInput.y);
        CameraController._Instance._MoveInput = Quaternion.Euler(0f, CameraController._Instance.transform.localEulerAngles.y, 0f) * CameraController._Instance._MoveInput;

        CameraController._Instance._TargetSpeedMagnitude = CameraController._Instance._MoveInput != Vector3.zero ? CameraController._Instance._MoveSpeedLimit : 0f;
        CameraController._Instance._TargetSpeed = CameraController._Instance._MoveInput.normalized * CameraController._Instance._TargetSpeedMagnitude * (GameManager._Instance._InputActions.FindAction("Sprint").ReadValue<float>() == 0 ? 1f : 2.3f);
        CameraController._Instance._MoveSpeed = Vector3.Lerp(CameraController._Instance._MoveSpeed, CameraController._Instance._TargetSpeed, Time.deltaTime *
            (CameraController._Instance._TargetSpeedMagnitude != 0f ? CameraController._Instance._LerpSpeed : CameraController._Instance._LerpSpeed * 1.5f));

        if (!(EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()))
        {
            CameraController._Instance._ZoomInput = GameManager._Instance._InputActions.FindAction("ScrollWheel").ReadValue<Vector2>();
            if (CameraController._Instance._ZoomInput.y == 1f && CameraController._Instance._TargetZoom > 1600f)
            {
                CameraController._Instance._TargetZoom -= _zoomAmount;
            }
            else if (CameraController._Instance._ZoomInput.y == -1f && CameraController._Instance._TargetZoom < 4750f)
            {
                CameraController._Instance._TargetZoom += _zoomAmount;
            }
        }
    }
    public override void LateUpdate()
    {
        if (GameManager._Instance._IsGameStopped)
            return;

        float targetXAngle = CameraController._Instance._TargetXAngle + CameraController._Instance._InputXOffset / 2.35f;
        CameraController._Instance.transform.localPosition += Time.deltaTime * CameraController._Instance._MoveSpeed * 30f;
        CameraController._Instance.transform.localPosition = new Vector3(CameraController._Instance.transform.localPosition.x, Mathf.Lerp(CameraController._Instance.transform.localPosition.y, CameraController._Instance._TargetZoom, Time.deltaTime * 5f), CameraController._Instance.transform.localPosition.z);
        CameraController._Instance.transform.localEulerAngles = new Vector3(Mathf.Lerp(CameraController._Instance.transform.localEulerAngles.x, targetXAngle, Time.deltaTime * 5f), CameraController._Instance.transform.localEulerAngles.y, CameraController._Instance.transform.localEulerAngles.z);
        CameraController._Instance.transform.localPosition = new Vector3(Mathf.Clamp(CameraController._Instance.transform.localPosition.x, -16000f, 16000f), CameraController._Instance.transform.localPosition.y, Mathf.Clamp(CameraController._Instance.transform.localPosition.z, -16000f, 16000f));

    }
}