using UnityEngine;

public class EnhancedPhysicsCube : PhysicsCubeObject
{
    [Header("Enhanced Physics")]
    public Vector3 angularVelocity = Vector3.zero;
    public bool isActivePiece = false;
    public float rotationDamping = 0.98f;
    public float lifetimeAfterDestruction = 8f;
    private float destructionTimer = 0f;

    void Update()
    {
        if (isActivePiece)
        {
            // Apply rotation
            if (angularVelocity.magnitude > 0.01f)
            {
                transform.Rotate(angularVelocity * Time.deltaTime * 50f);
                angularVelocity *= rotationDamping;
            }

            // Count down lifetime and destroy
            destructionTimer += Time.deltaTime;
            if (destructionTimer > lifetimeAfterDestruction)
            {
                Destroy(gameObject);
            }
        }
    }

    public override void CheckGroundCollision()
    {
        if (!enableCollisions) return;

        float groundLevel = 0f;
        float bottom = transform.position.y - height / 2;

        if (bottom <= groundLevel && velocity.y < 0)
        {
            float penetration = groundLevel - bottom;
            transform.position += new Vector3(0, penetration, 0);

            // Bounce with energy loss
            velocity.y = -velocity.y * bounciness;
            velocity.x *= (1f - friction);
            velocity.z *= (1f - friction);

            // Add some random rotation on bounce
            if (isActivePiece)
            {
                angularVelocity += new Vector3(
                    Random.Range(-2f, 2f),
                    Random.Range(-1f, 1f),
                    Random.Range(-2f, 2f)
                );
            }

            if (Mathf.Abs(velocity.y) < 0.1f)
            {
                velocity.y = 0f;
            }
        }
    }
}