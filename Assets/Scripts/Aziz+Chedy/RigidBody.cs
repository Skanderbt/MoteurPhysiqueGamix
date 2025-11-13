using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CustomRigidBody : MonoBehaviour
{
    public float mass = 1f;
    public Vector3 size = Vector3.one;
    public float gravity = 9.81f;

    public Vector3 position;
    public Vector3 velocity = Vector3.zero;
    private Vector3 accumulatedForce = Vector3.zero;
    
    // For better integration
    private Vector3 previousAcceleration = Vector3.zero;

    private Mesh mesh;
    private CubeMesh cubeTemplate;
    private float dt => Time.fixedDeltaTime;

    void Awake()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        var mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = Color.cyan;
        GetComponent<MeshRenderer>().material = mat;

        cubeTemplate = new CubeMesh(size.x, size.y, size.z);
    }

    void Start()
    {
        position = transform.position;
    }

    public void AddForce(Vector3 force)
    {
        accumulatedForce += force;
    }

    void FixedUpdate()
    {
        // Add gravity force
        accumulatedForce += new Vector3(0, -gravity * mass, 0);

        // Calculate acceleration
        Vector3 acceleration = accumulatedForce / mass;
        
        // Semi-implicit Euler (Symplectic Euler) - more stable for oscillations
        // Update velocity first, then position using new velocity
        velocity += acceleration * dt;
        position += velocity * dt;

        // Ground collision with coefficient of restitution
        float halfY = size.y * 0.5f;
        if (position.y < halfY)
        {
            position.y = halfY;
            if (velocity.y < 0)
            {
                velocity.y *= -0.3f; // 30% bounce
                
                // Add small friction to x and z when bouncing
                velocity.x *= 0.95f;
                velocity.z *= 0.95f;
            }
        }

        // Store acceleration for potential future use
        previousAcceleration = acceleration;
        
        // Clear forces for next frame
        accumulatedForce = Vector3.zero;
        
        UpdateMesh();
        transform.position = position;
    }

    void UpdateMesh()
    {
        Vector3[] verts = new Vector3[cubeTemplate.vertices.Length];
        for (int i = 0; i < verts.Length; i++)
        {
            // Use local space coordinates, not world space
            verts[i] = cubeTemplate.vertices[i];
        }
        mesh.Clear();
        mesh.vertices = verts;
        mesh.triangles = cubeTemplate.triangles;
        mesh.RecalculateNormals();
    }

    public Vector3 AttachmentPoint => position + new Vector3(0, size.y * 0.5f, 0);
}