using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager _Instance;

    [SerializeField]
    private GameObject _audioSourcePrefab;
    [SerializeField]
    public GameObject _SoundObjectsParent;

    public GameObject _CurrentMusicObject { get; private set; }
    public GameObject _CurrentAtmosphereObject { get; private set; }


    #region Sounds

    public AudioClip _Button;


    public List<AudioClip> _StonePlaneSounds;
    public List<AudioClip> _DirtPlaneSounds;
    public List<AudioClip> _GrassPlaneSounds;
    public List<AudioClip> _MetalPlaneSounds;
    public List<AudioClip> _ConcretePlaneSounds;
    public List<AudioClip> _WoodPlaneSounds;
    public List<AudioClip> _FabricPlaneSounds;
    public List<AudioClip> _WaterPlaneSounds;
    public List<AudioClip> _IcePlaneSounds;
    public List<AudioClip> _SnowyPlaneSounds;
    #endregion

    [SerializeField]
    private List<AudioClip> _atmosphereSounds;

    #region Musics

    [SerializeField]
    private AudioClip _menuMusic;


    #endregion


    private Coroutine _stopCurrentMusicCoroutine;
    private Coroutine _stopCurrentAtmosphereCoroutine;

    private void Awake()
    {
        _Instance = this;
        _SoundObjectsParent = transform.Find("SoundObjectsParent").gameObject;
        //_CurrentMusicObject = transform.Find("MusicObject").gameObject;
        //_CurrentAtmosphereObject = transform.Find("AtmosphereObject").gameObject;
    }
    private void Start()
    {
        int sceneNumber = GameManager._Instance._LevelIndex;
        if (sceneNumber == 0)
        {
            PlayMusic(_menuMusic);
        }
        else
        {
            //PlayMusic(GetRandomSoundFromList(InGameMusics));
        }

    }
    private void LateUpdate()
    {
        if (SoundManager._Instance._CurrentMusicObject != null && GameManager._Instance != null)
        {
            _CurrentMusicObject.transform.position = GameManager._Instance._MainCamera.transform.position;
        }
        if (SoundManager._Instance._CurrentAtmosphereObject != null && GameManager._Instance != null)
        {
            _CurrentAtmosphereObject.transform.position = GameManager._Instance._MainCamera.transform.position;
        }
    }

    public AudioClip GetRandomSoundFromList(List<AudioClip> list)
    {
        if (list == null || list.Count == 0) return null;

        return list[UnityEngine.Random.Range(0, list.Count)];
    }


    /// <summary>
    /// BossNumber Starts From 1
    /// </summary>
    /// <param name="bossNumber"></param>
    public void PlayMusic(AudioClip clip, AudioClip atmosphere = null)
    {
        if (atmosphere != null)
        {
            if (_CurrentAtmosphereObject == null)
            {
                PlayAtmosphereArrangement(atmosphere);
            }
            else
            {
                GameManager._Instance.CoroutineCall(ref _stopCurrentAtmosphereCoroutine, StopCurrentAtmosphereAndPlay(atmosphere), this);
            }
        }

        if (_CurrentMusicObject != null)
        {
            GameManager._Instance.CoroutineCall(ref _stopCurrentMusicCoroutine, StopCurrentMusicAndPlay(clip), this);
        }
        else
        {
            PlayMusicArrangement(clip);
        }
    }
    private IEnumerator StopCurrentAtmosphereAndPlay(AudioClip clip)
    {
        AudioSource source = _CurrentAtmosphereObject.GetComponent<AudioSource>();

        if (clip.name == source.clip.name) yield break;

        while (source.volume > 0.05f)
        {
            source.volume -= Time.deltaTime * 3f;
            yield return null;
        }
        Destroy(_CurrentAtmosphereObject);

        PlayAtmosphereArrangement(clip);
    }
    /// <summary>
    /// Stops current music AND plays the next.
    /// </summary>
    private IEnumerator StopCurrentMusicAndPlay(AudioClip clip)
    {
        AudioSource source = _CurrentMusicObject.GetComponent<AudioSource>();

        if (clip.name == source.clip.name) yield break;

        while (source.volume > 0.05f)
        {
            source.volume -= Time.deltaTime * 3f;
            yield return null;
        }
        Destroy(_CurrentMusicObject);

        PlayMusicArrangement(clip);
    }
    private void PlayMusicArrangement(AudioClip clip)
    {
        float volume = 0.25f;
        _CurrentMusicObject = PlaySound(clip, Vector3.zero, volume, true, 1f, true, false);
        if (_CurrentMusicObject != null)
        {
            _CurrentMusicObject.name = "MusicObject";
            DontDestroyOnLoad(_CurrentMusicObject);
        }
    }

    private void PlayAtmosphereArrangement(AudioClip clip)
    {
        _CurrentAtmosphereObject = PlaySound(clip, Vector3.zero, 0.045f, true, 1f, false, true);
        if (_CurrentAtmosphereObject != null)
        {
            _CurrentAtmosphereObject.name = "AtmosphereObject";
            DontDestroyOnLoad(_CurrentAtmosphereObject);
        }
    }

    /// <summary>
    /// Play Sound by creating a AudioSourcePrefab Object.
    /// </summary>
    public GameObject PlaySound(AudioClip clip, Vector3 position, float volume = 1f, bool isLooping = false, float pitch = 1f, bool isMusic = false, bool isAtmosphere = false)
    {
        if (clip == null) return null;

        AudioSource audioSource = Instantiate(_audioSourcePrefab, position, Quaternion.identity).GetComponent<AudioSource>();
        audioSource.clip = clip;
        if (isMusic)
            audioSource.volume = Options._Instance._MusicVolume * volume;
        else
            audioSource.volume = Options._Instance._SoundVolume * volume;
        audioSource.transform.localEulerAngles = new Vector3(volume, 0f, 0f);
        audioSource.loop = isLooping;
        audioSource.pitch = pitch;
        audioSource.Play();
        if (!isMusic && !isAtmosphere)
            audioSource.gameObject.transform.SetParent(_SoundObjectsParent.transform);
        if (isMusic || isAtmosphere)
            audioSource.spatialBlend = 0f;

        if (!isLooping)
            Destroy(audioSource.gameObject, audioSource.clip.length / Mathf.Clamp(pitch, 0.1f, 1f) + 1f);
        return audioSource.gameObject;
    }

    /*public void PlayPlaneSound(PlaneSoundType type, Vector3 pos, float speed, float pitchMultiplier = 1f, float volumeMultiplier = 1f)
    {
        float pitchFromSpeed = speed / 45f + 0.8f;
        pitchFromSpeed *= pitchMultiplier;
        AudioClip clip = null;
        switch (type)
        {
            case PlaneSoundType.Stone:
                clip = GetRandomSoundFromList(_StonePlaneSounds);
                break;
            case PlaneSoundType.Dirt:
                clip = GetRandomSoundFromList(_DirtPlaneSounds);
                break;
            case PlaneSoundType.Grass:
                clip = GetRandomSoundFromList(_GrassPlaneSounds);
                break;
            case PlaneSoundType.Metal:
                clip = GetRandomSoundFromList(_MetalPlaneSounds);
                break;
            case PlaneSoundType.Concrete:
                clip = GetRandomSoundFromList(_ConcretePlaneSounds);
                break;
            case PlaneSoundType.Wood:
                clip = GetRandomSoundFromList(_WoodPlaneSounds);
                break;
            case PlaneSoundType.Fabric:
                clip = GetRandomSoundFromList(_FabricPlaneSounds);
                break;
            case PlaneSoundType.Water:
                clip = GetRandomSoundFromList(_WaterPlaneSounds);
                break;
            case PlaneSoundType.Ice:
                clip = GetRandomSoundFromList(_IcePlaneSounds);
                break;
            case PlaneSoundType.Snowy:
                clip = GetRandomSoundFromList(_SnowyPlaneSounds);
                break;
            default:
                break;
        }
        if (clip != null)
            PlaySound(clip, pos, volumeMultiplier, false, pitchFromSpeed + UnityEngine.Random.Range(-0.05f, 0.05f));
    }*/

    public void PlayButtonSound()
    {
        SoundManager._Instance.PlaySound(SoundManager._Instance._Button, Camera.main.transform.position + Vector3.forward, 0.15f, false, UnityEngine.Random.Range(0.9f, 1.1f));
    }

    public void PauseAllSound()
    {
        foreach (Transform sound in _SoundObjectsParent.transform)
        {
            sound.GetComponent<AudioSource>().Pause();
        }
    }
    public void ContinueAllSound()
    {
        foreach (Transform sound in _SoundObjectsParent.transform)
        {
            sound.GetComponent<AudioSource>().UnPause();
        }
    }

    public void PauseMusic()
    {
        if (_CurrentMusicObject != null)
            _CurrentMusicObject.GetComponent<AudioSource>().Pause();
        if (_CurrentAtmosphereObject != null)
            _CurrentAtmosphereObject.GetComponent<AudioSource>().Pause();
    }
    public void ContinueMusic()
    {
        if (_CurrentMusicObject != null)
            _CurrentMusicObject.GetComponent<AudioSource>().UnPause();
        if (_CurrentAtmosphereObject != null)
            _CurrentAtmosphereObject.GetComponent<AudioSource>().UnPause();
    }


    public void SlowDownAllSound()
    {
        foreach (Transform sound in _SoundObjectsParent.transform)
        {
            sound.GetComponent<AudioSource>().pitch = 0.85f;
        }
    }
    public void UnSlowDownAllSound()
    {
        foreach (Transform sound in _SoundObjectsParent.transform)
        {
            sound.GetComponent<AudioSource>().pitch = 1f;
        }
    }
}