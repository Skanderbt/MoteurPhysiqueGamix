using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages all physics examples from the Energized Rigid Body Fracture paper
/// Recreates: Beam, Plate, Chain, and Bunny examples using pure code-based physics
/// </summary>
public class PhysicsExamplesManager : MonoBehaviour
{
    [Header("Example Spacing")]
    public float exampleSpacing = 30f;
    public float groundY = 0f;

    [Header("Physics Settings")]
    public float dt = 0.016f; // Smoother 60Hz
    public Vector3 gravity = new Vector3(0, -0.8f, 0); // SUPER slow gravity (was -3, now -0.8)
    
    private List<IPhysicsExample> examples = new List<IPhysicsExample>();

    void Start()
    {
        CreateGround();
        SpawnAllExamples();
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

    void SpawnAllExamples()
    {
        // ONLY Cube Fracture
        var cube = gameObject.AddComponent<CubeFractureExample>();
        cube.Initialize(new Vector3(0f, 22f, 0f), groundY, dt, gravity);
        CreateLabel("CUBE FRACTURE", new Vector3(0f, 28f, 0f));
        examples.Add(cube);
    }

    void CreateLabel(string text, Vector3 position)
    {
        var labelObj = new GameObject($"Label_{text}");
        labelObj.transform.position = position;
        
        // Create 3D text using TextMesh
        var textMesh = labelObj.AddComponent<TextMesh>();
        textMesh.text = text;
        textMesh.fontSize = 30;
        textMesh.color = Color.white;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.characterSize = 0.5f;
    }

    void Update()
    {
        foreach (var example in examples)
        {
            if (example != null && example.IsActive())
            {
                example.StepSimulation();
            }
        }
    }
}

/// <summary>
/// Interface for all physics examples
/// </summary>
public interface IPhysicsExample
{
    void Initialize(Vector3 position, float groundY, float dt, Vector3 gravity);
    void StepSimulation();
    bool IsActive();
}
