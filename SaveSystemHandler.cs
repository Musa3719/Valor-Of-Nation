using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.Splines;

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
            _LastCreatedUnitIndex = GameManager._Instance._LastCreatedUnitIndex,
            _BridgePositions = new List<Vector3>(),
            _BridgeRotations = new List<Vector3>(),
            _BridgeAttachedRiverNames = new List<string>(),
            _DirtRoads = new List<RoadSplineData>(),
            _AsphaltRoads = new List<RoadSplineData>(),
            _RailRoads = new List<RoadSplineData>(),
            _Cities = new List<CityData>(),
            _LandUnits = new List<UnitData>(),
            _NavalUnits = new List<UnitData>(),
            _CarryingLandUnits = new List<UnitData>(),
            _CarryingNavalUnits = new List<UnitData>(),
        };

        //add game data here
        foreach (var bridge in GameObject.FindGameObjectsWithTag("Bridge"))
        {
            data._BridgePositions.Add(bridge.transform.position);
            data._BridgeRotations.Add(bridge.transform.localEulerAngles);
            data._BridgeAttachedRiverNames.Add(bridge.GetComponent<BridgeUnitController>()._AttachedRiver.name);
        }
        foreach (var dirtRoad in GameObject.FindGameObjectsWithTag("DirtRoad"))
        {
            RoadSplineData newData = RoadBuilder._Instance.GetRoadDataForSaving(dirtRoad);
            data._DirtRoads.Add(newData);
        }
        foreach (var asphaltRoad in GameObject.FindGameObjectsWithTag("AsphaltRoad"))
        {
            RoadSplineData newData = RoadBuilder._Instance.GetRoadDataForSaving(asphaltRoad);
            data._DirtRoads.Add(newData);
        }
        foreach (var railRoad in GameObject.FindGameObjectsWithTag("RailRoad"))
        {
            RoadSplineData newData = RoadBuilder._Instance.GetRoadDataForSaving(railRoad);
            data._DirtRoads.Add(newData);
        }


        foreach (var city in GameObject.FindGameObjectsWithTag("City"))
        {
            City cityComponent = city.GetComponent<City>();
            data._Cities.Add(cityComponent.ToData());
        }

        foreach (var landUnit in GameObject.FindGameObjectsWithTag("LandUnit"))
        {
            Unit unitComponent = landUnit.GetComponent<Unit>();
            data._LandUnits.Add(unitComponent.ToData());

            if (landUnit.transform.Find("CarryingUnits").childCount > 0)
            {
                foreach (Transform carryingUnit in landUnit.transform.Find("CarryingUnits"))
                {
                    if (carryingUnit.GetComponent<Unit>()._IsNaval)
                        data._CarryingNavalUnits.Add(carryingUnit.GetComponent<Unit>().ToData());
                    else
                        data._CarryingLandUnits.Add(carryingUnit.GetComponent<Unit>().ToData());
                }
            }
        }
        foreach (var navalUnit in GameObject.FindGameObjectsWithTag("NavalUnit"))
        {
            Unit unitComponent = navalUnit.GetComponent<Unit>();
            data._NavalUnits.Add(unitComponent.ToData());

            if (navalUnit.transform.Find("CarryingUnits").childCount > 0)
            {
                foreach (Transform carryingUnit in navalUnit.transform.Find("CarryingUnits"))
                {
                    if (carryingUnit.GetComponent<Unit>()._IsNaval)
                        data._CarryingNavalUnits.Add(carryingUnit.GetComponent<Unit>().ToData());
                    else
                        data._CarryingLandUnits.Add(carryingUnit.GetComponent<Unit>().ToData());
                }
            }
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

        GameManager._Instance._LastCreatedUnitIndex = data._LastCreatedUnitIndex;

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
            GameObject newBridge = Instantiate(TerrainController._Instance._BridgePrefab, data._BridgePositions[i], Quaternion.identity);
            newBridge.transform.localEulerAngles = data._BridgeRotations[i];
            newBridge.transform.SetParent(GameObject.Find("Bridges").transform);
            newBridge.GetComponent<BridgeUnitController>()._AttachedRiver= TerrainController._Instance.NameToRiverController(data._BridgeAttachedRiverNames[i]);
        }

        GameObject container = GameObject.Find("RoadSystemDirt");
        for (int i = 0; i < data._DirtRoads.Count; i++)
        {
            GameObject newRoad = RoadBuilder._Instance.InstantiateSplineObject(container.transform, false);
            RoadBuilder._Instance.SetRoadDataForLoading(newRoad, data._DirtRoads[i]);
            RoadBuilder._Instance.ReArrangeSpline(newRoad.GetComponent<SplineContainer>());
        }
        container = GameObject.Find("RoadSystemAsphalt");
        for (int i = 0; i < data._AsphaltRoads.Count; i++)
        {
            GameObject newRoad = RoadBuilder._Instance.InstantiateSplineObject(container.transform, false);
            RoadBuilder._Instance.SetRoadDataForLoading(newRoad, data._AsphaltRoads[i]);
            RoadBuilder._Instance.ReArrangeSpline(newRoad.GetComponent<SplineContainer>());
        }
        container = GameObject.Find("RoadSystemRail");
        for (int i = 0; i < data._RailRoads.Count; i++)
        {
            GameObject newRoad = RoadBuilder._Instance.InstantiateSplineObject(container.transform, false);
            RoadBuilder._Instance.SetRoadDataForLoading(newRoad, data._RailRoads[i]);
            RoadBuilder._Instance.ReArrangeSpline(newRoad.GetComponent<SplineContainer>());
        }

        foreach (var city in GameObject.FindGameObjectsWithTag("City"))
        {
            foreach (var cty in data._Cities)
            {
                if (cty._Name == city.GetComponent<City>()._Name)
                {
                    city.GetComponent<City>().LoadFromData(cty);
                    break;
                }
            }
        }

        foreach (var landUnit in GameObject.FindGameObjectsWithTag("LandUnit"))
        {
            landUnit.tag = "Untagged";
            Destroy(landUnit);
        }
        foreach (var navalUnit in GameObject.FindGameObjectsWithTag("NavalUnit"))
        {
            navalUnit.tag = "Untagged";
            Destroy(navalUnit);
        }

        GameManager._Instance._FriendlyUnitColliders.Clear();

        for (int i = 0; i < data._LandUnits.Count; i++)
        {
            Instantiate(GameManager._Instance._LandUnitPrefab);
        }
        for (int i = 0; i < data._NavalUnits.Count; i++)
        {
            Instantiate(GameManager._Instance._NavalUnitPrefab);
        }

        var landUnits = GameObject.FindGameObjectsWithTag("LandUnit");
        var navalUnits = GameObject.FindGameObjectsWithTag("NavalUnit");


        for (int i = 0; i < landUnits.Length; i++)
        {
            landUnits[i].GetComponent<Unit>().LoadFromData(data._LandUnits[i]);
        }
        for (int i = 0; i < navalUnits.Length; i++)
        {
            navalUnits[i].GetComponent<Unit>().LoadFromData(data._NavalUnits[i]);
        }


        GameObject[] carryingLandUnitsArray = new GameObject[data._CarryingLandUnits.Count];
        for (int i = 0; i < data._CarryingLandUnits.Count; i++)
        {
            carryingLandUnitsArray[i] = Instantiate(GameManager._Instance._LandUnitPrefab);
        }
        GameObject[] carryingNavalUnitsArray = new GameObject[data._CarryingNavalUnits.Count];
        for (int i = 0; i < data._CarryingNavalUnits.Count; i++)
        {
            carryingNavalUnitsArray[i] = Instantiate(GameManager._Instance._NavalUnitPrefab);
        }

        for (int i = 0; i < carryingLandUnitsArray.Length; i++)
        {
            carryingLandUnitsArray[i].GetComponent<Unit>().LoadFromData(data._CarryingLandUnits[i]);
        }
        for (int i = 0; i < carryingNavalUnitsArray.Length; i++)
        {
            carryingNavalUnitsArray[i].GetComponent<Unit>().LoadFromData(data._CarryingNavalUnits[i]);
        }
        for (int i = 0; i < carryingLandUnitsArray.Length; i++)
        {
            carryingLandUnitsArray[i].GetComponent<Unit>().LoadAttachedUnitsFromData(data._CarryingLandUnits[i]);
        }
        for (int i = 0; i < carryingNavalUnitsArray.Length; i++)
        {
            carryingNavalUnitsArray[i].GetComponent<Unit>().LoadAttachedUnitsFromData(data._CarryingNavalUnits[i]);
        }

        GameInputController._Instance._SelectedUnits.ClearSelected();
        GameInputController._Instance.CloseAllInGameOtherUI();

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
    public int _LastCreatedUnitIndex;

    public List<Vector3> _BridgePositions;
    public List<Vector3> _BridgeRotations;
    public List<string> _BridgeAttachedRiverNames;

    public List<RoadSplineData> _DirtRoads;
    public List<RoadSplineData> _AsphaltRoads;
    public List<RoadSplineData> _RailRoads;

    public List<CityData> _Cities;
    
    public List<UnitData> _LandUnits;
    public List<UnitData> _NavalUnits;
    public List<UnitData> _CarryingLandUnits;
    public List<UnitData> _CarryingNavalUnits;
}

