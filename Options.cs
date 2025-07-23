using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Options : MonoBehaviour
{
    public static Options _Instance;
    public float _SoundVolume { get; private set; }
    public float _MusicVolume { get; private set; }

    private Slider _soundSlider;
    private Slider _musicSlider;


    private void Awake()
    {
        _Instance = this;

        Debug.unityLogger.logEnabled = Debug.isDebugBuild;
        _SoundVolume = PlayerPrefs.GetFloat("Sound", 0.33f);
        _MusicVolume = PlayerPrefs.GetFloat("Music", 0.33f);

        _soundSlider = GameObject.FindGameObjectWithTag("UI").transform.Find("Options").Find("SoundSlider").GetComponent<Slider>();
        _musicSlider = GameObject.FindGameObjectWithTag("UI").transform.Find("Options").Find("MusicSlider").GetComponent<Slider>();

        _soundSlider.value = _SoundVolume;
        _musicSlider.value = _MusicVolume;
    }
    private void Start()
    {
        ArrangeGraphics();
    }
    
    public void SoundVolumeChanged(float newValue)
    {
        _SoundVolume = newValue;
        PlayerPrefs.SetFloat("Sound", newValue);
        ArrangeActiveSoundVolumes(newValue);
    }
    public void MusicVolumeChanged(float newValue)
    {
        _MusicVolume = newValue;
        PlayerPrefs.SetFloat("Music", newValue);
        ArrangeActiveMusicVolumes(newValue);
        if (SoundManager._Instance != null)
            SoundManager._Instance.ContinueMusic();
    }

    public void SetGraphicSetting(int number)
    {
        QualitySettings.SetQualityLevel(number);
    }
    public void ArrangeGraphics()
    {
        int quality = QualitySettings.GetQualityLevel();

    }

    private void ArrangeActiveSoundVolumes(float newValue)
    {
        if (SoundManager._Instance != null && SoundManager._Instance._SoundObjectsParent != null)
        {
            foreach (Transform sound in SoundManager._Instance._SoundObjectsParent.transform)
            {
                if (newValue != 0f)
                    sound.GetComponent<AudioSource>().volume = newValue * sound.transform.localEulerAngles.x;
            }
        }

        if (SoundManager._Instance != null && SoundManager._Instance._CurrentAtmosphereObject != null)
        {
            if (newValue != 0f)
                SoundManager._Instance._CurrentAtmosphereObject.GetComponent<AudioSource>().volume = newValue * SoundManager._Instance._CurrentAtmosphereObject.transform.localEulerAngles.x;
            else
                SoundManager._Instance._CurrentAtmosphereObject.GetComponent<AudioSource>().volume = 0f;
        }
    }
    private void ArrangeActiveMusicVolumes(float newValue)
    {
        if (SoundManager._Instance != null && SoundManager._Instance._CurrentMusicObject != null)
        {
            if (newValue != 0f)
                SoundManager._Instance._CurrentMusicObject.GetComponent<AudioSource>().volume = newValue * SoundManager._Instance._CurrentMusicObject.transform.localEulerAngles.x;
            else
                SoundManager._Instance._CurrentMusicObject.GetComponent<AudioSource>().volume = 0f;
        }
    }
}