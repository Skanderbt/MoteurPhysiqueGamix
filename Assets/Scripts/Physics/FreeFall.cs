using UnityEngine;

public class FreeFall : MonoBehaviour
{
    [Header("Physical parameters")]
    public Vector3 velocity = Vector3.zero;
    public float gravity = 9.81f;

    [Header("Initial / Visual")]
    public Vector3 startPosition = new Vector3(0f, 5f, 0f);
    public Color cubeColor = Color.red;

    // Internals
    private Vector3 position;
    private CubeObject cubeObject;
    private float timer = 0f;

    void Start()
    {
        // Initialisation
        position = startPosition;
        velocity = Vector3.zero;

        // Créer le cube
        cubeObject = new CubeObject(position, cubeColor, "FreeFallCube");

        Debug.Log($"FreeFall started at position: {position}");
    }

    void Update()
    {
        float dt = Time.deltaTime;
        timer += dt;

        // Intégration d'Euler explicite
        velocity += Vector3.down * gravity * dt;
        position += velocity * dt;

        // Collision simple avec le plan y = 0
        if (position.y <= 0f)
        {
            position.y = 0f;
            velocity = Vector3.zero;
        }

        // Appliquer la position directement au transform pour test
        if (cubeObject != null && cubeObject.cube != null)
        {
            cubeObject.cube.transform.position = position;
        }

        // Debug info
        if (timer > 1f)
        {
            Debug.Log($"Cube position: {position}, velocity: {velocity}");
            timer = 0f;
        }
    }

    // Optionnel: nettoyer
    private void OnDestroy()
    {
        if (cubeObject != null) cubeObject.Destroy();
    }
}