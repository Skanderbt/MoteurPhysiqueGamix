using UnityEngine;

/// <summary>
/// Simple manager for Plate Impact scene - spawns plate falling on sphere
/// </summary>
public class PlateImpactManager : MonoBehaviour, IPhysicsExample
{
    private class Block
    {
        public GameObject go;
        public Vector3 vel;
        public Vector3 angularVel;
        public float mass = 1f;
        public float radius;
    }

    private class Constraint
    {
        public int a, b;
        public float rest;
        public float stiffness;
        public bool broken;
    }

    private System.Collections.Generic.List<Block> blocks = new System.Collections.Generic.List<Block>();
    private System.Collections.Generic.List<Constraint> constraints = new System.Collections.Generic.List<Constraint>();
    private GameObject sphere;
    
    private float groundY = 0f;
    private float dt = 0.016f;
    private Vector3 gravity = new Vector3(0, -0.8f, 0);
    private bool initialized = false;

    [Header("Plate Parameters")]
    public int gridSize = 8; // Larger grid for thin plate
    public float blockSize = 0.2f; // Very thin blocks
    public float blockSpacing = 0.22f; // Tight spacing for solid plate
    public float plateThickness = 0.15f; // Thin in Y direction
    public float stiffness = 40f; // Moderate stiffness
    public float breakThreshold = 0.15f; // Breaks easily on impact
    public float damping = 0.9995f; // High damping to slow pieces down
    public float alpha = 0.08f; // Energy transfer for visible separation
    public float restitution = 0.02f; // Very low bounce
    public float friction = 0.98f; // High friction
    public float sphereRadius = 1.2f; // Smaller sphere

    void Start()
    {
        CreateGround();
        Initialize(new Vector3(0, 20f, 0), groundY, dt, gravity);
        CreateLabel();
    }

    void CreateGround()
    {
        var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.name = "Ground";
        plane.transform.position = new Vector3(0, groundY, 0);
        plane.transform.localScale = new Vector3(5, 1, 5);
        var rend = plane.GetComponent<MeshRenderer>();
        rend.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        rend.material.color = new Color(0.2f, 0.6f, 0.3f, 1f);
        Destroy(plane.GetComponent<Collider>());
    }

    void CreateLabel()
    {
        var labelObj = new GameObject("Label_PLATE_IMPACT");
        labelObj.transform.position = new Vector3(0f, 26f, 0f);
        var textMesh = labelObj.AddComponent<TextMesh>();
        textMesh.text = "PLATE IMPACT";
        textMesh.fontSize = 30;
        textMesh.color = Color.white;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.characterSize = 0.5f;
    }

    public void Initialize(Vector3 position, float groundY, float dt, Vector3 gravity)
    {
        this.groundY = groundY;
        this.dt = dt;
        this.gravity = gravity;
        this.initialized = true;
        
        CreateSphere(position);
        CreatePlate(position);
    }

    void CreateSphere(Vector3 centerPos)
    {
        sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.position = new Vector3(0, groundY + sphereRadius, 0);
        sphere.transform.localScale = Vector3.one * sphereRadius * 2f;
        sphere.name = "ImpactSphere";
        Destroy(sphere.GetComponent<Collider>());
        
        var rend = sphere.GetComponent<MeshRenderer>();
        rend.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        rend.material.color = new Color(0.7f, 0.3f, 0.2f, 1f);
    }

    void CreatePlate(Vector3 centerPos)
    {
        // Create a single-layer thin plate (like glass)
        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                float xPos = (x - gridSize / 2f + 0.5f) * blockSpacing;
                float zPos = (z - gridSize / 2f + 0.5f) * blockSpacing;
                Vector3 pos = centerPos + new Vector3(xPos, 0, zPos);
                
                var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                block.transform.position = pos;
                // Make it THIN - small in Y direction
                block.transform.localScale = new Vector3(blockSize, plateThickness, blockSize);
                block.name = $"PlateBlock_{x}_{z}";
                Destroy(block.GetComponent<Collider>());
                
                var rend = block.GetComponent<MeshRenderer>();
                rend.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                // Light blue/cyan color like glass
                rend.material.color = new Color(0.7f, 0.85f, 0.95f, 1f);
                
                blocks.Add(new Block
                {
                    go = block,
                    vel = Vector3.zero,
                    angularVel = Vector3.zero,
                    mass = 0.5f, // Light like glass
                    radius = blockSize * 0.5f
                });
            }
        }

        // Connect only immediate neighbors (4-way grid connectivity for thin plate)
        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                int idx = x * gridSize + z;
                
                // Connect to right neighbor
                if (x < gridSize - 1)
                {
                    int rightIdx = (x + 1) * gridSize + z;
                    float dist = Vector3.Distance(blocks[idx].go.transform.position, blocks[rightIdx].go.transform.position);
                    constraints.Add(new Constraint { a = idx, b = rightIdx, rest = dist, stiffness = stiffness, broken = false });
                }
                
                // Connect to back neighbor
                if (z < gridSize - 1)
                {
                    int backIdx = x * gridSize + (z + 1);
                    float dist = Vector3.Distance(blocks[idx].go.transform.position, blocks[backIdx].go.transform.position);
                    constraints.Add(new Constraint { a = idx, b = backIdx, rest = dist, stiffness = stiffness, broken = false });
                }
            }
        }
        
        Debug.Log($"Thin plate created: {blocks.Count} blocks, {constraints.Count} constraints");
    }

    public void StepSimulation()
    {
        if (!initialized) return;

        foreach (var b in blocks)
            b.vel += gravity * dt;

        foreach (var c in constraints)
        {
            if (c.broken) continue;

            var a = blocks[c.a];
            var b = blocks[c.b];
            
            Vector3 pa = a.go.transform.position;
            Vector3 pb = b.go.transform.position;
            
            Vector3 d = pb - pa;
            float len = d.magnitude;
            if (len < 1e-6f) continue;
            
            Vector3 n = d / len;
            float extension = len - c.rest;
            
            Vector3 F = -c.stiffness * extension * n;
            
            a.vel += F * dt / a.mass;
            b.vel -= F * dt / b.mass;
            
            if (Mathf.Abs(extension) > breakThreshold * c.rest)
            {
                c.broken = true;
                
                float E = 0.5f * c.stiffness * extension * extension;
                float dV = Mathf.Sqrt(2f * alpha * E / a.mass);
                
                // Random impulse - like cube fracture, chaotic and natural
                Vector3 randomDirA = Random.onUnitSphere * Random.Range(0.5f, 1.5f);
                Vector3 randomDirB = Random.onUnitSphere * Random.Range(0.5f, 1.5f);
                
                a.vel += (-n + randomDirA) * dV * 0.15f;
                b.vel += (n + randomDirB) * dV * 0.15f;
                
                // Random rotation for chaotic tumbling
                a.angularVel += Random.onUnitSphere * dV * Random.Range(0.05f, 0.15f);
                b.angularVel += Random.onUnitSphere * dV * Random.Range(0.05f, 0.15f);
            }
        }

        Vector3 sphereCenter = sphere.transform.position;
        foreach (var b in blocks)
        {
            Vector3 toBlock = b.go.transform.position - sphereCenter;
            float dist = toBlock.magnitude;
            float minDist = sphereRadius + b.radius;
            
            if (dist < minDist && dist > 0)
            {
                Vector3 normal = toBlock / dist;
                float overlap = minDist - dist;
                b.go.transform.position += normal * overlap;
                
                float velDotN = Vector3.Dot(b.vel, normal);
                if (velDotN < 0)
                {
                    b.vel -= normal * velDotN * (1f + restitution);
                    b.vel *= friction;
                    b.angularVel += Vector3.Cross(normal, b.vel) * 0.2f;
                }
            }
        }

        foreach (var b in blocks)
        {
            Vector3 pos = b.go.transform.position;
            
            if (pos.y - b.radius < groundY)
            {
                pos.y = groundY + b.radius;
                b.vel.y = -b.vel.y * restitution;
                b.vel.x *= friction;
                b.vel.z *= friction;
                b.angularVel *= friction;
                b.go.transform.position = pos;
            }
        }

        for (int i = 0; i < blocks.Count; i++)
        {
            for (int j = i + 1; j < blocks.Count; j++)
            {
                var A = blocks[i];
                var B = blocks[j];
                
                Vector3 dir = B.go.transform.position - A.go.transform.position;
                float dist = dir.magnitude;
                float minDist = A.radius + B.radius;
                
                if (dist < minDist && dist > 0)
                {
                    Vector3 n = dir / dist;
                    float overlap = (minDist - dist) * 0.5f;
                    
                    A.go.transform.position -= n * overlap;
                    B.go.transform.position += n * overlap;
                    
                    Vector3 relVel = B.vel - A.vel;
                    float impulseMag = Vector3.Dot(relVel, n);
                    
                    if (impulseMag < 0)
                    {
                        Vector3 impulse = -impulseMag * n * 0.2f;
                        A.vel -= impulse;
                        B.vel += impulse;
                    }
                }
            }
        }

        foreach (var b in blocks)
        {
            b.go.transform.position += b.vel * dt;
            
            if (b.angularVel.magnitude > 0.01f)
            {
                Quaternion deltaRot = Quaternion.Euler(b.angularVel * Mathf.Rad2Deg * dt);
                b.go.transform.rotation = deltaRot * b.go.transform.rotation;
            }
            
            b.vel *= damping;
            b.angularVel *= damping;
        }
    }

    public bool IsActive()
    {
        return initialized;
    }

    void Update()
    {
        StepSimulation();
    }

    void OnDrawGizmos()
    {
        if (!initialized || constraints == null) return;
        
        Gizmos.color = Color.cyan;
        foreach (var c in constraints)
        {
            if (c.broken) continue;
            if (blocks.Count <= c.a || blocks.Count <= c.b) continue;
            Gizmos.DrawLine(blocks[c.a].go.transform.position, blocks[c.b].go.transform.position);
        }
    }
}
