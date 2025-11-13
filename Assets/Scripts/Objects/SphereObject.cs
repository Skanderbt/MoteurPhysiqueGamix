using UnityEngine;

public class SphereObject : PhysicsObject
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

    void CreateSphereMesh()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();

        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null) meshRenderer = gameObject.AddComponent<MeshRenderer>();

        sphereMesh = new Mesh();
        GenerateSphereMesh(radius, resolution);

        meshFilter.mesh = sphereMesh;

        // Matériau
        Material material = new Material(Shader.Find("Standard"));
        material.color = Color.blue;
        meshRenderer.material = material;
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

                Vector3 pointOnSphere = PointOnSphere(xPos, yPos, radius);
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
    }

    Vector3 PointOnSphere(float u, float v, float radius)
    {
        float theta = u * 2 * Mathf.PI;
        float phi = v * Mathf.PI;

        float x = radius * Mathf.Sin(phi) * Mathf.Cos(theta);
        float y = radius * Mathf.Cos(phi);
        float z = radius * Mathf.Sin(phi) * Mathf.Sin(theta);

        return new Vector3(x, y, z);
    }

    protected override void InitializeBounds()
    {
        bounds = new Bounds(transform.position, Vector3.one * radius * 2);
        size = bounds.size;
    }

    protected override void UpdateBounds()
    {
        bounds.center = transform.position;
        bounds.size = Vector3.one * radius * 2;
    }

    protected override void CheckGroundCollision()
    {
        float groundLevel = 0f;
        float bottom = transform.position.y - radius;

        if (bottom <= groundLevel && velocity.y < 0)
        {
            // Collision avec le sol
            transform.position = new Vector3(
                transform.position.x,
                groundLevel + radius,
                transform.position.z
            );

            // Appliquer la restitution
            velocity.y = -velocity.y * bounciness;

            // Appliquer le frottement
            velocity.x *= (1f - friction);
            velocity.z *= (1f - friction);

            Debug.Log($"Sphere ground collision! Bounce: {velocity.y}");
        }
    }

    protected override void ResolveCollision(PhysicsObject other)
    {
        if (other is SphereObject otherSphere)
        {
            ResolveSphereSphereCollision(otherSphere);
        }
        else
        {
            base.ResolveCollision(other);
        }
    }

    void ResolveSphereSphereCollision(SphereObject other)
    {
        Vector3 collisionNormal = (transform.position - other.transform.position).normalized;
        float distance = Vector3.Distance(transform.position, other.transform.position);
        float overlap = (radius + other.radius) - distance;

        if (overlap > 0)
        {
            // Séparer les sphères
            transform.position += collisionNormal * overlap * 0.5f;
            other.transform.position -= collisionNormal * overlap * 0.5f;

            // Collision élastique
            Vector3 relativeVelocity = velocity - other.velocity;
            float velocityAlongNormal = Vector3.Dot(relativeVelocity, collisionNormal);

            if (velocityAlongNormal > 0) return;

            float restitution = Mathf.Min(bounciness, other.bounciness);
            float j = -(1 + restitution) * velocityAlongNormal;
            j /= (1 / mass) + (1 / other.mass);

            Vector3 impulse = j * collisionNormal;
            velocity += impulse / mass;
            other.velocity -= impulse / other.mass;

            Debug.Log($"Sphere-sphere collision! Impulse: {impulse.magnitude}");
        }
    }
}