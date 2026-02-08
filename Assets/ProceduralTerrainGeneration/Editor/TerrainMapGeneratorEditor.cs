using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TerrainMapGenerator))]
public class TerrainMapGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        TerrainMapGenerator mapGenerator = (TerrainMapGenerator)target;

        if(DrawDefaultInspector() && mapGenerator.AutoUpdate)
        {
            mapGenerator.GenerateMap();
        }

        if (GUILayout.Button("Generate"))
        {
            mapGenerator.GenerateMap();
        }
    }
}
