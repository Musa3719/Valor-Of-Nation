using UnityEngine;
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

    private float _baseYPos;
    private float _targetYAngle;

    private void Awake()
    {
        _Instance = this;
        _targetYAngle = -80f;
    }

    private void Update()
    {
        if (GameManager._Instance._IsGameStopped) return;

        Physics.Raycast(transform.position, -Vector3.up, out RaycastHit hit, 20000f, GameManager._Instance._TerrainRayLayers);
        _baseYPos = hit.point.y;
        transform.parent.transform.position = new Vector3(transform.parent.transform.position.x, Mathf.Lerp(transform.parent.transform.position.y, _baseYPos, Time.deltaTime * 3f), transform.parent.transform.position.z);

        if (GameManager._Instance._InputActions.FindAction("RotateCamera").ReadValue<float>() != 0)
            _targetYAngle += Mouse.current.delta.x.ReadValue() * 0.18f;
        _targetYAngle = _targetYAngle % 360f;
        if (_targetYAngle < 0)
            _targetYAngle += 360f;
        transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, Mathf.LerpAngle(transform.localEulerAngles.y, _targetYAngle, Time.deltaTime * 15), transform.localEulerAngles.z);
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

    public override void Enter(MapState oldState)
    {
        _zoomAmount = 40f;
        CameraController._Instance._TargetZoom = 450f;
        CameraController._Instance._TargetXAngle = 45f;
        CameraController._Instance._MoveSpeedLimit = 40f;
        CameraController._Instance._LerpSpeed = 3.5f;
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

        CameraController._Instance._MoveInput = GameManager._Instance._InputActions.FindAction("Move").ReadValue<Vector2>();
        CameraController._Instance._MoveInput = new Vector3(CameraController._Instance._MoveInput.x, 0f, CameraController._Instance._MoveInput.y);
        CameraController._Instance._MoveInput = Quaternion.Euler(0f, CameraController._Instance.transform.localEulerAngles.y, 0f) * CameraController._Instance._MoveInput;

        CameraController._Instance._TargetSpeedMagnitude = CameraController._Instance._MoveInput != Vector3.zero ? CameraController._Instance._MoveSpeedLimit : 0f;
        CameraController._Instance._TargetSpeed = CameraController._Instance._MoveInput.normalized * CameraController._Instance._TargetSpeedMagnitude * (GameManager._Instance._InputActions.FindAction("Sprint").ReadValue<float>() == 0 ? 1f : 3.2f);
        CameraController._Instance._MoveSpeed = Vector3.Lerp(CameraController._Instance._MoveSpeed, CameraController._Instance._TargetSpeed, Time.deltaTime *
            (CameraController._Instance._TargetSpeedMagnitude != 0f ? CameraController._Instance._LerpSpeed : CameraController._Instance._LerpSpeed * 1.5f));

        CameraController._Instance._ZoomInput = GameManager._Instance._InputActions.FindAction("ScrollWheel").ReadValue<Vector2>();
        if (CameraController._Instance._ZoomInput.y == 1f && CameraController._Instance._TargetZoom > 200f)
        {
            CameraController._Instance._TargetZoom -= _zoomAmount;
        }
        else if (CameraController._Instance._ZoomInput.y == -1f && CameraController._Instance._TargetZoom < 900f)
        {
            CameraController._Instance._TargetZoom += _zoomAmount;
        }

    }
    public override void LateUpdate()
    {
        if (GameManager._Instance._IsGameStopped)
            return;

        CameraController._Instance.transform.localPosition += Time.deltaTime * CameraController._Instance._MoveSpeed * 6f;
        CameraController._Instance.transform.localPosition = new Vector3(CameraController._Instance.transform.localPosition.x, Mathf.Lerp(CameraController._Instance.transform.localPosition.y, CameraController._Instance._TargetZoom, Time.deltaTime * 5f), CameraController._Instance.transform.localPosition.z);
        CameraController._Instance.transform.localEulerAngles = new Vector3(Mathf.Lerp(CameraController._Instance.transform.localEulerAngles.x, CameraController._Instance._TargetXAngle, Time.deltaTime * 5f), CameraController._Instance.transform.localEulerAngles.y, CameraController._Instance.transform.localEulerAngles.z);
        CameraController._Instance.transform.localPosition = new Vector3(Mathf.Clamp(CameraController._Instance.transform.localPosition.x, -16000f, 16000f), CameraController._Instance.transform.localPosition.y, Mathf.Clamp(CameraController._Instance.transform.localPosition.z, -16000f, 16000f));

    }
}
public class StrategicMapState : MapState
{
    private float _zoomAmount;

    public override void Enter(MapState oldState)
    {
        _zoomAmount = 200f;
        CameraController._Instance._TargetZoom = 3000f;
        CameraController._Instance._TargetXAngle = 60f;
        CameraController._Instance._MoveSpeedLimit = 80f;
        CameraController._Instance._LerpSpeed = 4f;

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

        CameraController._Instance._ZoomInput = GameManager._Instance._InputActions.FindAction("ScrollWheel").ReadValue<Vector2>();
        if (CameraController._Instance._ZoomInput.y == 1f && CameraController._Instance._TargetZoom > 1600f)
        {
            CameraController._Instance._TargetZoom -= _zoomAmount;
        }
        else if (CameraController._Instance._ZoomInput.y == -1f && CameraController._Instance._TargetZoom < 7500f)
        {
            CameraController._Instance._TargetZoom += _zoomAmount;
        }

    }
    public override void LateUpdate()
    {
        if (GameManager._Instance._IsGameStopped)
            return;

        CameraController._Instance.transform.localPosition += Time.deltaTime * CameraController._Instance._MoveSpeed * 30f;
        CameraController._Instance.transform.localPosition = new Vector3(CameraController._Instance.transform.localPosition.x, Mathf.Lerp(CameraController._Instance.transform.localPosition.y, CameraController._Instance._TargetZoom, Time.deltaTime * 5f), CameraController._Instance.transform.localPosition.z);
        CameraController._Instance.transform.localEulerAngles = new Vector3(Mathf.Lerp(CameraController._Instance.transform.localEulerAngles.x, CameraController._Instance._TargetXAngle, Time.deltaTime * 5f), CameraController._Instance.transform.localEulerAngles.y, CameraController._Instance.transform.localEulerAngles.z);
        CameraController._Instance.transform.localPosition = new Vector3(Mathf.Clamp(CameraController._Instance.transform.localPosition.x, -16000f, 16000f), CameraController._Instance.transform.localPosition.y, Mathf.Clamp(CameraController._Instance.transform.localPosition.z, -16000f, 16000f));

    }
}