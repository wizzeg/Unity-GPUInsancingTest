using System.Drawing;
using Unity.VisualScripting;
using UnityEngine;

public class Landscape : MonoBehaviour
{

    [SerializeField]
    private float size = 300;
    [SerializeField]
    private float density = 1;

    [SerializeField]
    private Mesh mesh;
    [SerializeField]
    private Material material;

    ComputeBuffer positionBuffer;
    ComputeBuffer argsBuffer;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //transform.Rotate(90, 0, 0);
        transform.localScale = new Vector3(size, size, size);

        Vector3[] Vectors = HelperScripts.GenerateGrassPositions(size, density, transform.position);
        int InstanceCount = Vectors.Length;
        Debug.Log("Grass rendered: " + InstanceCount);

        positionBuffer = new ComputeBuffer(InstanceCount, sizeof(float) * 4, ComputeBufferType.Structured);
        Vector4[] meshPositions = new Vector4[InstanceCount];
        for (int i = 0; i < InstanceCount; i++)
        {
            meshPositions[i] = new Vector4(Vectors[i].x, Vectors[i].y, Vectors[i].z, 10.0f);
        }

        positionBuffer.SetData(meshPositions);
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
        positionBuffer.Release();
        positionBuffer = null;
        argsBuffer.Release();
        argsBuffer = null;
        Debug.Log("Released buffers");
    }

    // Update is called once per frame
    void Update()
    {
        Bounds bounds = new Bounds(transform.position, Vector3.one *  size);
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
}
