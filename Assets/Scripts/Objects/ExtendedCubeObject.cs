using UnityEngine;

public class ExtendedCubeObject : MonoBehaviour
{
    [Header("Cube Properties")]
    public float width = 1f;
    public float height = 1f;
    public float depth = 1f;
    public Color color = Color.white;

    [Header("Physics Properties")]
    public float mass = 1f;
    public Vector3 velocity = Vector3.zero;
    public bool useGravity = true;

    // Mesh components
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh mesh;
    private Vector3[] originalVertices;

    void Start()
    {
        InitializeCube();
    }

    void InitializeCube()
    {
        // Create or get mesh components
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();
        if (meshRenderer == null) meshRenderer = gameObject.AddComponent<MeshRenderer>();

        // Create mesh using your vertex-based approach
        mesh = new Mesh();
        float hx = width / 2, hy = height / 2, hz = depth / 2;

        originalVertices = new Vector3[]
        {
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

        mesh.vertices = originalVertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;

        // Setup material
        Material material = new Material(Shader.Find("Standard"));
        material.color = color;
        meshRenderer.material = material;
    }

    // Method to apply transformation matrix (from your CubeObject)
    public void ApplyMatrix(float[,] M)
    {
        if (mesh == null || originalVertices == null) return;

        Vector3[] transformed = new Vector3[originalVertices.Length];

        for (int i = 0; i < originalVertices.Length; i++)
        {
            Vector3 v = originalVertices[i];
            transformed[i] = new Vector3(
                M[0, 0] * v.x + M[0, 1] * v.y + M[0, 2] * v.z + M[0, 3],
                M[1, 0] * v.x + M[1, 1] * v.y + M[1, 2] * v.z + M[1, 3],
                M[2, 0] * v.x + M[2, 1] * v.y + M[2, 2] * v.z + M[2, 3]
            );
        }

        mesh.vertices = transformed;
        mesh.RecalculateNormals();
    }

    // Method to apply transformation using your Math3D utilities
    public void ApplyTransformation(Matrix4x4 transformation)
    {
        if (mesh == null || originalVertices == null) return;

        Vector3[] transformed = new Vector3[originalVertices.Length];

        for (int i = 0; i < originalVertices.Length; i++)
        {
            transformed[i] = Math3D.MultiplyMatrixVector3(transformation, originalVertices[i]);
        }

        mesh.vertices = transformed;
        mesh.RecalculateNormals();
    }

    public void SetColor(Color newColor)
    {
        color = newColor;
        if (meshRenderer != null && meshRenderer.material != null)
        {
            meshRenderer.material.color = newColor;
        }
    }

    public void SetSize(float newWidth, float newHeight, float newDepth)
    {
        width = newWidth;
        height = newHeight;
        depth = newDepth;
        InitializeCube(); // Recreate with new dimensions
    }
}