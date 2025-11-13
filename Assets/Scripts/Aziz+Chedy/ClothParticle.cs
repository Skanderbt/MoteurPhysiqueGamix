using UnityEngine;

public class ClothParticle
{
    public Vector3 position;
    public Vector3 velocity;
    public Vector3 force;
    public float mass = 0.1f;
    public bool isPinned = false;

    public ClothParticle(Vector3 pos, float m)
    {
        position = pos;
        velocity = Vector3.zero;
        force = Vector3.zero;
        mass = m;
    }

    public void AddForce(Vector3 f)
    {
        if (!isPinned)
            force += f;
    }

    public void Integrate(float dt, float gravity)
    {
        if (isPinned) return;

        // Semi-implicit Euler integration
        velocity += (force / mass) * dt;
        velocity.y -= gravity * dt; // Add gravity
        position += velocity * dt;

        // Clear force for next frame
        force = Vector3.zero;
    }

    public void ApplyGroundCollision(float groundY = 0f, float bounciness = 0.3f, float friction = 0.8f)
    {
        if (position.y < groundY)
        {
            position.y = groundY;
            velocity.y = Mathf.Abs(velocity.y) * bounciness;
            velocity.x *= friction;
            velocity.z *= friction;
        }
    }
}
