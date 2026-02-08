using UnityEngine;

public static class Noise
{
    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, float scale)
    {
        float[,] noiseMap = new float[mapWidth, mapHeight];
        if(scale <= 0)
        {
            scale = 0.0001f;
        }


        for(int y = 0; y < mapHeight; y++)
        {
            for(int x = 0; x < mapWidth; x++)
            {
                float noiseX = x / scale;
                float noiseY = y / scale;
                var noiseValue = Mathf.PerlinNoise(noiseX, noiseY);

                noiseMap[x, y] = noiseValue;
            }
        }

        return noiseMap;
    }
}
