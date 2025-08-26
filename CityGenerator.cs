#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

[ExecuteInEditMode]
public class CityGenerator : MonoBehaviour
{
    public float _Radius = 50f;
    public List<GameObject> _BuildingPrefabs;

    [ContextMenu("Generate City")]
    public void GenerateCity()
    {
        ClearCity();

        if (_BuildingPrefabs == null || _BuildingPrefabs.Count == 0)
        {
            Debug.LogWarning("No building prefabs assigned.");
            return;
        }

        Vector3 basePos = transform.position;
        Physics.Raycast(transform.position + Vector3.up * 1500f, -Vector3.up, out RaycastHit hit, 2000f, LayerMask.GetMask("UpperTerrain"));
        if (hit.collider != null && hit.collider.CompareTag("City"))
        {
            basePos.y = hit.point.y;
        }
        int buildingCount = Mathf.RoundToInt(0.07f * Mathf.PI * _Radius * _Radius);
        for (int i = 0; i < buildingCount; i++)
        {
            Vector3 spawnPos = GetSpawnPos(basePos);


            GameObject prefab = _BuildingPrefabs[Random.Range(0, _BuildingPrefabs.Count)];
            GameObject building = (GameObject)PrefabUtility.InstantiatePrefab(prefab, transform.Find("Spawned"));
            building.transform.localScale = new Vector3(building.transform.localScale.x / transform.localScale.x, building.transform.localScale.y / transform.localScale.y, building.transform.localScale.z / transform.localScale.z) * 0.5f * Random.Range(0.85f, 1.15f);
            building.transform.position = spawnPos - Vector3.up * 1f;

            float randomYRotation = Random.Range(0f, 360f);
            building.transform.rotation = Quaternion.Euler(0, randomYRotation, 0);
            building.transform.localEulerAngles = new Vector3(0f, building.transform.localEulerAngles.y, 0f);
        }

        Dictionary<Material, List<CombineInstance>> matToCombineList = new Dictionary<Material, List<CombineInstance>>();
        Transform spawnedRoot = transform.Find("Spawned");

        for (int i = 0; i < spawnedRoot.childCount; i++)
        {
            var child = spawnedRoot.GetChild(i);
            var mf = child.GetComponentInChildren<MeshFilter>();
            var mr = child.GetComponentInChildren<MeshRenderer>();

            if (mf == null || mr == null) continue;

            Material mat = mr.sharedMaterial;

            if (!matToCombineList.ContainsKey(mat))
                matToCombineList[mat] = new List<CombineInstance>();

            CombineInstance ci = new CombineInstance();
            ci.mesh = mf.sharedMesh;
            ci.transform = transform.worldToLocalMatrix * mf.transform.localToWorldMatrix;
            matToCombineList[mat].Add(ci);
        }

        foreach (var kvp in matToCombineList)
        {
            Mesh combined = new Mesh();
            combined.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            combined.CombineMeshes(kvp.Value.ToArray(), true, true);

            GameObject combinedObj = new GameObject("Combined_" + kvp.Key.name);
            combinedObj.transform.SetParent(spawnedRoot);
            combinedObj.transform.position = transform.position;
            combinedObj.transform.localRotation = Quaternion.identity;
            combinedObj.transform.localScale = Vector3.one;

            var mf = combinedObj.AddComponent<MeshFilter>();
            mf.sharedMesh = combined;

            var mr = combinedObj.AddComponent<MeshRenderer>();
            mr.sharedMaterial = kvp.Key;

            combinedObj.isStatic = true;
        }

        ClearCity(true);
    }
    private Vector3 GetSpawnPos(Vector3 basePos)
    {
        float minDistance = 10f;
        Vector2 randomCircle = Random.insideUnitCircle * _Radius;
        Vector3 spawnPos = basePos + new Vector3(randomCircle.x, 0, randomCircle.y);
        int i = 0;
        while (i < 1000 && IsPosNearAnotherObject(spawnPos, minDistance))
        {
            randomCircle = Random.insideUnitCircle * _Radius;
            spawnPos = basePos + new Vector3(randomCircle.x, 0, randomCircle.y);
            i++;
            if (i > 800) minDistance = 2f;
            else if (i > 600) minDistance = 4f;
            else if (i > 400) minDistance = 6f;
            else if (i > 200) minDistance = 8f;
        }

        return spawnPos;
    }
    private bool IsPosNearAnotherObject(Vector3 spawnPos, float minDistance)
    {
        foreach (Transform spawnedBuilding in transform.Find("Spawned"))
        {
            if ((spawnedBuilding.position - spawnPos).magnitude < minDistance) return true;
        }
        return false;
    }

    [ContextMenu("Clear City")]
    public void ClearCity(bool isSavingCombinedMesh = false)
    {
        if (transform.Find("Spawned").childCount != 0)
        {
            GameObject[] arrayForRemove = new GameObject[transform.Find("Spawned").childCount];
            for (int i = 0; i < transform.Find("Spawned").childCount; i++)
            {
                if (!isSavingCombinedMesh || !transform.Find("Spawned").GetChild(i).name.StartsWith("Combined"))
                    arrayForRemove[i] = transform.Find("Spawned").GetChild(i).gameObject;
            }
            for (int i = 0; i < arrayForRemove.Length; i++)
            {
                DestroyImmediate(arrayForRemove[i]);
            }
        }
    }

}
#endif
