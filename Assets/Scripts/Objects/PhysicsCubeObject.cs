using UnityEngine;

public class PhysicsCubeObject : MonoBehaviour
{
    [Header("Cube Properties")]
    public float width = 1f;
    public float height = 1f;
    public float depth = 1f;
    public Color color = Color.white;

    [Header("Physics Properties")]
    public float mass = 1.0f;
    public Vector3 velocity = Vector3.zero;
    public Vector3 acceleration = Vector3.zero;
    public Vector3 angularVelocity = Vector3.zero; // Rotation velocity
    public bool useGravity = true;
    public float bounciness = 0.7f;
    public float friction = 0.1f;
    public bool enableCollisions = true;
    public float angularDamping = 0.95f; // Rotation slows down over time

    protected PhysicsSimulationManager physicsManager;
    protected MeshFilter meshFilter;
    protected MeshRenderer meshRenderer;
    protected Mesh mesh;
    protected Vector3[] originalVertices;
    protected Bounds bounds;
    protected Vector3 size = Vector3.one;
    private Vector3 lastPosition;

    void Awake()
    {
        physicsManager = FindFirstObjectByType<PhysicsSimulationManager>();
    }

    void Start()
    {
        CreateCubeMesh();
        InitializeBounds();
        lastPosition = transform.position;
    }

    public void SetPhysicsManager(PhysicsSimulationManager manager)
    {
        physicsManager = manager;
    }

    public void CreateCubeMesh()
    {
        // Assurer que les composants existent
        meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();

        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null) meshRenderer = gameObject.AddComponent<MeshRenderer>();

        // Cr�er un nouveau mesh
        mesh = new Mesh();
        mesh.name = "CubeMesh";

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
        mesh.RecalculateBounds();

        meshFilter.mesh = mesh;

        // CORRECTION: Appliquer la couleur APR�S avoir cr�� le mesh
        SetColor(color);
    }

    // CORRECTION: M�thode am�lior�e pour d�finir la couleur
    public void SetColor(Color newColor)
    {
        color = newColor;

        if (meshRenderer == null)
        {
            meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null) return;
        }

        // CORRECTION: V�rifier si le mat�riau existe d�j�
        if (meshRenderer.material == null)
        {
            // Cr�er un nouveau mat�riau
            Material material = new Material(Shader.Find("Standard"));
            material.color = color;
            material.enableInstancing = true;
            meshRenderer.material = material;
        }
        else
        {
            // CORRECTION: Modifier la couleur du mat�riau existant
            meshRenderer.material.color = color;
        }

        // CORRECTION: Forcer la mise � jour
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        meshRenderer.receiveShadows = true;

        // CORRECTION: Forcer la mise � jour du mat�riau
        if (meshRenderer.material != null)
        {
            meshRenderer.material.name = "DynamicMaterial_" + gameObject.name;
        }
    }

    public virtual void InitializeBounds()
    {
        bounds = new Bounds(transform.position, new Vector3(width, height, depth));
        size = bounds.size;
    }

    public virtual void ApplyForces(float deltaTime)
    {
        Vector3 totalForce = Vector3.zero;

        if (useGravity)
        {
            float gravityValue = physicsManager != null ? physicsManager.gravity : 9.81f;
            totalForce += new Vector3(0, -gravityValue * mass, 0);
        }

        if (physicsManager != null && physicsManager.enableAirResistance)
        {
            Vector3 airResistance = -velocity * friction * mass;
            totalForce += airResistance;
        }

        acceleration = totalForce / mass;
    }

    public void Integrate(float deltaTime)
    {
        // Linear motion
        velocity += acceleration * deltaTime;
        velocity = Vector3.ClampMagnitude(velocity, 50f);
        transform.position += velocity * deltaTime;
        
        // Angular motion (rotation)
        if (angularVelocity.magnitude > 0.01f)
        {
            // Apply rotation
            Quaternion deltaRotation = Quaternion.Euler(angularVelocity * deltaTime * Mathf.Rad2Deg);
            transform.rotation = deltaRotation * transform.rotation;
            
            // Apply damping (rotation slows down over time)
            angularVelocity *= angularDamping;
        }
        
        UpdateBounds();
    }

    public virtual void UpdateBounds()
    {
        bounds.center = transform.position;
        bounds.size = new Vector3(width, height, depth);
    }

    public virtual void CheckGroundCollision()
    {
        if (!enableCollisions) return;

        float groundLevel = 0f;
        float bottom = transform.position.y - height / 2;

        if (bottom <= groundLevel && velocity.y < 0)
        {
            float penetration = groundLevel - bottom;
            transform.position += new Vector3(0, penetration, 0);

            // Bounce
            velocity.y = -velocity.y * bounciness;
            velocity.x *= (1f - friction);
            velocity.z *= (1f - friction);
            
            // Add rotation from ground impact if moving horizontally
            Vector3 horizontalVel = new Vector3(velocity.x, 0, velocity.z);
            if (horizontalVel.magnitude > 0.5f)
            {
                // Torque from friction - cube rolls
                Vector3 rotationAxis = Vector3.Cross(Vector3.up, horizontalVel.normalized);
                float rotationMagnitude = horizontalVel.magnitude * friction * 2f;
                angularVelocity += rotationAxis * rotationMagnitude;
            }
            
            // Apply friction to rotation too
            angularVelocity *= (1f - friction * 0.5f);

            if (Mathf.Abs(velocity.y) < 0.1f)
            {
                velocity.y = 0f;
            }
        }
    }

    public void AddForce(Vector3 force)
    {
        velocity += force / mass;
    }

    public void AddImpulse(Vector3 impulse)
    {
        velocity += impulse;
    }

    public Bounds GetBounds()
    {
        return bounds;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(bounds.center, bounds.size);

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, velocity.normalized * 2f);
    }
}