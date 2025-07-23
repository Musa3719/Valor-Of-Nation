using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
public enum Language
{
    EN,
    TR
}
public class Localization : MonoBehaviour
{
    public static Localization _Instance;

    public Language _ActiveLanguage { get; private set; }

    public List<string> _UI;
    public List<string> _Tutorial;

    public static event Action _LanguageChangedEvent;

    private void Awake()
    {
        _Instance = this;

        _ActiveLanguage = (Language)PlayerPrefs.GetInt("Language", 0);
        _UI = new List<string>();
        _Tutorial = new List<string>();
        StartCoroutine(SceneLoadedCoroutine());

    }
    IEnumerator SceneLoadedCoroutine()
    {
        yield return new WaitForSecondsRealtime(0.05f);
        SetLanguage(_ActiveLanguage);
    }
    public void SetLanguage(Language language)
    {
        _ActiveLanguage = language;
        PlayerPrefs.SetInt("Language", (int)language);
        LocalizeTexts();
    }
    public void SetLanguage(int number)
    {
        _ActiveLanguage = (Language)number;
        PlayerPrefs.SetInt("Language", number);
        LocalizeTexts();
    }
    public void NextLanguage()
    {
        int number = (((int)_ActiveLanguage) + 1);
        number = number >= Enum.GetNames(typeof(Language)).Length ? 0 : number;
        SetLanguage(number);
    }
    public void PreviousLanguage()
    {
        int number = (((int)_ActiveLanguage) - 1);
        number = number < 0 ? Enum.GetNames(typeof(Language)).Length - 1 : number;
        SetLanguage(number);
    }

    private void LocalizeTexts()
    {
        ArrangeList(_Tutorial, "Tutorial.txt");
        ArrangeUI();

        _LanguageChangedEvent?.Invoke();
    }
    private void ArrangeUI()
    {
        string fileName = "";
        if (SceneManager.GetActiveScene().buildIndex == 0)
            fileName = "MenuUI.txt";
        else
            fileName = "GameUI.txt";
        ArrangeList(_UI, fileName);
    }
    private void ArrangeList(List<string> list, string fileName)
    {
        list.Clear();
        string languageFilePath = "/" + (_ActiveLanguage).ToString() + "/";
        string path = Application.streamingAssetsPath + languageFilePath + fileName;

        StreamReader reader = new StreamReader(path);
        string line = "";
        while ((line = reader.ReadLine()) != null)
        {
            list.Add(line);
        }
        reader.Close();
    }

}
