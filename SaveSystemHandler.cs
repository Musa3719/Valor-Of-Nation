using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Collections;
using UnityEngine.SceneManagement;

public class SaveSystemHandler : MonoBehaviour
{
    public static SaveSystemHandler _Instance;
    private void Awake()
    {
        if (_Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        _Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    private string SavePath(int index) => Path.Combine(Application.persistentDataPath, "Save" + index.ToString() + ".json");

    public void SaveGame(int index)
    {

        GameData data = new GameData
        {
            _BridgePositions = new List<Vector3>(),
            _BridgeRotations = new List<Vector3>(),
            _DirtRoadPositions = new List<Vector3>(),
            _DirtRoadRotations = new List<Vector3>(),
            _AsphaltRoadPositions = new List<Vector3>(),
            _AsphaltRoadRotations = new List<Vector3>(),
            _RailRoadPositions = new List<Vector3>(),
            _RailRoadRotations = new List<Vector3>()
        };

        //add game data here
        foreach (var bridge in GameObject.FindGameObjectsWithTag("Bridge"))
        {
            data._BridgePositions.Add(bridge.transform.position);
            data._BridgeRotations.Add(bridge.transform.localEulerAngles);
        }
        foreach (var dirtRoad in GameObject.FindGameObjectsWithTag("DirtRoad"))
        {
            data._DirtRoadPositions.Add(dirtRoad.transform.position);
            data._DirtRoadRotations.Add(dirtRoad.transform.localEulerAngles);
        }
        foreach (var asphaltRoad in GameObject.FindGameObjectsWithTag("AsphaltRoad"))
        {
            data._AsphaltRoadPositions.Add(asphaltRoad.transform.position);
            data._AsphaltRoadRotations.Add(asphaltRoad.transform.localEulerAngles);
        }
        foreach (var railRoad in GameObject.FindGameObjectsWithTag("RailRoad"))
        {
            data._RailRoadPositions.Add(railRoad.transform.position);
            data._RailRoadRotations.Add(railRoad.transform.localEulerAngles);
        }

        SaveGameData(index, data);
    }

    public void LoadGame(int index)
    {
        GameManager._Instance.StopGame();
        GameManager._Instance._LastLoadedGameIndex = index;
        GameData data = LoadGameData(index);

        //SceneManager.LoadScene(1);

        if (data == null) return;

        //delete runtime created objects
        foreach (var bridge in GameObject.FindGameObjectsWithTag("Bridge"))
        {
            Destroy(bridge);
        }
        foreach (var dirtRoad in GameObject.FindGameObjectsWithTag("DirtRoad"))
        {
            Destroy(dirtRoad);

        }
        foreach (var asphaltRoad in GameObject.FindGameObjectsWithTag("AsphaltRoad"))
        {
            Destroy(asphaltRoad);

        }
        foreach (var railRoad in GameObject.FindGameObjectsWithTag("RailRoad"))
        {
            Destroy(railRoad);

        }

        //load game data
        for (int i = 0; i < data._BridgePositions.Count; i++)
        {
            Instantiate(TerrainController._Instance._BridgePrefab, data._BridgePositions[i], Quaternion.identity).transform.localEulerAngles = data._BridgeRotations[i];
        }
        for (int i = 0; i < data._DirtRoadPositions.Count; i++)
        {
            Instantiate(TerrainController._Instance._DirtRoadPrefab, data._DirtRoadPositions[i], Quaternion.identity).transform.localEulerAngles = data._DirtRoadRotations[i];
        }
        for (int i = 0; i < data._AsphaltRoadPositions.Count; i++)
        {
            Instantiate(TerrainController._Instance._AsphaltRoadPrefab, data._AsphaltRoadPositions[i], Quaternion.identity).transform.localEulerAngles = data._AsphaltRoadRotations[i];
        }
        for (int i = 0; i < data._RailRoadPositions.Count; i++)
        {
            Instantiate(TerrainController._Instance._RailRoadPrefab, data._RailRoadPositions[i], Quaternion.identity).transform.localEulerAngles = data._RailRoadRotations[i];
        }

        GameManager._Instance.UnstopGame();
    }

    private void SaveGameData(int index, GameData data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath(index), json);
        Debug.Log("Game saved to " + SavePath(index));
    }
    public GameData LoadGameData(int index)
    {
        if (!File.Exists(SavePath(index)))
        {
            Debug.LogWarning("Save file not found.");
            return null;
        }

        string json = File.ReadAllText(SavePath(index));
        return JsonUtility.FromJson<GameData>(json);
    }
}

[System.Serializable]
public class GameData
{
    // game and level state

    public List<Vector3> _BridgePositions;
    public List<Vector3> _BridgeRotations;

    public List<Vector3> _DirtRoadPositions;
    public List<Vector3> _DirtRoadRotations;

    public List<Vector3> _AsphaltRoadPositions;
    public List<Vector3> _AsphaltRoadRotations;

    public List<Vector3> _RailRoadPositions;
    public List<Vector3> _RailRoadRotations;
}

