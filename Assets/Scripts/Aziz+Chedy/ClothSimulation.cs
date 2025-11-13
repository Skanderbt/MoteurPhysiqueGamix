using UnityEngine;
using UnityEngine.InputSystem;

public class ClothSimulation : MonoBehaviour
{
    [Header("Cloth Grid")]
    public int width = 20;  // Number of particles in width
    public int height = 20; // Number of particles in height
    public float spacing = 0.2f; // Distance between particles

    [Header("Physics")]
    public float particleMass = 0.05f;
    public float gravity = 9.8f;
    public float maxVelocity = 10f;
    public int constraintIterations = 3;
    
    [Header("Spring Forces")]
    public float structuralStiffness = 50f;  // Structural springs (adjacent particles)
    public float shearStiffness = 25f;       // Shear springs (diagonal)
    public float bendStiffness = 15f;        // Bend springs (skip one particle)
    public float damping = 0.5f;



    [Header("Environment")]
    public float airResistance = 0.02f;
    public Vector3 windForce = Vector3.zero;
    public bool enableWind = false;

    private ClothParticle[,] particles;
    private GameObject clothMesh;
    private Mesh mesh;
    private Vector3[] vertices;
    private int[] triangles;

    private float lastStructuralStiffness, lastShearStiffness, lastBendStiffness;
    private float lastDamping, lastParticleMass, lastSpacing;

    void Start()
    {
        CreateCloth();
        InitializeLastParameters();
    }

    void CreateCloth()
    {
        particles = new ClothParticle[width, height];

        // Calculate starting position to center the cloth
        float totalWidth = (width - 1) * spacing;
        float startX = -totalWidth / 2f;
        float startY = 5f; // Start high above the ball
        float startZ = 0f;

        // Create particles
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 pos = new Vector3(
                    startX + x * spacing,
                    startY,
                    startZ + y * spacing
                );
                particles[x, y] = new ClothParticle(pos, particleMass);

                // Pin the top row
                if (y == height - 1)
                {
                    particles[x, y].isPinned = true;
                }
            }
        }

        CreateClothMesh();
    }

    void CreateClothMesh()
    {
        if (clothMesh == null)
        {
            clothMesh = new GameObject("ClothMesh");
            clothMesh.AddComponent<MeshFilter>();
            clothMesh.AddComponent<MeshRenderer>();
        }
        
        // Always create a new material to ensure color updates
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(1f, 1f, 1f, 0.9f); // White semi-transparent
        mat.SetFloat("_Mode", 3); // Transparent
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
        
        clothMesh.GetComponent<MeshRenderer>().material = mat;

        mesh = new Mesh();
        clothMesh.GetComponent<MeshFilter>().mesh = mesh;

        // Create vertices array
        vertices = new Vector3[width * height];
        UpdateMeshVertices();

        // Create triangles
        triangles = new int[(width - 1) * (height - 1) * 6];
        int triIndex = 0;
        for (int x = 0; x < width - 1; x++)
        {
            for (int y = 0; y < height - 1; y++)
            {
                int i = x * height + y;
                // Triangle 1
                triangles[triIndex++] = i;
                triangles[triIndex++] = i + height;
                triangles[triIndex++] = i + 1;
                // Triangle 2
                triangles[triIndex++] = i + 1;
                triangles[triIndex++] = i + height;
                triangles[triIndex++] = i + height + 1;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }



    void InitializeLastParameters()
    {
        lastStructuralStiffness = structuralStiffness;
        lastShearStiffness = shearStiffness;
        lastBendStiffness = bendStiffness;
        lastDamping = damping;
        lastParticleMass = particleMass;
        lastSpacing = spacing;
    }

    void Update()
    {
        CheckParameterChanges();
        
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            ResetCloth();
        }
        if (Keyboard.current.wKey.wasPressedThisFrame)
        {
            enableWind = !enableWind;
            windForce = enableWind ? new Vector3(2f, 0, 0) : Vector3.zero;
        }
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ReturnToSpringSystem();
        }
    }

    void CheckParameterChanges()
    {
        bool needsReset = false;

        // Check if any parameter changed and reset if so
        if (Mathf.Abs(structuralStiffness - lastStructuralStiffness) > 0.01f)
        {
            lastStructuralStiffness = structuralStiffness;
            needsReset = true;
        }
        if (Mathf.Abs(shearStiffness - lastShearStiffness) > 0.01f)
        {
            lastShearStiffness = shearStiffness;
            needsReset = true;
        }
        if (Mathf.Abs(bendStiffness - lastBendStiffness) > 0.01f)
        {
            lastBendStiffness = bendStiffness;
            needsReset = true;
        }
        if (Mathf.Abs(damping - lastDamping) > 0.01f)
        {
            lastDamping = damping;
            needsReset = true;
        }
        if (Mathf.Abs(particleMass - lastParticleMass) > 0.001f)
        {
            lastParticleMass = particleMass;
            needsReset = true;
        }
        if (Mathf.Abs(spacing - lastSpacing) > 0.01f)
        {
            lastSpacing = spacing;
            needsReset = true;
        }

        if (needsReset)
        {
            ResetCloth();
        }
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        // Apply forces
        ApplySprings();
        ApplyWind();
        ApplyAirResistance();

        // Integrate
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                particles[x, y].Integrate(dt, gravity);
                
                // Clamp velocity to prevent explosions
                if (particles[x, y].velocity.sqrMagnitude > maxVelocity * maxVelocity)
                {
                    particles[x, y].velocity = particles[x, y].velocity.normalized * maxVelocity;
                }
                
                particles[x, y].ApplyGroundCollision(0f, 0.1f, 0.9f);
            }
        }

        // Apply constraints multiple times for stability
        for (int iter = 0; iter < constraintIterations; iter++)
        {
            ApplyConstraints();
        }

        UpdateMeshVertices();
    }

    void ApplySprings()
    {
        // Structural springs (horizontal and vertical neighbors)
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Right neighbor
                if (x < width - 1)
                    ApplySpringForce(particles[x, y], particles[x + 1, y], spacing, structuralStiffness);
                // Down neighbor
                if (y > 0)
                    ApplySpringForce(particles[x, y], particles[x, y - 1], spacing, structuralStiffness);
            }
        }

        // Shear springs (diagonal neighbors)
        for (int x = 0; x < width - 1; x++)
        {
            for (int y = 0; y < height - 1; y++)
            {
                float diagonalDist = spacing * Mathf.Sqrt(2);
                ApplySpringForce(particles[x, y], particles[x + 1, y + 1], diagonalDist, shearStiffness);
                ApplySpringForce(particles[x + 1, y], particles[x, y + 1], diagonalDist, shearStiffness);
            }
        }

        // Bend springs (skip one particle)
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (x < width - 2)
                    ApplySpringForce(particles[x, y], particles[x + 2, y], spacing * 2, bendStiffness);
                if (y > 1)
                    ApplySpringForce(particles[x, y], particles[x, y - 2], spacing * 2, bendStiffness);
            }
        }
    }

    void ApplySpringForce(ClothParticle p1, ClothParticle p2, float restLength, float stiffness)
    {
        Vector3 delta = p2.position - p1.position;
        float currentLength = delta.magnitude;
        if (currentLength < 0.0001f) return;

        Vector3 direction = delta / currentLength;
        float displacement = currentLength - restLength;

        // Hooke's law
        Vector3 springForce = stiffness * displacement * direction;

        // Damping
        Vector3 relativeVelocity = p2.velocity - p1.velocity;
        Vector3 dampingForce = damping * Vector3.Dot(relativeVelocity, direction) * direction;

        Vector3 totalForce = springForce + dampingForce;

        p1.AddForce(totalForce);
        p2.AddForce(-totalForce);
    }

    void ApplyConstraints()
    {
        // Constrain structural springs to prevent stretching
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Right neighbor
                if (x < width - 1)
                    SatisfyConstraint(particles[x, y], particles[x + 1, y], spacing);
                // Down neighbor
                if (y > 0)
                    SatisfyConstraint(particles[x, y], particles[x, y - 1], spacing);
            }
        }
    }

    void SatisfyConstraint(ClothParticle p1, ClothParticle p2, float restLength)
    {
        Vector3 delta = p2.position - p1.position;
        float currentLength = delta.magnitude;
        if (currentLength < 0.0001f) return;

        float difference = (currentLength - restLength) / currentLength;
        Vector3 correction = delta * 0.5f * difference;

        if (!p1.isPinned)
            p1.position += correction;
        if (!p2.isPinned)
            p2.position -= correction;
    }

    void ApplyWind()
    {
        if (!enableWind) return;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                particles[x, y].AddForce(windForce * particleMass);
            }
        }
    }

    void ApplyAirResistance()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 drag = -airResistance * particles[x, y].velocity;
                particles[x, y].AddForce(drag);
            }
        }
    }



    void UpdateMeshVertices()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 pos = particles[x, y].position;
                
                // Check for invalid values
                if (float.IsNaN(pos.x) || float.IsNaN(pos.y) || float.IsNaN(pos.z) ||
                    float.IsInfinity(pos.x) || float.IsInfinity(pos.y) || float.IsInfinity(pos.z))
                {
                    Debug.LogError("Invalid particle position detected! Resetting cloth.");
                    ResetCloth();
                    return;
                }
                
                vertices[x * height + y] = pos;
            }
        }
        mesh.vertices = vertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    void ResetCloth()
    {
        Destroy(clothMesh);
        clothMesh = null;
        CreateCloth();
        InitializeLastParameters();
    }

    void ReturnToSpringSystem()
    {
        // Clean up
        Destroy(clothMesh);
        Destroy(this.gameObject);
        
        // Re-enable SpringSystem
        SpringSystem springSystem = FindFirstObjectByType<SpringSystem>();
        if (springSystem != null)
        {
            springSystem.enabled = true;
        }
    }

    void OnGUI()
    {
        float scale = 1.0f;
        Matrix4x4 oldMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

        GUILayout.BeginArea(new Rect(10, 10, 380, 600));

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 18;
        titleStyle.fontStyle = FontStyle.Bold;

        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 14;

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 14;

        GUILayout.Label("<b>CLOTH SIMULATION</b>", titleStyle);
        GUILayout.Space(5);

        if (GUILayout.Button("← Back to Spring/Chain", buttonStyle, GUILayout.Height(30)))
        {
            ReturnToSpringSystem();
        }
        GUILayout.Space(10);

        GUILayout.Label("━━━ SPRING STIFFNESS ━━━", titleStyle);
        structuralStiffness = LabelSlider("Structural", structuralStiffness, 10, 200, labelStyle);
        shearStiffness = LabelSlider("Shear", shearStiffness, 5, 100, labelStyle);
        bendStiffness = LabelSlider("Bend", bendStiffness, 5, 50, labelStyle);
        damping = LabelSlider("Damping", damping, 0, 5, labelStyle);



        GUILayout.Space(10);
        GUILayout.Label("━━━ ENVIRONMENT ━━━", titleStyle);
        gravity = LabelSlider("Gravity", gravity, 0, 20, labelStyle);
        airResistance = LabelSlider("Air Resistance", airResistance, 0, 0.5f, labelStyle);

        GUILayout.Space(10);
        if (GUILayout.Button("RESET CLOTH (R)", buttonStyle, GUILayout.Height(30)))
        {
            ResetCloth();
        }
        
        string windStatus = enableWind ? "✓ Wind ON" : "✗ Wind OFF";
        GUILayout.Label($"{windStatus} (Press W)", labelStyle);
        
        GUILayout.Label($"Cloth: {width}x{height} = {width * height} particles", labelStyle);

        GUILayout.EndArea();
        GUI.matrix = oldMatrix;
    }

    float LabelSlider(string label, float val, float min, float max, GUIStyle labelStyle)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label + ":", labelStyle, GUILayout.Width(150));
        val = GUILayout.HorizontalSlider(val, min, max, GUILayout.Width(150));
        GUILayout.Label(val.ToString("F2"), labelStyle, GUILayout.Width(60));
        GUILayout.EndHorizontal();
        return val;
    }
}
