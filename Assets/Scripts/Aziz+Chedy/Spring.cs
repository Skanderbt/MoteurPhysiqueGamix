using UnityEngine;

/// <summary>
/// Realistic spring physics implementation using Hooke's Law and damping.
/// Spring force: F = -k * (x - x0) where x is current length, x0 is rest length
/// Damping force: F = -c * v where v is velocity along spring direction
/// The spring applies forces in both compression and extension (realistic behavior)
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class Spring : MonoBehaviour
{
    public Transform fixedEnd;
    public CustomRigidBody body;
    public float restLength = 3f;
    public float k = 120f;
    public float c = 8f;
    public float airDrag = 0.1f; // Air resistance coefficient

    private LineRenderer lr;
    private const int segments = 40;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.positionCount = segments + 1;
        lr.startWidth = lr.endWidth = 0.08f;
        lr.material = new Material(Shader.Find("Unlit/Color"));
        lr.material.color = Color.yellow;
        lr.useWorldSpace = true; // FIXED HERE
    }

    void FixedUpdate()
    {
        if (!body || !fixedEnd) return;

        Vector3 A = fixedEnd.position;
        Vector3 B = body.AttachmentPoint;
        Vector3 delta = B - A;
        float len = delta.magnitude;
        if (len < 0.01f) return;

        Vector3 dir = delta / len;
        
        // Calculate spring displacement (positive = stretched, negative = compressed)
        float displacement = len - restLength;
        
        // PHYSICAL CONSTRAINT: Check if mass is above the fixed point
        // If mass goes above red dot, spring goes SLACK (provides no force)
        bool massIsAboveFixedPoint = B.y > A.y;
        
        Vector3 springF = Vector3.zero;
        Vector3 dampF = Vector3.zero;
        
        if (!massIsAboveFixedPoint)
        {
            // Normal operation: spring can pull (tension) or push (compression)
            // Spring force: F = -k * displacement
            springF = -k * displacement * dir;
            
            // Damping force: opposes velocity along spring direction
            Vector3 relVel = body.velocity;
            float velAlongSpring = Vector3.Dot(relVel, dir);
            dampF = -c * velAlongSpring * dir;
        }
        else
        {
            // Spring is SLACK - mass has gone above fixed point
            // In reality, the spring would be loose and provide NO force
            // Only gravity acts on the mass
            if (Time.frameCount % 60 == 0)
            {
                Debug.Log($"<color=red>[SPRING SLACK] Mass above fixed point! No spring force applied.</color>");
            }
        }
        
        // Air drag: always opposes velocity (independent of spring)
        Vector3 dragF = -airDrag * body.velocity.magnitude * body.velocity;

        // Debug forces periodically
        if (Time.frameCount % 60 == 0 && !massIsAboveFixedPoint)
        {
            Debug.Log($"<color=cyan>[SPRING] Len={len:F3}, Disp={displacement:F3}, " +
                      $"SpringF={springF.y:F1}N, DampF={dampF.y:F1}N, DragF={dragF.y:F1}N, " +
                      $"Vel={body.velocity.y:F3}</color>");
        }

        // Apply all forces
        body.AddForce(springF + dampF + dragF);

        DrawHelix(A, B, len);
    }

    void DrawHelix(Vector3 start, Vector3 end, float length)
    {
        Vector3 axis = (end - start) / length;
        float radius = 0.2f;
        int coils = 8;
        float step = coils * 2f * Mathf.PI / segments;

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float angle = i * step;
            Vector3 center = Vector3.Lerp(start, end, t);
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
            Quaternion rot = Quaternion.FromToRotation(Vector3.up, axis);
            lr.SetPosition(i, center + rot * offset);
        }
    }

    public void Set(float stiffness, float damping, float rest)
    {
        k = stiffness; c = damping; restLength = rest;
    }
}