using UnityEngine;

public class CubeObject
{
    public GameObject cube;
    public Material material;
    private Mesh mesh;
    private Vector3[] originalVertices;
    public Color cubeColor;

    // Constructeur pour RigidBody3DState
    public CubeObject(float a, float b, float c, Color color)
    {
        cubeColor = color;

        // Créer le GameObject
        cube = new GameObject("PhysicsCube");

        // Ajouter les composants nécessaires
        MeshFilter meshFilter = cube.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = cube.AddComponent<MeshRenderer>();

        // Créer le mesh manuellement
        mesh = new Mesh();
        mesh.name = "CubeMesh";

        float hx = a / 2, hy = b / 2, hz = c / 2;

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
        mesh.RecalculateBounds();

        meshFilter.mesh = mesh;

        // Appliquer la couleur
        material = new Material(Shader.Find("Standard"));
        material.color = cubeColor;
        meshRenderer.material = material;

        // Activer les ombres
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        meshRenderer.receiveShadows = true;
    }

    // Constructeur pour FreeFall
    public CubeObject(Vector3 position, Color color, string name = "CubeObject")
    {
        cubeColor = color;

        // Créer le GameObject
        cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.position = position;

        // Cloner le mesh pour éviter les références partagées
        MeshFilter mf = cube.GetComponent<MeshFilter>();
        mesh = Object.Instantiate(mf.sharedMesh);
        mf.mesh = mesh;

        // Sauvegarder les sommets originaux
        originalVertices = mesh.vertices;

        // Appliquer la couleur
        material = new Material(Shader.Find("Standard"));
        material.color = cubeColor;
        cube.GetComponent<Renderer>().material = material;

        // Activer les ombres
        Renderer renderer = cube.GetComponent<Renderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        renderer.receiveShadows = true;
    }

    // Méthode pour appliquer une matrice de transformation
    public void ApplyMatrix(float[,] M)
    {
        if (mesh == null || originalVertices == null)
        {
            Debug.LogWarning("Mesh or originalVertices is null in ApplyMatrix");
            return;
        }

        Vector3[] transformed = new Vector3[originalVertices.Length];

        for (int i = 0; i < originalVertices.Length; i++)
        {
            Vector3 v = originalVertices[i];
            float x = v.x, y = v.y, z = v.z;

            float newX = M[0, 0] * x + M[0, 1] * y + M[0, 2] * z + M[0, 3];
            float newY = M[1, 0] * x + M[1, 1] * y + M[1, 2] * z + M[1, 3];
            float newZ = M[2, 0] * x + M[2, 1] * y + M[2, 2] * z + M[2, 3];

            transformed[i] = new Vector3(newX, newY, newZ);
        }

        mesh.vertices = transformed;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    // Méthode pour appliquer une transformation Matrix4x4
    public void ApplyMatrix(Matrix4x4 M)
    {
        if (mesh == null || originalVertices == null)
        {
            Debug.LogWarning("Mesh or originalVertices is null in ApplyMatrix");
            return;
        }

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
        mesh.RecalculateBounds();
    }

    // Méthode pour détruire l'objet
    public void Destroy()
    {
        if (cube != null) Object.Destroy(cube);
        if (material != null) Object.Destroy(material);
    }

    // Propriétés pour accéder aux vertices et triangles (pour RigidBody3DState)
    public Vector3[] vertices
    {
        get { return originalVertices; }
    }

    public int[] triangles
    {
        get { return mesh.triangles; }
    }

    // Méthode pour changer la couleur
    public void SetColor(Color newColor)
    {
        cubeColor = newColor;
        if (material != null)
        {
            material.color = newColor;
        }
    }

    // Méthode pour définir la position directement
    public void SetPosition(Vector3 position)
    {
        if (cube != null)
        {
            cube.transform.position = position;
        }
    }
}