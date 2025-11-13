using UnityEngine;

public class GravityForce
{
    private Vector3 gravity = new Vector3(0, -9.81f, 0);

    public GravityForce() { }

    public GravityForce(Vector3 customGravity)
    {
        gravity = customGravity;
    }

    public Vector3 CalculateForce(float mass)
    {
        return gravity * mass;
    }

    public void SetGravity(Vector3 newGravity)
    {
        gravity = newGravity;
    }
}