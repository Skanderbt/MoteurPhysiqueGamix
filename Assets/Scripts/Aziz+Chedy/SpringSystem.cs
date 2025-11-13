using UnityEngine;
using UnityEngine.InputSystem;

public class SpringSystem : MonoBehaviour
{
    // Mode selection
    public enum PhysicsMode { Spring, Chain, Cloth }
    public PhysicsMode currentMode = PhysicsMode.Spring;

    // Spring parameters
    public float k = 120f;
    public float c = 8f;
    public float L0 = 3f;

    // Chain parameters
    public float chainLength = 3f;
    public float chainDamping = 5f;

    // Common parameters
    public float mass = 2f;
    public float initialCompression = 1.5f;

    private Transform fixedPoint;
    private GameObject redDot;
    private CustomRigidBody body;
    private Spring spring;
    private Chain chain;

    // Physics tracking
    private float totalEnergy = 0f;
    private float equilibriumY = 0f;

    // Track parameter changes
    private float lastK, lastC, lastL0, lastMass;
    private float lastChainLength, lastChainDamping;
    private float lastInitialCompression;

    void Start()
    {
        CreateScene();
    }

    void CreateScene()
    {
        // Ceiling
        GameObject ceiling = new GameObject("Ceiling");
        ceiling.transform.position = new Vector3(0, 6f, 0);
        fixedPoint = ceiling.transform;

        // Red Dot
        redDot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        redDot.name = "RedDot";
        redDot.transform.position = fixedPoint.position;
        redDot.transform.localScale = Vector3.one * 0.3f;
        redDot.transform.SetParent(fixedPoint);
        var mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = Color.red;
        redDot.GetComponent<Renderer>().material = mat;
        Destroy(redDot.GetComponent<SphereCollider>());

        // Mass
        GameObject cubeGO = new GameObject("Mass");
        body = cubeGO.AddComponent<CustomRigidBody>();
        body.mass = mass;
        body.size = new Vector3(1f, 1f, 1f);

        // Cube Y calculation - spring should be compressedLength long
        // Spring goes from fixedPoint to top of cube (attachmentPoint)
        // attachmentPoint = cubeCenter + size.y/2
        // We want: fixedPoint.y - attachmentPoint.y = compressedLength
        // So: cubeCenter.y = fixedPoint.y - compressedLength - size.y/2
        float compressedLength = L0 - initialCompression;
        float cubeY = fixedPoint.position.y - compressedLength - body.size.y * 0.5f;

        body.position = new Vector3(0, cubeY, 0);
        cubeGO.transform.position = body.position;

        // Create both spring and chain, enable based on mode
        GameObject constraintGO = new GameObject(currentMode == PhysicsMode.Spring ? "Spring" : "Chain");

        if (currentMode == PhysicsMode.Spring)
        {
            spring = constraintGO.AddComponent<Spring>();
            spring.fixedEnd = fixedPoint;
            spring.body = body;
            spring.Set(k, c, L0);
            spring.GetComponent<LineRenderer>().useWorldSpace = true;
        }
        else
        {
            chain = constraintGO.AddComponent<Chain>();
            chain.fixedEnd = fixedPoint;
            chain.body = body;
            chain.Set(chainLength, chainDamping);
            chain.GetComponent<LineRenderer>().useWorldSpace = true;
        }

        // Calculate equilibrium position: where spring force = gravity force
        // k * (L_eq - L0) = m * g
        // L_eq = L0 + (m * g) / k
        float equilibriumSpringLength = L0 + (mass * body.gravity) / k;
        equilibriumY = fixedPoint.position.y - equilibriumSpringLength - body.size.y * 0.5f;

        Debug.Log($"Cube center Y: {cubeY:F2}");
        Debug.Log($"Top of cube: {body.AttachmentPoint.y:F2}");
        Debug.Log($"Red dot Y: {fixedPoint.position.y:F2}");
        Debug.Log($"Spring length: {Vector3.Distance(fixedPoint.position, body.AttachmentPoint):F2}");
        Debug.Log($"Equilibrium Y (calculated): {equilibriumY:F2} at spring length {equilibriumSpringLength:F2}");

        // Initialize tracking
        lastK = k; lastC = c; lastL0 = L0; lastMass = mass;
        lastChainLength = chainLength; lastChainDamping = chainDamping;
        lastInitialCompression = initialCompression;
    }

    void Update()
    {
        // Check for parameter changes in SPRING mode
        if (currentMode == PhysicsMode.Spring)
        {
            if (Mathf.Abs(k - lastK) > 0.01f || Mathf.Abs(c - lastC) > 0.01f ||
                Mathf.Abs(L0 - lastL0) > 0.01f || Mathf.Abs(mass - lastMass) > 0.01f ||
                Mathf.Abs(initialCompression - lastInitialCompression) > 0.01f)
            {
                Debug.Log($"<color=yellow>SPRING PARAMETER CHANGED - RESETTING: k={k:F1}, c={c:F1}, L0={L0:F2}, mass={mass:F2}, compression={initialCompression:F2}</color>");
                lastK = k; lastC = c; lastL0 = L0; lastMass = mass;
                lastInitialCompression = initialCompression;
                Reset();
                return;
            }
        }
        // Check for parameter changes in CHAIN mode
        else if (currentMode == PhysicsMode.Chain)
        {
            if (Mathf.Abs(chainLength - lastChainLength) > 0.01f ||
                Mathf.Abs(chainDamping - lastChainDamping) > 0.01f ||
                Mathf.Abs(mass - lastMass) > 0.01f)
            {
                Debug.Log($"<color=orange>CHAIN PARAMETER CHANGED - RESETTING: length={chainLength:F2}, damp={chainDamping:F1}, mass={mass:F2}</color>");
                lastChainLength = chainLength;
                lastChainDamping = chainDamping;
                lastMass = mass;
                Reset();
                return;
            }
        }

        // Debug current state every frame
        if (body && fixedPoint && Time.frameCount % 30 == 0) // Every 30 frames
        {
            float currentSpringLength = Vector3.Distance(fixedPoint.position, body.AttachmentPoint);
            float displacement = currentSpringLength - L0;
            float springForce = -k * displacement;
            float gravityForce = -mass * body.gravity;
            float netForce = springForce + gravityForce;

            Debug.Log($"[Frame {Time.frameCount}] Y={body.position.y:F3}, Vel={body.velocity.y:F3}, " +
                      $"SpringLen={currentSpringLength:F3}, Disp={displacement:F3}, " +
                      $"SpringF={springForce:F1}N, GravF={gravityForce:F1}N, NetF={netForce:F1}N");
        }

        if (Keyboard.current.rKey.wasPressedThisFrame) Reset();
    }

    void Reset()
    {
        // Destroy using stored references instead of GameObject.Find()
        if (body != null)
        {
            Destroy(body.gameObject);
            body = null;
        }
        
        if (spring != null)
        {
            Destroy(spring.gameObject);
            spring = null;
        }
        
        if (chain != null)
        {
            Destroy(chain.gameObject);
            chain = null;
        }
        
        if (fixedPoint != null)
        {
            Destroy(fixedPoint.gameObject); // This destroys ceiling and red dot
            fixedPoint = null;
        }
        
        redDot = null; // Red dot is child of ceiling, so it's already destroyed
        
        CreateScene();
    }

    void SwitchMode()
    {
        if (currentMode == PhysicsMode.Spring)
            currentMode = PhysicsMode.Chain;
        else if (currentMode == PhysicsMode.Chain)
            currentMode = PhysicsMode.Spring;
        
        Debug.Log($"<color=green>Switched to {currentMode} mode!</color>");
        Reset();
    }

    void SwitchToCloth()
    {
        // Clean up current scene using stored references
        if (body != null) Destroy(body.gameObject);
        if (spring != null) Destroy(spring.gameObject);
        if (chain != null) Destroy(chain.gameObject);
        if (fixedPoint != null) Destroy(fixedPoint.gameObject);
        
        // Create cloth simulation
        GameObject clothGO = new GameObject("ClothSystem");
        clothGO.AddComponent<ClothSimulation>();
        
        // Disable this SpringSystem after cleanup
        this.enabled = false;
        
        Debug.Log("<color=cyan>Switched to Cloth Simulation!</color>");
    }

    void OnGUI()
    {
        // Scale up the UI
        float scale = 2.0f;
        Matrix4x4 oldMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

        GUILayout.BeginArea(new Rect(10, 10, 320, 450));

        // Larger font style
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 18;
        titleStyle.fontStyle = FontStyle.Bold;

        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 14;

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 14;

        GUIStyle modeButtonStyle = new GUIStyle(GUI.skin.button);
        modeButtonStyle.fontSize = 16;
        modeButtonStyle.fontStyle = FontStyle.Bold;

        string modeTitle = currentMode == PhysicsMode.Spring ? "SPRING SIMULATION" : "CHAIN SIMULATION";
        GUILayout.Label($"<b>{modeTitle}</b>", titleStyle);
        GUILayout.Space(5);

        // Mode switch button
        string buttonText = currentMode == PhysicsMode.Spring ? "Switch to CHAIN →" : "Switch to SPRING →";
        if (GUILayout.Button(buttonText, modeButtonStyle, GUILayout.Height(35)))
        {
            SwitchMode();
        }
        GUILayout.Space(5);
        
        // Cloth button
        if (GUILayout.Button("CLOTH SIMULATION →", modeButtonStyle, GUILayout.Height(35)))
        {
            SwitchToCloth();
        }
        GUILayout.Space(10);

        // Mode-specific parameters
        if (currentMode == PhysicsMode.Spring)
        {
            k = LabelSlider("Stiffness (k)", k, 10, 500, labelStyle);
            c = LabelSlider("Damping (c)", c, 0, 50, labelStyle);
            L0 = LabelSlider("Rest Length (L0)", L0, 1, 6, labelStyle);
            initialCompression = LabelSlider("Compression", initialCompression, 0, L0, labelStyle);
        }
        else
        {
            chainLength = LabelSlider("Chain Length", chainLength, 1, 6, labelStyle);
            chainDamping = LabelSlider("Damping", chainDamping, 0, 20, labelStyle);
        }

        mass = LabelSlider("Mass (m)", mass, 0.5f, 10, labelStyle);

        GUILayout.Space(5);
        if (GUILayout.Button("RESET", buttonStyle, GUILayout.Height(30))) Reset();

        if (body)
        {
            float currentLength = Vector3.Distance(fixedPoint.position, body.AttachmentPoint);

            GUILayout.Space(10);
            GUILayout.Label("━━━ STATE ━━━", titleStyle);
            GUILayout.Label($"Position Y: {body.position.y:F2} m", labelStyle);
            GUILayout.Label($"Velocity Y: {body.velocity.y:F2} m/s", labelStyle);

            if (currentMode == PhysicsMode.Spring)
            {
                float displacement = currentLength - L0;
                float springPE = 0.5f * k * displacement * displacement;
                float gravityPE = mass * body.gravity * body.position.y;
                float kineticE = 0.5f * mass * body.velocity.sqrMagnitude;
                totalEnergy = springPE + gravityPE + kineticE;

                GUILayout.Label($"Spring Length: {currentLength:F2} m", labelStyle);
                GUILayout.Label($"Displacement: {displacement:F2} m", labelStyle);
                GUILayout.Label($"Equilibrium Y: {equilibriumY:F2} m", labelStyle);

                GUILayout.Space(10);
                GUILayout.Label("━━━ ENERGY ━━━", titleStyle);
                GUILayout.Label($"Spring PE: {springPE:F1} J", labelStyle);
                GUILayout.Label($"Gravity PE: {gravityPE:F1} J", labelStyle);
                GUILayout.Label($"Kinetic E: {kineticE:F1} J", labelStyle);
                GUILayout.Label($"Total E: {totalEnergy:F1} J", labelStyle);
            }
            else
            {
                float gravityPE = mass * body.gravity * body.position.y;
                float kineticE = 0.5f * mass * body.velocity.sqrMagnitude;
                totalEnergy = gravityPE + kineticE;

                GUILayout.Label($"Chain Length: {currentLength:F2} m", labelStyle);
                GUILayout.Label($"Max Length: {chainLength:F2} m", labelStyle);
                bool isTaut = currentLength >= chainLength;
                GUILayout.Label($"Status: {(isTaut ? "TAUT ⚡" : "SLACK ~")}", labelStyle);

                GUILayout.Space(10);
                GUILayout.Label("━━━ ENERGY ━━━", titleStyle);
                GUILayout.Label($"Gravity PE: {gravityPE:F1} J", labelStyle);
                GUILayout.Label($"Kinetic E: {kineticE:F1} J", labelStyle);
                GUILayout.Label($"Total E: {totalEnergy:F1} J", labelStyle);
            }
        }
        GUILayout.EndArea();

        // Restore original matrix
        GUI.matrix = oldMatrix;
    }

    float LabelSlider(string label, float val, float min, float max, GUIStyle labelStyle)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label + ":", labelStyle, GUILayout.Width(150));
        val = GUILayout.HorizontalSlider(val, min, max, GUILayout.Width(100));
        GUILayout.Label(val.ToString("F1"), labelStyle, GUILayout.Width(50));
        GUILayout.EndHorizontal();
        return val;
    }
}