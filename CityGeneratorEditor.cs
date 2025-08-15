#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CityGenerator))]
public class CityGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CityGenerator generator = (CityGenerator)target;
        if (GUILayout.Button("Generate City"))
        {
            generator.GenerateCity();
        }
        if (GUILayout.Button("Clear City"))
        {
            generator.ClearCity();
        }
    }
}


[CustomEditor(typeof(ForestGenerator))]
public class ForestGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ForestGenerator generator = (ForestGenerator)target;
        if (GUILayout.Button("Generate Forest"))
        {
            generator.GenerateForest();
        }
        if (GUILayout.Button("Clear Forest"))
        {
            generator.ClearForest();
        }
    }
}
#endif