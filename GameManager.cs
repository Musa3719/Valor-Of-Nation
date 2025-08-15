using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Collections;
using UnityEngine.EventSystems;
using Gaia;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameManager : MonoBehaviour
{
    public InputActionAsset _InputActions;
    public LayerMask _TerrainAndWaterLayers;
    public LayerMask _TerrainWaterAndUpperLayers;
    public Gradient _SelectedGradientForCurrentRoute;
    public Gradient _NotSelectedGradientForCurrentRoute;
    public GameObject _LandUnitPrefab;
    public GameObject _NavalUnitPrefab;
    public GameObject _UnitUIPrefab;
    public GameObject _SquadUIPrefab;
    public Sprite _InfantryAttachedToTrucksIcon;
    public Sprite _ArtilleryTowedIcon;
    public Sprite _AntiTankTowedIcon;
    public Sprite _AntiAirTowedIcon;
    public List<Sprite> _UnitTypeIcons;
    public List<GameObject> _UnitModelPrefabs;

    public static GameManager _Instance;

    public GameObject _MainCamera { get; private set; }
    public GameObject _StopScreen { get; private set; }
    public GameObject _InGameScreen { get; private set; }
    public GameObject _OptionsScreen { get; private set; }
    public TextMeshProUGUI _ProcessingScreenText { get; private set; }
    public GameObject _RoadCannotPlaceText { get; private set; }
    public GameObject _ConstructionScreen { get; private set; }
    public GameObject _LoadingScreen { get; private set; }

    public int _GameSpeed { get => (int)transform.localEulerAngles.x; private set => transform.localEulerAngles = new Vector3(value, 0f, 0f); }
    public bool _IsGameStopped { get; private set; }
    public bool _IsGameLoading { get; set; }
    public int _LevelIndex { get; private set; }
    public int _LastLoadedGameIndex { get; set; }

    public GraphicRaycaster _Raycaster { get; set; }
    public PointerEventData _PointerEventData { get; set; }
    public EventSystem _EventSystem { get; set; }
    public MapState _MapState;

    public int _LastCreatedUnitIndex { get; set; }

    private Coroutine _slowTimeCoroutine;

    private void Awake()
    {
        _Instance = this;
        _GameSpeed = 1;
        _MainCamera = Camera.main.gameObject;
        //Application.targetFrameRate = 30;
        _OptionsScreen = GameObject.FindGameObjectWithTag("UI").transform.Find("Options").gameObject;
        _LoadingScreen = GameObject.FindGameObjectWithTag("UI").transform.Find("Loading").gameObject;

        _LevelIndex = SceneManager.GetActiveScene().buildIndex;
        if (_LevelIndex != 0)
        {
            _StopScreen = GameObject.FindGameObjectWithTag("UI").transform.Find("StopScreen").gameObject;
            _InGameScreen = GameObject.FindGameObjectWithTag("UI").transform.Find("InGameScreen").gameObject;
            _ProcessingScreenText = GameObject.FindGameObjectWithTag("UI").transform.Find("ProcessingScreen").Find("Text (TMP)").GetComponent<TextMeshProUGUI>();
            _RoadCannotPlaceText = GameObject.FindGameObjectWithTag("UI").transform.Find("InGameScreen").transform.Find("RoadCannotPlaceText").gameObject;
            _ConstructionScreen = GameObject.FindGameObjectWithTag("UI").transform.Find("InGameScreen").Find("ConstructionScreen").gameObject;
            _Raycaster = GameObject.Find("UI").GetComponent<GraphicRaycaster>();
            _EventSystem = FindFirstObjectByType<EventSystem>();
        }

    }

    private void Start()
    {
        StartButtonEvents();
        if (_LevelIndex != 0)
        {
            ChangeMapState(new StrategicMapState());
            UnstopGame();
        }
    }

    private void Testing()
    {

        if (Input.GetKeyDown(KeyCode.T))
        {
            Infantry inf = new Infantry(GameInputController._Instance._SelectedUnits[0].GetComponent<Unit>());
            inf._Amount = (int)Time.time;
            GameInputController._Instance._SelectedUnits[0].GetComponent<Unit>().AddSquad(inf);
            //new GetOnShipOrder().ExecuteOrder(GameInputController._Instance._SelectedUnits[0]);
        }
        if (Input.GetKeyDown(KeyCode.Y))
        {
            GameInputController._Instance.DeSelectSquad(GameInputController._Instance._SelectedUnits[0].GetComponent<Unit>()._Squads[0]);
            GameInputController._Instance._SelectedUnits[0].GetComponent<Unit>().RemoveSquad(GameInputController._Instance._SelectedUnits[0].GetComponent<Unit>()._Squads[0]);
            //new GetOnTruckOrder().ExecuteOrder(GameInputController._Instance._SelectedObjects[0]);
            //new EvacuateShipOrder().ExecuteOrder(GameInputController._Instance._SelectedUnits[0]);

        }
        if (Input.GetKeyDown(KeyCode.U))
        {
            //new GetOffTruckOrder().ExecuteOrder(GameInputController._Instance._SelectedObjects[0]);
            //new EvacuateShipOrder().ExecuteOrder(GameInputController._Instance._SelectedObjects[0]);
        }

        if (Input.GetKeyDown(KeyCode.L)) { SaveSystemHandler._Instance.LoadGame(0); }
        if (Input.GetKeyDown(KeyCode.K)) { SaveSystemHandler._Instance.SaveGame(0); }

    }
    private void Update()
    {
        if (_LevelIndex != 0)
        {
            Testing();

            _MapState.Update();

            if (_InputActions.FindAction("Construction").triggered && _LevelIndex != 0 && !_IsGameStopped)
            {
                OpenOrCloseConstructionScreen(!_ConstructionScreen.activeInHierarchy);
            }
        }


        if (_InputActions.FindAction("Cancel").triggered)
        {
            if (_LevelIndex != 0)
            {
                if (_IsGameStopped)
                {
                    if (_OptionsScreen.activeInHierarchy)
                        CloseOptionsScreen();
                    else if (_StopScreen.activeInHierarchy)
                        UnstopGame();
                    else
                        StopGame();
                }
                else
                {
                    if (_ConstructionScreen.activeInHierarchy && RoadBuilder._Instance._ActiveSplineContainer != null)
                        RoadBuilder._Instance.ChangeActiveSplineContainer(null);
                    else if (_ConstructionScreen.activeInHierarchy)
                        OpenOrCloseConstructionScreen(false);
                    else if (GameInputController._Instance._SelectedUnits.Count > 0)
                    {
                        GameInputController._Instance._SelectedUnits.ClearSelected();
                        GameInputController._Instance.CloseAllInGameOtherUI();
                    }
                    else if (_InGameScreen.transform.Find("OtherInGameMenus").gameObject.activeInHierarchy)
                        GameInputController._Instance.CloseAllInGameOtherUI();
                    else
                        StopGame();
                }
            }
            else
            {
                if (_OptionsScreen.activeInHierarchy)
                    CloseOptionsScreen();
            }
        }
    }
    private void LateUpdate()
    {
        if (_LevelIndex != 0)
            _MapState.LateUpdate();
    }

    public GameObject CreateUnit(Vector3 position, bool isNaval, bool isEnemy = false, List<Squad> squads = null)
    {
        GameObject newUnit = Instantiate(isNaval ? _NavalUnitPrefab : _LandUnitPrefab, position, Quaternion.identity);
        Unit unitComponent = newUnit.GetComponent<Unit>();
        unitComponent._IsEnemy = isEnemy;

        if (squads != null)
            unitComponent._Squads = squads;

        return newUnit;
    }
    public Transform[] GetNearChildTransforms(Transform parent)
    {
        int childCount = parent.childCount;
        Transform[] children = new Transform[childCount];

        for (int i = 0; i < childCount; i++)
        {
            children[i] = parent.GetChild(i);
        }

        return children;
    }
    public T GetThisTypeOfSquad<T>(GameObject executerObject) where T : class
    {
        foreach (var squad in executerObject.GetComponent<Unit>()._Squads)
        {
            if (squad is T)
                return squad as T;
        }
        return null;
    }

    private void StartButtonEvents()
    {
        if (_LevelIndex == 0)
        {
            GameObject.FindGameObjectWithTag("UI").transform.Find("MainMenu").Find("Play").GetComponent<Button>().onClick.AddListener(SoundManager._Instance.PlayButtonSound);
            GameObject.FindGameObjectWithTag("UI").transform.Find("MainMenu").Find("Play").GetComponent<Button>().onClick.AddListener(ToGameScene);
            GameObject.FindGameObjectWithTag("UI").transform.Find("MainMenu").Find("LoadGame").GetComponent<Button>().onClick.AddListener(SoundManager._Instance.PlayButtonSound);
            GameObject.FindGameObjectWithTag("UI").transform.Find("MainMenu").Find("LoadGame").GetComponent<Button>().onClick.AddListener(ToGameScene);
            GameObject.FindGameObjectWithTag("UI").transform.Find("MainMenu").Find("Options").GetComponent<Button>().onClick.AddListener(SoundManager._Instance.PlayButtonSound);
            GameObject.FindGameObjectWithTag("UI").transform.Find("MainMenu").Find("Options").GetComponent<Button>().onClick.AddListener(OpenOptionsScreen);
            GameObject.FindGameObjectWithTag("UI").transform.Find("MainMenu").Find("Exit").GetComponent<Button>().onClick.AddListener(SoundManager._Instance.PlayButtonSound);
            GameObject.FindGameObjectWithTag("UI").transform.Find("MainMenu").Find("Exit").GetComponent<Button>().onClick.AddListener(QuitGame);
            GameObject.FindGameObjectWithTag("UI").transform.Find("Options").Find("BackToMenu").GetComponent<Button>().onClick.AddListener(SoundManager._Instance.PlayButtonSound);
            GameObject.FindGameObjectWithTag("UI").transform.Find("Options").Find("BackToMenu").GetComponent<Button>().onClick.AddListener(() => CloseOptionsScreen(true));
            GameObject.FindGameObjectWithTag("UI").transform.Find("Options").Find("Language").Find("PreviousLanguage").GetComponent<Button>().onClick.AddListener(SoundManager._Instance.PlayButtonSound);
            GameObject.FindGameObjectWithTag("UI").transform.Find("Options").Find("Language").Find("PreviousLanguage").GetComponent<Button>().onClick.AddListener(Localization._Instance.PreviousLanguage);
            GameObject.FindGameObjectWithTag("UI").transform.Find("Options").Find("Language").Find("NextLanguage").GetComponent<Button>().onClick.AddListener(SoundManager._Instance.PlayButtonSound);
            GameObject.FindGameObjectWithTag("UI").transform.Find("Options").Find("Language").Find("NextLanguage").GetComponent<Button>().onClick.AddListener(Localization._Instance.NextLanguage);
            GameObject.FindGameObjectWithTag("UI").transform.Find("Options").Find("MusicSlider").GetComponent<Slider>().onValueChanged.AddListener(Options._Instance.MusicVolumeChanged);
            GameObject.FindGameObjectWithTag("UI").transform.Find("Options").Find("SoundSlider").GetComponent<Slider>().onValueChanged.AddListener(Options._Instance.SoundVolumeChanged);
        }
        else
        {
            GameInputController._Instance._ArmyUIContent.transform.parent.parent.parent.Find("OrderUI").Find("MoveOrderButton").GetComponent<Button>().onClick.AddListener(SoundManager._Instance.PlayButtonSound);
            GameInputController._Instance._ArmyUIContent.transform.parent.parent.parent.Find("OrderUI").Find("MoveOrderButton").GetComponent<Button>().onClick.AddListener(() => GameInputController._Instance._CurrentPlayerOrder = new MoveOrder());
            GameInputController._Instance._ArmyUIContent.transform.parent.parent.parent.Find("OrderUI").Find("StopOrderButton").GetComponent<Button>().onClick.AddListener(SoundManager._Instance.PlayButtonSound);
            GameInputController._Instance._ArmyUIContent.transform.parent.parent.parent.Find("OrderUI").Find("StopOrderButton").GetComponent<Button>().onClick.AddListener(() => GameInputController._Instance.StopOrderButtonClicked());
            GameInputController._Instance._ArmyUIContent.transform.parent.parent.parent.Find("OrderUI").Find("SelectAllButton").GetComponent<Button>().onClick.AddListener(SoundManager._Instance.PlayButtonSound);
            GameInputController._Instance._ArmyUIContent.transform.parent.parent.parent.Find("OrderUI").Find("SelectAllButton").GetComponent<Button>().onClick.AddListener(() => GameInputController._Instance.SelectAllSquadsButtonClicked());
            GameInputController._Instance._ArmyUIContent.transform.parent.parent.parent.Find("OrderUI").Find("OrderTypeButton").GetComponent<Button>().onClick.AddListener(SoundManager._Instance.PlayButtonSound);
            GameInputController._Instance._ArmyUIContent.transform.parent.parent.parent.Find("OrderUI").Find("OrderTypeButton").GetComponent<Button>().onClick.AddListener(() => GameInputController._Instance.OrderTypeButtonClicked());
            GameInputController._Instance._ArmyUIContent.transform.parent.parent.parent.Find("OrderUI").Find("ToNewUnitButton").GetComponent<Button>().onClick.AddListener(SoundManager._Instance.PlayButtonSound);
            GameInputController._Instance._ArmyUIContent.transform.parent.parent.parent.Find("OrderUI").Find("ToNewUnitButton").GetComponent<Button>().onClick.AddListener(() => GameInputController._Instance.ToNewUnitButtonClicked());
            GameInputController._Instance._ArmyUIContent.transform.parent.parent.parent.Find("OrderUI").Find("SplitUnitButton").GetComponent<Button>().onClick.AddListener(SoundManager._Instance.PlayButtonSound);
            GameInputController._Instance._ArmyUIContent.transform.parent.parent.parent.Find("OrderUI").Find("SplitUnitButton").GetComponent<Button>().onClick.AddListener(() => GameInputController._Instance.SplitUnitButtonClicked());
            GameInputController._Instance._ArmyUIContent.transform.parent.parent.parent.Find("OrderUI").Find("GetOnTruckButton").GetComponent<Button>().onClick.AddListener(SoundManager._Instance.PlayButtonSound);
            GameInputController._Instance._ArmyUIContent.transform.parent.parent.parent.Find("OrderUI").Find("GetOnTruckButton").GetComponent<Button>().onClick.AddListener(() => GameInputController._Instance.GetOnTruckButtonClicked());
            GameInputController._Instance._ArmyUIContent.transform.parent.parent.parent.Find("OrderUI").Find("GetOffTruckButton").GetComponent<Button>().onClick.AddListener(SoundManager._Instance.PlayButtonSound);
            GameInputController._Instance._ArmyUIContent.transform.parent.parent.parent.Find("OrderUI").Find("GetOffTruckButton").GetComponent<Button>().onClick.AddListener(() => GameInputController._Instance.GetOffTruckButtonClicked());

            GameObject.FindGameObjectWithTag("UI").transform.Find("InGameScreen").Find("ConstructionScreen").Find("Remove").GetComponent<Button>().onClick.AddListener(SoundManager._Instance.PlayButtonSound);
            GameObject.FindGameObjectWithTag("UI").transform.Find("InGameScreen").Find("ConstructionScreen").Find("Remove").GetComponent<Button>().onClick.AddListener(() => TerrainController._Instance.ConstructionButtonClicked(0));
            GameObject.FindGameObjectWithTag("UI").transform.Find("InGameScreen").Find("ConstructionScreen").Find("Bridge").GetComponent<Button>().onClick.AddListener(SoundManager._Instance.PlayButtonSound);
            GameObject.FindGameObjectWithTag("UI").transform.Find("InGameScreen").Find("ConstructionScreen").Find("Bridge").GetComponent<Button>().onClick.AddListener(() => TerrainController._Instance.ConstructionButtonClicked(1));
            GameObject.FindGameObjectWithTag("UI").transform.Find("InGameScreen").Find("ConstructionScreen").Find("DirtRoad").GetComponent<Button>().onClick.AddListener(SoundManager._Instance.PlayButtonSound);
            GameObject.FindGameObjectWithTag("UI").transform.Find("InGameScreen").Find("ConstructionScreen").Find("DirtRoad").GetComponent<Button>().onClick.AddListener(() => TerrainController._Instance.ConstructionButtonClicked(2));
            GameObject.FindGameObjectWithTag("UI").transform.Find("InGameScreen").Find("ConstructionScreen").Find("AsphaltRoad").GetComponent<Button>().onClick.AddListener(SoundManager._Instance.PlayButtonSound);
            GameObject.FindGameObjectWithTag("UI").transform.Find("InGameScreen").Find("ConstructionScreen").Find("AsphaltRoad").GetComponent<Button>().onClick.AddListener(() => TerrainController._Instance.ConstructionButtonClicked(3));
            GameObject.FindGameObjectWithTag("UI").transform.Find("InGameScreen").Find("ConstructionScreen").Find("RailRoad").GetComponent<Button>().onClick.AddListener(SoundManager._Instance.PlayButtonSound);
            GameObject.FindGameObjectWithTag("UI").transform.Find("InGameScreen").Find("ConstructionScreen").Find("RailRoad").GetComponent<Button>().onClick.AddListener(() => TerrainController._Instance.ConstructionButtonClicked(4));
            GameObject.FindGameObjectWithTag("UI").transform.Find("StopScreen").Find("Continue").GetComponent<Button>().onClick.AddListener(SoundManager._Instance.PlayButtonSound);
            GameObject.FindGameObjectWithTag("UI").transform.Find("StopScreen").Find("Continue").GetComponent<Button>().onClick.AddListener(UnstopGame);
            GameObject.FindGameObjectWithTag("UI").transform.Find("StopScreen").Find("Options").GetComponent<Button>().onClick.AddListener(SoundManager._Instance.PlayButtonSound);
            GameObject.FindGameObjectWithTag("UI").transform.Find("StopScreen").Find("Options").GetComponent<Button>().onClick.AddListener(OpenOptionsScreen);
            GameObject.FindGameObjectWithTag("UI").transform.Find("StopScreen").Find("ToMenu").GetComponent<Button>().onClick.AddListener(SoundManager._Instance.PlayButtonSound);
            GameObject.FindGameObjectWithTag("UI").transform.Find("StopScreen").Find("ToMenu").GetComponent<Button>().onClick.AddListener(ToMenuScene);
            GameObject.FindGameObjectWithTag("UI").transform.Find("Options").Find("BackToMenu").GetComponent<Button>().onClick.AddListener(SoundManager._Instance.PlayButtonSound);
            GameObject.FindGameObjectWithTag("UI").transform.Find("Options").Find("BackToMenu").GetComponent<Button>().onClick.AddListener(() => CloseOptionsScreen(true));
            GameObject.FindGameObjectWithTag("UI").transform.Find("Options").Find("MusicSlider").GetComponent<Slider>().onValueChanged.AddListener(Options._Instance.MusicVolumeChanged);
            GameObject.FindGameObjectWithTag("UI").transform.Find("Options").Find("SoundSlider").GetComponent<Slider>().onValueChanged.AddListener(Options._Instance.SoundVolumeChanged);
        }
    }
    public Sprite GetUnitTypeIconFromSquad(Squad squad)
    {
        switch (squad)
        {
            case Infantry t:
                return _UnitTypeIcons[0];
            case Truck t:
                return _UnitTypeIcons[1];
            case Train t:
                return _UnitTypeIcons[2];
            case Tank t:
                return _UnitTypeIcons[3];
            case APC t:
                return _UnitTypeIcons[4];
            case Artillery t:
                return _UnitTypeIcons[5];
            case RocketArtillery t:
                return _UnitTypeIcons[6];
            case AntiTank t:
                return _UnitTypeIcons[7];
            case AntiAir t:
                return _UnitTypeIcons[8];
            case Jet t:
                return _UnitTypeIcons[9];
            case Helicopter t:
                return _UnitTypeIcons[10];
            case CargoPlane t:
                return _UnitTypeIcons[11];
            case Cruiser t:
                return _UnitTypeIcons[12];
            case Destroyer t:
                return _UnitTypeIcons[13];
            case Corvette t:
                return _UnitTypeIcons[14];
            case TransportShip t:
                return _UnitTypeIcons[15];
            default:
                return _UnitTypeIcons[0];
        }
    }
    public GameObject GetUnitModelPrefabFromSquad(Squad squad)
    {
        switch (squad)
        {
            case Infantry t:
                return _UnitModelPrefabs[0];
            case Truck t:
                return _UnitModelPrefabs[1];
            case Train t:
                return _UnitModelPrefabs[2];
            case Tank t:
                return _UnitModelPrefabs[3];
            case APC t:
                return _UnitModelPrefabs[4];
            case Artillery t:
                return _UnitModelPrefabs[5];
            case RocketArtillery t:
                return _UnitModelPrefabs[6];
            case AntiTank t:
                return _UnitModelPrefabs[7];
            case AntiAir t:
                return _UnitModelPrefabs[8];
            case Jet t:
                return _UnitModelPrefabs[9];
            case Helicopter t:
                return _UnitModelPrefabs[10];
            case CargoPlane t:
                return _UnitModelPrefabs[11];
            case Cruiser t:
                return _UnitModelPrefabs[12];
            case Destroyer t:
                return _UnitModelPrefabs[13];
            case Corvette t:
                return _UnitModelPrefabs[14];
            case TransportShip t:
                return _UnitModelPrefabs[15];
            default:
                return _UnitModelPrefabs[0];
        }
    }
    #region CommonMethods

    public T GetRandomFromList<T>(List<T> list)
    {
        return list[UnityEngine.Random.Range(0, list.Count)];
    }
    public static void ShuffleList(List<string> list)
    {
        System.Random rng = new System.Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            string value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
    /// <param name="speed">1/second</param>
    public float LinearLerpFloat(float startValue, float endValue, float speed, float startTime)
    {
        float endTime = startTime + 1 / speed;
        return Mathf.Lerp(startValue, endValue, (Time.time - startTime) / (endTime - startTime));
    }
    /// <param name="speed">1/second</param>
    public Vector2 LinearLerpVector2(Vector2 startValue, Vector2 endValue, float speed, float startTime)
    {
        float endTime = startTime + 1 / speed;
        return Vector2.Lerp(startValue, endValue, (Time.time - startTime) / (endTime - startTime));
    }
    /// <param name="speed">1/second</param>
    public Vector3 LinearLerpVector3(Vector3 startValue, Vector3 endValue, float speed, float startTime)
    {
        float endTime = startTime + 1 / speed;
        return Vector3.Lerp(startValue, endValue, (Time.time - startTime) / (endTime - startTime));
    }

    /// <param name="speed">1/second</param>
    public float LimitLerpFloat(float startValue, float endValue, float speed)
    {
        if (endValue - startValue != 0f)
            return Mathf.Lerp(startValue, endValue, Time.deltaTime * speed * 7f * (endValue - startValue));
        return endValue;
    }
    /// <param name="speed">1/second</param>
    public Vector2 LimitLerpVector2(Vector2 startValue, Vector2 endValue, float speed)
    {
        if ((endValue - startValue).magnitude != 0f)
            return Vector2.Lerp(startValue, endValue, Time.deltaTime * speed * 7f / (endValue - startValue).magnitude);
        return endValue;
    }
    /// <param name="speed">1/second</param>
    public Vector3 LimitLerpVector3(Vector3 startValue, Vector3 endValue, float speed)
    {
        if ((endValue - startValue).magnitude != 0f)
            return Vector3.Lerp(startValue, endValue, Time.deltaTime * speed * 7f / (endValue - startValue).magnitude);
        return endValue;
    }

    public bool RandomPercentageChance(float percentage)
    {
        return percentage >= UnityEngine.Random.Range(1f, 99f);
    }

    public void CoroutineCall(ref Coroutine coroutine, IEnumerator method, MonoBehaviour script)
    {
        if (coroutine != null)
            script.StopCoroutine(coroutine);
        coroutine = script.StartCoroutine(method);
    }
    public void CallForAction(System.Action action, float time, bool isRealtime)
    {
        StartCoroutine(CallForActionCoroutine(action, time, isRealtime));
    }
    private IEnumerator CallForActionCoroutine(System.Action action, float time, bool isRealtime)
    {
        if (isRealtime)
            yield return new WaitForSecondsRealtime(time);
        else
            yield return new WaitForSeconds(time);
        action?.Invoke();
    }
    public void CallForActionOneFrame(System.Action action)
    {
        StartCoroutine(CallForActionOneFrameCoroutine(action));
    }
    private IEnumerator CallForActionOneFrameCoroutine(System.Action action)
    {
        yield return null;
        action?.Invoke();
    }
    public Transform GetParent(Transform tr)
    {
        Transform parentTransform = tr.transform;
        while (parentTransform.parent != null)
        {
            parentTransform = parentTransform.parent;
        }
        return parentTransform;
    }
    public Vector3 RotateVector3OnYAxis(Vector3 baseVector, float angle)
    {
        return Quaternion.AngleAxis(angle, Vector3.up) * baseVector;

    }
    public void BufferActivated(ref bool buffer, MonoBehaviour coroutineHolderScript, ref Coroutine coroutine)
    {
        buffer = false;
        if (coroutine != null)
            coroutineHolderScript.StopCoroutine(coroutine);
    }
    #endregion

#if UNITY_EDITOR
    [MenuItem("Tools/Set Terrain Settings For All")]
    public static void SetAllTerrainSettings()
    {
        var terrains = FindObjectsByType<Terrain>(FindObjectsSortMode.None);
        foreach (var terrain in terrains)
        {
            terrain.gameObject.layer = LayerMask.NameToLayer("Terrain");
            terrain.basemapDistance = 20000;
            terrain.heightmapPixelError = 1;
            terrain.heightmapMinimumLODSimplification = 5;
            terrain.heightmapMaximumLOD = 3;
            terrain.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            terrain.terrainData.baseMapResolution = 4096;
            terrain.GetComponent<TerrainDetailOverwrite>().m_unityDetailDistance = 1100;
            terrain.GetComponent<TerrainDetailOverwrite>().m_gaiaDetailDistance = 1000;
            terrain.GetComponent<TerrainDetailOverwrite>().m_gaiaDetailDistance = 1000;
            terrain.GetComponent<TerrainDetailOverwrite>().m_unityDetailDensity = 0.01f;
            terrain.GetComponent<TerrainDetailOverwrite>().ApplySettings(true);
        }
    }
#endif


    public void ChangeMapState(MapState newMapState)
    {
        MapState oldMapState = _MapState;
        if (_MapState != null)
            _MapState.Exit(newMapState);

        _MapState = newMapState;
        _MapState.Enter(oldMapState);
    }

    public void ToMenuScene()
    {
        SceneManager.LoadScene(0);
    }
    public void ToGameScene()
    {
        SceneManager.LoadScene(1);
    }
    public void QuitGame()
    {
        Application.Quit();
    }


    public void StopGame(bool isStopScreen = true)
    {
        if (isStopScreen)
        {
            _StopScreen.SetActive(true);
            _InGameScreen.SetActive(false);
        }

        Time.timeScale = 0f;
        _IsGameStopped = true;
        SoundManager._Instance.PauseAllSound();
        SoundManager._Instance.PauseMusic();
    }
    public void UnstopGame()
    {
        if (_IsGameLoading) return;

        _StopScreen.SetActive(false);
        _InGameScreen.SetActive(true);
        CloseOptionsScreen(false);
        _IsGameStopped = false;
        Time.timeScale = 1f;
        SoundManager._Instance.ContinueAllSound();
        SoundManager._Instance.ContinueMusic();
    }

    public void OpenOptionsScreen()
    {
        if (GameManager._Instance._LevelIndex == 0)
        {
            GameObject.FindGameObjectWithTag("UI").transform.Find("Options").gameObject.SetActive(true);
            GameObject.FindGameObjectWithTag("UI").transform.Find("MainMenu").gameObject.SetActive(false);
        }
        else
        {
            GameObject.FindGameObjectWithTag("UI").transform.Find("Options").gameObject.SetActive(true);
            GameObject.FindGameObjectWithTag("UI").transform.Find("StopScreen").gameObject.SetActive(false);
        }
    }
    public void CloseOptionsScreen(bool isOpeningMenu = true)
    {
        if (GameManager._Instance._LevelIndex == 0)
        {
            GameObject.FindGameObjectWithTag("UI").transform.Find("Options").gameObject.SetActive(false);
            if (isOpeningMenu)
                GameObject.FindGameObjectWithTag("UI").transform.Find("MainMenu").gameObject.SetActive(true);
        }
        else
        {
            GameObject.FindGameObjectWithTag("UI").transform.Find("Options").gameObject.SetActive(false);
            if (isOpeningMenu)
                GameObject.FindGameObjectWithTag("UI").transform.Find("StopScreen").gameObject.SetActive(true);
        }
    }
    public void OpenOrCloseProcessingScreen(bool isOpening)
    {
        _ProcessingScreenText.enabled = isOpening;
    }

    public void OpenOrCloseConstructionScreen(bool isOpening)
    {
        if (isOpening)
            GameInputController._Instance.CloseAllInGameOtherUI();
        _ConstructionScreen.SetActive(isOpening);
    }

    public void Slowtime(float time)
    {
        CoroutineCall(ref _slowTimeCoroutine, SlowTimeCoroutine(time), this);
    }
    private IEnumerator SlowTimeCoroutine(float time)
    {
        SoundManager._Instance.SlowDownAllSound();

        float targetTimeScale = 0.2f;
        float slowInAndOutTime = 0.5f;

        float startTime = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - startTime < slowInAndOutTime)
        {
            Time.timeScale = Mathf.Lerp(Time.timeScale, targetTimeScale, (Time.realtimeSinceStartup - startTime) / slowInAndOutTime);
        }
        Time.timeScale = targetTimeScale;

        yield return new WaitForSecondsRealtime(time);

        startTime = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - startTime < slowInAndOutTime)
        {
            Time.timeScale = Mathf.Lerp(Time.timeScale, 1f, (Time.realtimeSinceStartup - startTime) / slowInAndOutTime);
        }
        Time.timeScale = 1f;

        SoundManager._Instance.UnSlowDownAllSound();
    }
}
