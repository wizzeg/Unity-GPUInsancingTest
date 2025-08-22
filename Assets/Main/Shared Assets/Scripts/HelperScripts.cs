using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

public class HelperScripts
{
    public struct Tile
    {
        public int index;
        public bool visible;
        public float extents;
        public Bounds bounds;
        public Vector3 center;
        public Vector3[] positions;
    }

    [BurstCompile]
    public static Vector3[] GenerateGrassPositions(float size, float density, Vector3 center)
    {
        float Extent = size / 2;


        int RowCount = (int)(size * density);
        int ColumnCount = (int)(size * density);

        float StepSize = 1 / density;

        Vector3[] Vectors = new Vector3[RowCount * ColumnCount];

        for (int i = 0; i < RowCount; i++)
        {
            for (int j = 0; j < ColumnCount; j++)
            {
                Vectors[(j * RowCount) + i].x = (i * StepSize) + (center.x - Extent);
                Vectors[(j * RowCount) + i].y = 0;
                Vectors[(j * RowCount) + i].z = (j * StepSize) + (center.z - Extent);
            }
        }
        return Vectors;
    }

    [BurstCompile] // wanted to use Native array for burst compile, but there are several drawbacks with nativearray.
    public static NativeArray<Tile> GenerateGrassTiles(float size, float density, Vector3 center, float tileSize)
    {
        //1. figure out how many tiles fit in size, generate new size
        //2. figure out tiles centers
        //3. populate tiles as before
        int fittingTiles = (int)(size / tileSize);
        int totalNumTiles = fittingTiles * fittingTiles;
        size = fittingTiles * tileSize;

        NativeArray<Tile> result = new NativeArray<Tile>(totalNumTiles, Allocator.Persistent);
        Vector3 firstTileCenter = center - ((Vector3.one * size)/2) + (Vector3.one * tileSize / 2);
        firstTileCenter.y = 0;
        for (int i = 0; i < result.Length; i++)
        {
            Vector3 tileCenter;
            tileCenter.x = tileSize * (i % fittingTiles);
            tileCenter.y = 0;
            tileCenter.z = tileSize * (i / fittingTiles);
            Tile tile = result[i];
            GenerateTile(ref tile, tileCenter, tileSize, density);
            tile.bounds = new Bounds(tileCenter, new Vector3(tileSize, 5f, tileSize));
            tile.index = 0;
            tile.extents = tileSize / 2;
            tile.center = tileCenter;
            result[i] = tile;
        }
        return result;
    }

    [BurstCompile]
    public static ref Tile GenerateTile(ref Tile tile, Vector3 center, float size, float density)
    {
        tile.positions = GenerateGrassPositions(size, density, center);
        return ref tile;
    }
}
