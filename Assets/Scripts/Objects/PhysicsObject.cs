using UnityEngine;
using System.Collections.Generic;

public class PhysicsObject : MonoBehaviour
{
    [Header("Physics Properties")]
    public float mass = 1.0f;
    public Vector3 velocity = Vector3.zero;
    public Vector3 acceleration = Vector3.zero;
    public bool useGravity = true;
    public float bounciness = 0.7f;
    public float friction = 0.1f;

    [Header("Collision Settings")]
    public bool enableCollisions = true;

    protected Bounds bounds;
    protected Vector3 size = Vector3.one;

    void Start()
    {
        InitializeBounds();
    }

    void FixedUpdate()
    {
        if (Time.fixedDeltaTime <= 0) return;

        ApplyForces();
        Integrate(Time.fixedDeltaTime);

        if (enableCollisions)
        {
            CheckGroundCollision();
            CheckObjectCollisions();
        }
    }

    protected virtual void InitializeBounds()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            bounds = renderer.bounds;
            size = bounds.size;
        }
        else
        {
            bounds = new Bounds(transform.position, size);
        }
    }

    protected virtual void ApplyForces()
    {
        Vector3 totalForce = Vector3.zero;

        // Gravity
        if (useGravity)
        {
            totalForce += new Vector3(0, -9.81f, 0) * mass;
        }

        acceleration = totalForce / mass;
    }

    protected virtual void Integrate(float deltaTime)
    {
        velocity += acceleration * deltaTime;
        transform.position += velocity * deltaTime;
        UpdateBounds();
    }

    protected virtual void UpdateBounds()
    {
        bounds.center = transform.position;
    }

    protected virtual void CheckGroundCollision()
    {
        float groundLevel = 0f;
        float bottom = bounds.min.y;

        if (bottom <= groundLevel && velocity.y < 0)
        {
            // Collision avec le sol
            transform.position = new Vector3(
                transform.position.x,
                groundLevel + size.y / 2,
                transform.position.z
            );

            // Appliquer la restitution (effet élastique)
            velocity.y = -velocity.y * bounciness;

            // Appliquer le frottement
            velocity.x *= (1f - friction);
            velocity.z *= (1f - friction);

            Debug.Log($"Ground collision! New velocity: {velocity}");
        }
    }

    protected virtual void CheckObjectCollisions()
    {
        // CORRECTION: Utiliser FindObjectsByType au lieu de FindObjectsOfType
        PhysicsObject[] allObjects = FindObjectsByType<PhysicsObject>(FindObjectsSortMode.None);

        foreach (PhysicsObject other in allObjects)
        {
            if (other != this && other.enableCollisions)
            {
                if (bounds.Intersects(other.bounds))
                {
                    ResolveCollision(other);
                }
            }
        }
    }

    protected virtual void ResolveCollision(PhysicsObject other)
    {
        // Calculer la direction de collision
        Vector3 collisionNormal = (transform.position - other.transform.position).normalized;

        // Séparer les objets
        float overlap = (bounds.size.x + other.bounds.size.x) / 2 -
                       Vector3.Distance(transform.position, other.transform.position);

        if (overlap > 0)
        {
            transform.position += collisionNormal * overlap * 0.5f;
            other.transform.position -= collisionNormal * overlap * 0.5f;

            // Échanger les vitesses (collision élastique simple)
            Vector3 tempVelocity = velocity;
            velocity = other.velocity * bounciness;
            other.velocity = tempVelocity * other.bounciness;

            Debug.Log($"Object collision between {name} and {other.name}");
        }
    }

    public void AddForce(Vector3 force, ForceMode mode = ForceMode.Force)
    {
        switch (mode)
        {
            case ForceMode.Force:
                acceleration += force / mass;
                break;
            case ForceMode.Acceleration:
                acceleration += force;
                break;
            case ForceMode.Impulse:
                velocity += force / mass;
                break;
            case ForceMode.VelocityChange:
                velocity += force;
                break;
        }
    }

    void OnDrawGizmos()
    {
        // Visualiser les bounds pour le débogage
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
}

public enum ForceMode { Force, Acceleration, Impulse, VelocityChange }