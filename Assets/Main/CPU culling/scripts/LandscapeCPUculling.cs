using System.Collections.Generic;
using Unity.Collections;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;
using UnityEngine.Rendering;

public class LandscapeCPUculling: MonoBehaviour
{
    [SerializeField]
    private float size = 300;
    [SerializeField]
    private float density = 1;
    [SerializeField]
    private int numberOfTiles = 11;

    [SerializeField]
    private Mesh mesh;
    [SerializeField]
    private Material material;

    ComputeBuffer positionBuffer;
    ComputeBuffer argsBuffer;

    int lastIndex = 0;
    int grassPerTile = 0;

    List<int> freePositions;
    List<int> toAdd;
    int[] addedIndexes;
    int addedIndexesTail;

    Camera cam;


    NativeArray<HelperScripts.Tile> TileArray;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cam = Camera.main;
        //transform.Rotate(90, 0, 0);

        TileArray = HelperScripts.GenerateGrassTiles(size, density, transform.position, size / numberOfTiles);
        int InstanceCount = 0;
        for (int i = 0; i < TileArray.Length; i++)
        {
            InstanceCount = TileArray[i].positions.Length;
            grassPerTile = TileArray[i].positions.Length;
        }
        Debug.Log("Grass positions: " + InstanceCount);

        addedIndexes = new int[numberOfTiles * numberOfTiles];
        for (int i = 0; i < addedIndexes.Length; i++)
        {
            addedIndexes[i] = -1;
        }
        addedIndexesTail = 0;

        //Problem StructuredBuffer[] does not exist in HLSL
        //Solution: use a single buffer, devise a structure with indexing to know which portion of the buffer is responsible for each tile, use SetData to swap out portions
        //Issue, compaction will be difficult in the general case, due to memory fragmentation/packing problem... But not an issue for this case, as each tile has same number of grass blades
        //Consideration: May be easier to do culling entirely on the GPU.

        //Steps
        //1. Create buffer that can hold every grass blade for every tile (could even put restrictions if viewport has restrictions)
        //2. Cull ones that are not close enough to viewport, can check angle to viewport direction based on field of view
        //3. Fill positions buffer and args buffer with correct data, mark tiles as visible


        positionBuffer = new ComputeBuffer(InstanceCount, sizeof(float) * 4, ComputeBufferType.Structured);

        for (int i = 0; i < TileArray.Length; i++)
        {
            InstanceCount = TileArray[i].positions.Length;
        }
        PreparePositionBuffer();

        //positionBuffer.SetData(meshPositions);
        material.SetBuffer("_Positions", positionBuffer);

        

        uint[] args = new uint[5]
        {
            (uint)mesh.GetIndexCount(0),
            (uint)InstanceCount,
            (uint)mesh.GetIndexStart(0),
            (uint)mesh.GetBaseVertex(0),
            0u
        };

        argsBuffer = new ComputeBuffer(InstanceCount, sizeof(uint) * args.Length, ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(args);
    }

    private void OnDisable()
    {
        ReleaseBuffers();
    }

    // Update is called once per frame
    void Update()
    {
        //Todo every frame
        //need to check every tile if they're visible, if they were visible previous frame but not now -> mark index in listas free + set visible = false,
        // if they are and were visible do nothing, if they are visible and weren't check free index list and SetData,
        // if free list is not empty -> move last free index into free index -> repeat to till free list is empty

        //1. Keep a list of free "indexes" or positions/spans of positions. + List of "to add" indexes
        //2. Run a for loop over all tiles
        //3. Check if tile is visible -> If not visible but were visible -> add index to free index list ... -> If visible but weren't visible add to "to add" List
        //4. For loop to add from "to add" to free list, if run out of space append at back of computebuffer
        //5. If free index list is not empty, fill in holes from back of computebuffer

        //6. do draw call
        uint[] args = new uint[5]
{
            (uint)mesh.GetIndexCount(0),
            (uint)lastIndex,
            (uint)mesh.GetIndexStart(0),
            (uint)mesh.GetBaseVertex(0),
            0u
        };
        argsBuffer.SetData(args);

        Bounds bounds = new Bounds(transform.position, Vector3.one * size);
        Graphics.DrawMeshInstancedIndirect(
            mesh,
            0,
            material,
            bounds,
            argsBuffer,
            0,
            null,
            UnityEngine.Rendering.ShadowCastingMode.Off,
            false,
            gameObject.layer
            );
    }

    void ReleaseBuffers()
    {
        positionBuffer.Release();
        positionBuffer = null;
        argsBuffer.Release();
        argsBuffer = null;
        TileArray.Dispose();
    }

    void PreparePositionBuffer()
    {
        bool isVisible = false;
        toAdd.Clear();
        freePositions.Clear();
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cam);
        for (int i = 0; i < TileArray.Length; i++)
        {
            
            isVisible = GeometryUtility.TestPlanesAABB(planes, TileArray[i].bounds);
            if (isVisible && !TileArray[i].visible)
            {
                toAdd.Add(i);
            }
            else if (!isVisible && TileArray[i].visible)
            {
                freePositions.Add(TileArray[i].index);
                HelperScripts.Tile tile = TileArray[TileArray[i].index];
                tile.visible = false;
                TileArray[TileArray[i].index] = tile;
            }
        }

        int added = 0;
        int currentIndex;
        for (int i = 0; i < toAdd.Count; i++)
        {
            currentIndex = toAdd[i];
            HelperScripts.Tile tile = TileArray[currentIndex];

            if (added < freePositions.Count)
            {
                positionBuffer.SetData(TileArray[currentIndex].positions, 0, grassPerTile * freePositions[added], grassPerTile);
                tile.index = freePositions[added];
                addedIndexes[added] = tile.index;
                tile.visible = true;
                TileArray[currentIndex] = tile;
            }
            else
            {
                positionBuffer.SetData(TileArray[currentIndex].positions, 0, grassPerTile * lastIndex, grassPerTile);
                tile.index = lastIndex / grassPerTile;
                addedIndexes[addedIndexesTail] = tile.index;
                lastIndex += grassPerTile;
                tile.visible = true;
                TileArray[currentIndex] = tile;
            }

            
            added++;
        }

        while (freePositions.Count > added)
        {
            if (lastIndex < 0) break;
            positionBuffer.SetData(TileArray[addedIndexes[addedIndexesTail]].positions, 0, freePositions[added] * grassPerTile, grassPerTile);
            lastIndex -= grassPerTile;
            addedIndexesTail--;
            added++;
        }
    }
}
