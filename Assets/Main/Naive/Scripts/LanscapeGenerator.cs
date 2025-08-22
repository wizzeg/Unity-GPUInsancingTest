using UnityEngine;

public class LanscapeGenerator : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField]
    private float size = 300;
    [SerializeField]
    private float density = 1;

    [SerializeField]
    private GameObject GrassBladePrefab;

    MeshFilter mesh;

    void Start()
    {
        TryGetComponent<MeshFilter>(out mesh);
        if (mesh != null)
        {
            Debug.Log("Mesh not null");
            //transform.Rotate(90, 0, 0);
            transform.localScale = new Vector3(size, size, size);
        }

        Vector3[] Vectors = HelperScripts.GenerateGrassPositions(size, density, transform.position);
        foreach (var v in Vectors)
        {
            GameObject grass = Instantiate(GrassBladePrefab, v, Quaternion.identity);
            grass.transform.localScale = new Vector3(10, 10, 10);
        }
        Debug.Log("There are " + Vectors.Length + " grass blades.");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

}
