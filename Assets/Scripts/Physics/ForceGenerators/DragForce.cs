using UnityEngine;

public class DragForce
{
    public float dragCoefficient = 0.1f;
    public float airDensity = 1.2f;
    public float crossSectionalArea = 1.0f;

    public DragForce() { }

    public DragForce(float drag, float density = 1.2f, float area = 1.0f)
    {
        dragCoefficient = drag;
        airDensity = density;
        crossSectionalArea = area;
    }

    public Vector3 CalculateForce(Vector3 velocity)
    {
        if (velocity.magnitude < 0.01f) return Vector3.zero;

        float speed = velocity.magnitude;
        float dragMagnitude = 0.5f * dragCoefficient * airDensity * crossSectionalArea * speed * speed;
        Vector3 dragDirection = -velocity.normalized;

        return dragDirection * dragMagnitude;
    }

    public void SetDragCoefficient(float newDrag)
    {
        dragCoefficient = newDrag;
    }
}