using UnityEngine;

/// <summary>
/// Chain physics - tension-only constraint.
/// Unlike springs, chains cannot push (no compression force).
/// They only pull when stretched beyond their natural length.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class Chain : MonoBehaviour
{
    public Transform fixedEnd;
    public CustomRigidBody body;
    public float maxLength = 3f;
    public float damping = 5f;

    private LineRenderer lr;
    private const int segments = 20;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.positionCount = segments + 1;
        lr.startWidth = lr.endWidth = 0.05f;
        lr.material = new Material(Shader.Find("Unlit/Color"));
        lr.material.color = new Color(0.4f, 0.3f, 0.2f); // Brown chain color
        lr.useWorldSpace = true;
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

        // CRITICAL: Chain can only PULL, never PUSH
        // Only apply force if chain is stretched beyond max length
        bool isTaut = len > maxLength;
        bool massIsAboveFixedPoint = B.y > A.y;

        Vector3 chainForce = Vector3.zero;
        Vector3 dampF = Vector3.zero;

        if (isTaut && !massIsAboveFixedPoint)
        {
            // Chain is stretched - apply tension force
            float extension = len - maxLength;
            
            // Very high stiffness to simulate inextensible chain
            float k = 5000f;
            chainForce = -k * extension * dir;

            // Damping to prevent jitter
            Vector3 relVel = body.velocity;
            float velAlongChain = Vector3.Dot(relVel, dir);
            dampF = -damping * velAlongChain * dir;
        }
        else if (massIsAboveFixedPoint)
        {
            // Chain is slack - mass above attachment point
            if (Time.frameCount % 60 == 0)
            {
                Debug.Log($"<color=magenta>[CHAIN SLACK] Mass above fixed point!</color>");
            }
        }
        else
        {
            // Chain is slack but mass is below - just hanging loose
            if (Time.frameCount % 120 == 0)
            {
                Debug.Log($"<color=cyan>[CHAIN LOOSE] Length={len:F2}, Max={maxLength:F2}</color>");
            }
        }

        // Apply forces
        body.AddForce(chainForce + dampF);

        // Draw chain as a catenary curve (or straight if taut)
        DrawChain(A, B, len);
    }

    void DrawChain(Vector3 start, Vector3 end, float length)
    {
        // If chain is slack, draw a catenary (hanging curve)
        // If taut, draw straight line
        
        bool isTaut = length >= maxLength * 0.98f;
        
        if (isTaut)
        {
            // Straight line when taut
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                lr.SetPosition(i, Vector3.Lerp(start, end, t));
            }
        }
        else
        {
            // Catenary curve when slack
            float slack = maxLength - length;
            float sag = Mathf.Min(slack * 0.5f, 1f);
            
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                Vector3 point = Vector3.Lerp(start, end, t);
                
                // Add parabolic sag
                float sagAmount = 4f * sag * t * (1f - t);
                point.y -= sagAmount;
                
                lr.SetPosition(i, point);
            }
        }
    }

    public void Set(float length, float damp)
    {
        maxLength = length;
        damping = damp;
    }
}
