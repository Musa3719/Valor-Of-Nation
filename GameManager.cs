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
    public LayerMask _TerrainRayLayers;

    public static GameManager _Instance;

    public GameObject _MainCamera { get; private set; }
    public GameObject _StopScreen { get; private set; }
    public GameObject _InGameScreen { get; private set; }
    public GameObject _OptionsScreen { get; private set; }
    public TextMeshProUGUI _ProcessingScreenText { get; private set; }
    public GameObject _RoadCannotPlaceText { get; private set; }
    public GameObject _BookScreen { get; private set; }
    public GameObject _ConstructionScreen { get; private set; }
    public GameObject _LoadingScreen { get; private set; }

    public InputActionAsset _InputActions;

    public MapState _MapState;

    public bool _IsGameStopped { get; private set; }
    public bool _IsGameLoading { get; set; }
    public int _LevelIndex { get; private set; }
    public int _LastLoadedGameIndex { get; set; }

    public GraphicRaycaster _Raycaster { get; set; }
    public PointerEventData _PointerEventData { get; set; }
    public EventSystem _EventSystem { get; set; }

    private Coroutine _slowTimeCoroutine;

    private void Awake()
    {
        _Instance = this;
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
            _BookScreen = GameObject.FindGameObjectWithTag("UI").transform.Find("InGameScreen").Find("BookScreen").gameObject;
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
            new GetOnShipOrder().ExecuteOrder(GameInputController._Instance._SelectedObjects[0]);
        }
        if (Input.GetKeyDown(KeyCode.Y))
        {
            new EvacuateShipOrder().ExecuteOrder(GameInputController._Instance._SelectedObjects[0]);
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

            if (_InputActions.FindAction("Book").triggered && _LevelIndex != 0 && !_IsGameStopped)
            {
                OpenOrCloseBookScreen(!_BookScreen.activeInHierarchy);
            }
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
                    if (_ConstructionScreen.activeInHierarchy)
                        OpenOrCloseConstructionScreen(false);
                    else if (_BookScreen.activeInHierarchy)
                        OpenOrCloseBookScreen(false);
                    else if (GameInputController._Instance._SelectedObjects.Count > 0)
                        GameInputController._Instance._SelectedObjects.ClearSelected();
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
    public void OpenOrCloseBookScreen(bool isOpening)
    {
        _BookScreen.SetActive(isOpening);
    }
    public void OpenOrCloseConstructionScreen(bool isOpening)
    {
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
