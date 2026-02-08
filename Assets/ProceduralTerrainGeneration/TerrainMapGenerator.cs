using UnityEngine;

public class TerrainMapGenerator : MonoBehaviour
{
    public int width;
    public int height;
    public float scale;

    public bool AutoUpdate;

    public void GenerateMap()
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(width, height, scale);

        MapDisplay mapDisplay = GetComponent<MapDisplay>(); // Tutorial used FindobjectOfType<MapDisplay>();
        mapDisplay.DrawNoiseMap(noiseMap);
    }

}
