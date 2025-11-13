using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Smooth cube fracture: A large cube made of many smaller colored cubes.
/// Falls slowly and gracefully shatters when touching the ground.
/// Inspired by smooth slow-motion fracture animations.
/// </summary>
public class CubeFractureExample : MonoBehaviour, IPhysicsExample
{
    private class Block
    {
        public GameObject go;
        public Vector3 vel;
        public Vector3 angularVel;
        public float mass = 1f;
        public float radius;
        public Vector3 originalPos;
    }

    private class Constraint
    {
        public int a, b;
        public float rest;
        public float stiffness;
        public bool broken;
    }

    private List<Block> blocks = new List<Block>();
    private List<Constraint> constraints = new List<Constraint>();
    
    private Vector3 centerPos;
    private float groundY;
    private float dt;
    private Vector3 gravity;
    private bool initialized = false;
    private bool hasImpacted = false;

    [Header("Cube Parameters")]
    public int cubeSize = 4;
    public float blockSize = 0.4f;
    public float blockSpacing = 0.42f;
    public float stiffness = 30f; // ULTRA low for super smooth break
    public float breakThreshold = 0.25f; // Lower threshold - breaks easily on touch
    public float damping = 0.9999f; // MAXIMUM damping for super smooth motion
    public float alpha = 0.08f; // ULTRA gentle energy transfer - barely any violence
    public float restitution = 0.02f; // Almost no bounce at all
    public float friction = 0.995f; // Maximum friction

    public void Initialize(Vector3 position, float groundY, float dt, Vector3 gravity)
    {
        this.centerPos = position;
        this.groundY = groundY;
        this.dt = dt;
        this.gravity = gravity;
        this.initialized = true;
        
        CreateCube();
    }

    void CreateCube()
    {
        // Create a large cube made of smaller cubes with beautiful colors
        Vector3 offset = new Vector3(
            -(cubeSize - 1) * blockSpacing * 0.5f,
            0,
            -(cubeSize - 1) * blockSpacing * 0.5f
        );

        for (int x = 0; x < cubeSize; x++)
        {
            for (int y = 0; y < cubeSize; y++)
            {
                for (int z = 0; z < cubeSize; z++)
                {
                    Vector3 localPos = new Vector3(x * blockSpacing, y * blockSpacing, z * blockSpacing);
                    Vector3 pos = centerPos + offset + localPos;
                    
                    var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    block.transform.position = pos;
                    block.transform.localScale = Vector3.one * blockSize;
                    block.name = $"CubeBlock_{x}_{y}_{z}";
                    Destroy(block.GetComponent<Collider>());
                    
                    var rend = block.GetComponent<MeshRenderer>();
                    rend.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    
                    // Beautiful gradient colors based on position
                    float hue = (x / (float)cubeSize + y / (float)cubeSize + z / (float)cubeSize) / 3f;
                    rend.material.color = Color.HSVToRGB(hue, 0.7f, 0.9f);
                    
                    blocks.Add(new Block
                    {
                        go = block,
                        vel = Vector3.zero,
                        angularVel = Vector3.zero,
                        mass = 1f,
                        radius = blockSize * 0.5f,
                        originalPos = localPos
                    });
                }
            }
        }

        // Connect adjacent blocks (not diagonals)
        for (int i = 0; i < blocks.Count; i++)
        {
            for (int j = i + 1; j < blocks.Count; j++)
            {
                float dist = Vector3.Distance(blocks[i].go.transform.position, blocks[j].go.transform.position);
                
                // Only connect immediate neighbors
                if (dist < blockSpacing * 1.1f)
                {
                    constraints.Add(new Constraint
                    {
                        a = i,
                        b = j,
                        rest = dist,
                        stiffness = stiffness,
                        broken = false
                    });
                }
            }
        }

        Debug.Log($"Smooth Cube created with {blocks.Count} blocks and {constraints.Count} constraints");
    }

    public void StepSimulation()
    {
        if (!initialized) return;

        // Apply gentle gravity
        foreach (var b in blocks)
            b.vel += gravity * dt;

        // Spring constraints
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
            
            // Check for graceful fracture
            if (hasImpacted && Mathf.Abs(extension) > breakThreshold * c.rest)
            {
                c.broken = true;
                
                // ULTRA gentle energy release - barely any impulse
                float E = 0.5f * c.stiffness * extension * extension;
                float dV = Mathf.Sqrt(2f * alpha * E / a.mass);
                
                // Apply MINIMAL impulse with tiny randomness for natural look
                Vector3 randomDir = Random.insideUnitSphere * 0.05f;
                a.vel += (-n + randomDir) * dV * 0.1f;
                b.vel += (n + randomDir) * dV * 0.1f;
                
                // BARELY any rotation for ultra slow tumbling
                a.angularVel += Vector3.Cross(n, Random.onUnitSphere) * dV * 0.02f;
                b.angularVel += Vector3.Cross(-n, Random.onUnitSphere) * dV * 0.02f;
                
                // Flash brighter color on break for visual feedback
                var rendA = a.go.GetComponent<MeshRenderer>();
                var rendB = b.go.GetComponent<MeshRenderer>();
                if (rendA != null) rendA.material.color = Color.Lerp(rendA.material.color, Color.white, 0.3f);
                if (rendB != null) rendB.material.color = Color.Lerp(rendB.material.color, Color.white, 0.3f);
            }
        }

        // Ground collision
        if (!hasImpacted)
        {
            foreach (var b in blocks)
            {
                if (b.go.transform.position.y - b.radius <= groundY + 0.5f)
                {
                    hasImpacted = true;
                    Debug.Log("✨ Smooth cube impact - graceful fracture beginning");
                    break;
                }
            }
        }

        // Ground collision
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

        // Gentle block-to-block collisions
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
                        Vector3 impulse = -impulseMag * n * 0.1f; // ULTRA gentle collision response
                        A.vel -= impulse;
                        B.vel += impulse;
                    }
                }
            }
        }

        // Smooth motion integration
        foreach (var b in blocks)
        {
            b.go.transform.position += b.vel * dt;
            
            if (b.angularVel.magnitude > 0.01f)
            {
                Quaternion deltaRot = Quaternion.Euler(b.angularVel * Mathf.Rad2Deg * dt);
                b.go.transform.rotation = deltaRot * b.go.transform.rotation;
            }
            
            // High damping for smooth motion
            b.vel *= damping;
            b.angularVel *= damping;
        }
    }

    public bool IsActive()
    {
        return initialized;
    }

    void OnDrawGizmos()
    {
        if (!initialized || constraints == null) return;
        
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.5f);
        foreach (var c in constraints)
        {
            if (c.broken) continue;
            if (blocks.Count <= c.a || blocks.Count <= c.b) continue;
            Gizmos.DrawLine(blocks[c.a].go.transform.position, blocks[c.b].go.transform.position);
        }
    }
}
