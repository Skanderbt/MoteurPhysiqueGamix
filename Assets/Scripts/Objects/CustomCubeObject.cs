using UnityEngine;

public class CustomCubeObject : PhysicsObject
{
    [Header("Cube Properties")]
    public float cubeWidth = 1f;
    public float cubeHeight = 1f;
    public float cubeDepth = 1f;
    public Color cubeColor = Color.white;

    void Start()
    {
        CreateCubeMesh();
        InitializeBounds();
    }

    void CreateCubeMesh()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();

        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null) meshRenderer = gameObject.AddComponent<MeshRenderer>();

        Mesh mesh = new Mesh();

        float hx = cubeWidth / 2, hy = cubeHeight / 2, hz = cubeDepth / 2;

        Vector3[] vertices = {
            new Vector3(-hx, -hy, -hz), new Vector3(hx, -hy, -hz),
            new Vector3(hx, hy, -hz), new Vector3(-hx, hy, -hz),
            new Vector3(-hx, -hy, hz), new Vector3(hx, -hy, hz),
            new Vector3(hx, hy, hz), new Vector3(-hx, hy, hz)
        };

        int[] triangles = {
            0,2,1, 0,3,2, 4,5,6, 4,6,7,
            0,1,5, 0,5,4, 2,3,7, 2,7,6,
            1,2,6, 1,6,5, 0,4,7, 0,7,3
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;

        // Matériau
        Material material = new Material(Shader.Find("Standard"));
        material.color = cubeColor;
        meshRenderer.material = material;
    }

    protected override void InitializeBounds()
    {
        bounds = new Bounds(transform.position, new Vector3(cubeWidth, cubeHeight, cubeDepth));
        size = bounds.size;
    }

    protected override void UpdateBounds()
    {
        bounds.center = transform.position;
        bounds.size = new Vector3(cubeWidth, cubeHeight, cubeDepth);
    }
}