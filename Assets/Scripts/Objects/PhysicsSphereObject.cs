using UnityEngine;

public class PhysicsSphereObject : PhysicsCubeObject
{
    [Header("Sphere Properties")]
    public float radius = 0.5f;
    public int resolution = 16;

    private Mesh sphereMesh;

    void Start()
    {
        CreateSphereMesh();
        InitializeBounds();
    }

    public void CreateSphereMesh()
    {
        meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();

        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null) meshRenderer = gameObject.AddComponent<MeshRenderer>();

        sphereMesh = new Mesh();
        GenerateSphereMesh(radius, resolution);

        meshFilter.mesh = sphereMesh;
        SetColor(color);
    }

    void GenerateSphereMesh(float radius, int resolution)
    {
        Vector3[] vertices = new Vector3[resolution * resolution];
        Vector2[] uv = new Vector2[vertices.Length];
        int[] triangles = new int[(resolution - 1) * (resolution - 1) * 6];

        int triIndex = 0;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float xPos = (float)x / (resolution - 1);
                float yPos = (float)y / (resolution - 1);

                Vector3 pointOnSphere = CalculatePointOnSphere(xPos, yPos, radius);
                vertices[y * resolution + x] = pointOnSphere;
                uv[y * resolution + x] = new Vector2(xPos, yPos);

                if (x < resolution - 1 && y < resolution - 1)
                {
                    int current = y * resolution + x;
                    int next = current + 1;
                    int below = current + resolution;
                    int belowNext = below + 1;

                    triangles[triIndex] = current;
                    triangles[triIndex + 1] = below;
                    triangles[triIndex + 2] = next;

                    triangles[triIndex + 3] = next;
                    triangles[triIndex + 4] = below;
                    triangles[triIndex + 5] = belowNext;

                    triIndex += 6;
                }
            }
        }

        sphereMesh.vertices = vertices;
        sphereMesh.triangles = triangles;
        sphereMesh.uv = uv;
        sphereMesh.RecalculateNormals();
        sphereMesh.RecalculateBounds();
    }

    Vector3 CalculatePointOnSphere(float u, float v, float radius)
    {
        float theta = u * 2 * Mathf.PI;
        float phi = v * Mathf.PI;

        float x = radius * Mathf.Sin(phi) * Mathf.Cos(theta);
        float y = radius * Mathf.Cos(phi);
        float z = radius * Mathf.Sin(phi) * Mathf.Sin(theta);

        return new Vector3(x, y, z);
    }

    public override void InitializeBounds()
    {
        bounds = new Bounds(transform.position, Vector3.one * radius * 2);
        size = bounds.size;
        width = radius * 2;
        height = radius * 2;
        depth = radius * 2;
    }

    public override void UpdateBounds()
    {
        bounds.center = transform.position;
        bounds.size = Vector3.one * radius * 2;
    }

    // FIX: Use override instead of new
    public override void ApplyForces(float deltaTime)
    {
        Vector3 totalForce = Vector3.zero;

        // Gravity - USE THE MANAGER'S GRAVITY
        if (useGravity && physicsManager != null)
        {
            totalForce += new Vector3(0, -physicsManager.gravity * mass, 0);
        }

        // Air resistance
        if (physicsManager != null && physicsManager.enableAirResistance)
        {
            Vector3 airResistance = -velocity * friction * mass;
            totalForce += airResistance;
        }

        // Newton's second law
        acceleration = totalForce / mass;
    }

    public override void CheckGroundCollision()
    {
        if (!enableCollisions) return;

        float groundLevel = 0f;
        float bottom = transform.position.y - radius;

        if (bottom <= groundLevel && velocity.y < 0)
        {
            float penetration = groundLevel - bottom;
            transform.position += new Vector3(0, penetration, 0);

            velocity.y = -velocity.y * bounciness;
            velocity.x *= (1f - friction);
            velocity.z *= (1f - friction);

            if (Mathf.Abs(velocity.y) < 0.1f)
            {
                velocity.y = 0f;
            }
        }
    }
}