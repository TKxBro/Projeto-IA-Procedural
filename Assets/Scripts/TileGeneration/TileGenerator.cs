using UnityEngine;

public class TileGenerator : MonoBehaviour
{
    [Header("Noise Settings")]
    public int seed = 0;
    [Range(0.001f, 1f)] public float noiseScale = 0.12f;

    [Tooltip("Offset extra no noise (pra deslocar o mapa).")]
    public Vector2 noiseOffset = Vector2.zero;

    [Header("Fractal Noise (opcional)")]
    [Range(1, 6)] public int octaves = 1;
    [Range(0f, 1f)] public float persistence = 0.5f;
    [Range(1f, 4f)] public float lacunarity = 2f;

    [Header("Cost Mapping")]
    [Tooltip("Maior custo poss�vel (se seu costMap vai 0,1,2 ent�o � 2).")]
    public int maxTerrainCost = 3;



    void ClearGrid()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }

    Vector2 DetermineTileSize(Bounds tileBounds)
    {
        return new Vector2((tileBounds.extents.x * 2) * 0.75f, (tileBounds.extents.z * 2));
    }

    public void GenerateGrid(GameObject tile, Vector2Int gridsize)
    {
        ClearGrid();

        seed = Random.Range(0, 5000);

        Vector2 tileSize = DetermineTileSize(tile.GetComponent<MeshFilter>().sharedMesh.bounds);
        Vector3 position = transform.position;

        // offsets derivados do seed (determin�stico)
        float seedX = seed * 1000.123f;
        float seedY = seed * 2000.456f;

        for (int x = 0; x < gridsize.x; x++)
        {
            for (int y = 0; y < gridsize.y; y++)
            {
                position.x = transform.position.x + tileSize.x * x;
                position.z = transform.position.z + tileSize.y * y;
                position.z += UnevenRowOffset(x, tileSize.y);

                float noiseValue = FractalPerlin(x, y, seedX, seedY);
                int cost = NoiseToCost(noiseValue);

                CreateTile(tile, position, new Vector2Int(x, y), cost);
            }
        }
    }

    float UnevenRowOffset(float x, float y)
    {
        return x % 2 == 0 ? y / 2 : 0f;
    }

    float FractalPerlin(int x, int y, float seedX, float seedY)
    {
        float amplitude = 1f;
        float frequency = 1f;
        float noiseHeight = 0f;
        float normalization = 0f;

        for (int i = 0; i < octaves; i++)
        {
            float sampleX = (x + noiseOffset.x + seedX) * noiseScale * frequency;
            float sampleY = (y + noiseOffset.y + seedY) * noiseScale * frequency;

            float perlin = Mathf.PerlinNoise(sampleX, sampleY); // 0..1
            noiseHeight += perlin * amplitude;

            normalization += amplitude;
            amplitude *= persistence;
            frequency *= lacunarity;
        }

        // normaliza pra 0..1
        if (normalization > 0f)
            noiseHeight /= normalization;

        return Mathf.Clamp01(noiseHeight);
    }

    int NoiseToCost(float noise01)
    {
        // Converte 0..1 em 0..maxTerrainCost
        int cost = Mathf.FloorToInt(noise01 * (maxTerrainCost + 1));
        return Mathf.Clamp(cost, 0, maxTerrainCost);
    }

    void CreateTile(GameObject t, Vector3 pos, Vector2Int id, int initialCost)
    {
        GameObject newTile = Instantiate(t.gameObject, pos, Quaternion.identity, transform);
        newTile.name = "Tile " + id;

        Tile tileComp = newTile.GetComponent<Tile>();
        if (tileComp != null)
            tileComp.Init(initialCost);

        Debug.Log($"Created tile {id} with cost {initialCost}");
    }
}
