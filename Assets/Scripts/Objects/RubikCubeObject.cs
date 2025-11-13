using UnityEngine;
using System.Collections.Generic;

public class RubikCubeObject : PhysicsCubeObject
{
    [Header("Rubik Cube Properties")]
    public bool isDestroyed = false;
    [Tooltip("Energy threshold (Joules) - cube breaks if impact exceeds this")]
    public float destructionForceThreshold = 150f; // Energy threshold in Joules
    public float baseSize = 1.6f;
    public List<GameObject> cubePieces = new List<GameObject>();

    [Header("Explosion Settings")]
    [Tooltip("How long pieces exist before cleanup")]
    public float pieceLifetime = 10f;

    private Color[] possibleColors = {
        Color.red, Color.blue, Color.green,
        Color.yellow, Color.white, Color.magenta,
        new Color(1f, 0.5f, 0f), // Orange
        Color.cyan, new Color(0.5f, 0.2f, 0.8f) // Violet
    };

    void Start()
    {
        // Don't call the base CreateCubeMesh - we'll create pieces instead
        CreateRubikCube();
        InitializeBounds();
    }

    public void CreateRubikCube()
    {
        width = baseSize;
        height = baseSize;
        depth = baseSize;
        
        // Disable the main mesh renderer - we only want to see pieces
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
        }
        meshRenderer.enabled = false;
        
        SetRandomColors();
    }

    public void SetRandomColors()
    {
        DestroyAllPieces();

        float pieceSize = baseSize / 3.2f;
        float gap = 0.02f;

        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                for (int z = 0; z < 3; z++)
                {
                    CreatePiece(x, y, z, pieceSize, gap);
                }
            }
        }
        InitializeBounds();
    }

    void CreatePiece(int x, int y, int z, float pieceSize, float gap)
    {
        GameObject piece = new GameObject($"RubikPiece_{x}_{y}_{z}");
        PhysicsCubeObject pieceObj = piece.AddComponent<PhysicsCubeObject>();

        Vector3 localPos = new Vector3(
            (x - 1) * (pieceSize + gap),
            (y - 1) * (pieceSize + gap),
            (z - 1) * (pieceSize + gap)
        );

        piece.transform.position = transform.position + localPos;
        piece.transform.SetParent(transform);

        // Piece properties - inherit from parent Rubik cube
        pieceObj.width = pieceSize;
        pieceObj.height = pieceSize;
        pieceObj.depth = pieceSize;
        
        // Physics properties inherited from parent
        pieceObj.mass = mass / 27f; // Each piece is 1/27th of total mass
        pieceObj.bounciness = bounciness * 0.8f; // Slightly less bouncy for realism
        pieceObj.friction = friction * 1.2f; // Slightly more friction
        
        pieceObj.useGravity = false; // Will be enabled on explosion
        pieceObj.enableCollisions = false; // Will be enabled on explosion

        pieceObj.CreateCubeMesh();
        pieceObj.SetColor(GetColorForPosition(x, y, z));

        cubePieces.Add(piece);
    }

    Color GetColorForPosition(int x, int y, int z)
    {
        // Color only the outer pieces
        if (x == 0 || x == 2 || y == 0 || y == 2 || z == 0 || z == 2)
            return GetRandomColor();

        return new Color(0.2f, 0.2f, 0.2f); // Dark inner pieces
    }

    Color GetRandomColor()
    {
        return possibleColors[Random.Range(0, possibleColors.Length)];
    }

    public override void CheckGroundCollision()
    {
        if (isDestroyed || !enableCollisions) return;

        float groundLevel = 0f;
        float bottom = transform.position.y - height / 2;

        if (bottom <= groundLevel && velocity.y < 0)
        {
            float impactSpeed = Mathf.Abs(velocity.y);
            // Impact force = kinetic energy = 0.5 * m * v^2
            float impactForce = 0.5f * mass * impactSpeed * impactSpeed;
            
            Debug.Log($"🎯 RUBIK CUBE GROUND IMPACT!");
            Debug.Log($"   Velocity: {impactSpeed:F2} m/s, Mass: {mass:F2}");
            Debug.Log($"   Impact Force: {impactForce:F2}, Threshold: {destructionForceThreshold:F2}");

            if (impactForce > destructionForceThreshold)
            {
                Debug.Log($"💥 DESTRUCTION! Force {impactForce:F2} > Threshold {destructionForceThreshold:F2}");
                DestroyRubikCube();
            }
            else
            {
                Debug.Log($"⚠️ Bounce - Impact too weak: {impactForce:F2} <= {destructionForceThreshold:F2}");
                // Normal bounce
                base.CheckGroundCollision();
            }
        }
    }

    public void DestroyRubikCube()
    {
        if (isDestroyed) return;

        isDestroyed = true;

        // Calculate impact energy - THIS drives the explosion intensity!
        // E = 0.5 * m * v^2
        float impactSpeed = velocity.magnitude;
        float impactEnergy = 0.5f * mass * impactSpeed * impactSpeed;
        
        Debug.Log($"💥 EXPLOSION PHYSICS:");
        Debug.Log($"   Cube Mass: {mass:F2} kg");
        Debug.Log($"   Impact Speed: {impactSpeed:F2} m/s");
        Debug.Log($"   Impact Energy: {impactEnergy:F2} J (this determines explosion violence!)");
        Debug.Log($"   Energy per piece: {impactEnergy/27f:F2} J");

        Vector3 inheritedVelocity = velocity;

        foreach (GameObject piece in cubePieces)
        {
            if (piece != null)
            {
                ReleasePiece(piece, inheritedVelocity, impactEnergy);
            }
        }

        // Hide main cube
        if (meshRenderer != null)
            meshRenderer.enabled = false;

        // Clean up after delay
        Invoke("CleanUpAfterExplosion", pieceLifetime);
    }

    void ReleasePiece(GameObject piece, Vector3 inheritedVelocity, float totalImpactEnergy)
    {
        piece.transform.SetParent(null);
        PhysicsCubeObject pieceObj = piece.GetComponent<PhysicsCubeObject>();

        if (pieceObj != null)
        {
            // Enable individual physics
            pieceObj.useGravity = true;
            pieceObj.enableCollisions = true;
            
            // Set physics manager reference
            pieceObj.SetPhysicsManager(physicsManager);
            
            // CRITICAL: Register piece with physics manager so it gets updated!
            if (physicsManager != null)
            {
                physicsManager.RegisterPieceAsCube(piece);
            }

            // Calculate explosion direction from center
            Vector3 centerToPiece = (piece.transform.position - transform.position);
            float distanceFromCenter = centerToPiece.magnitude;
            
            if (distanceFromCenter < 0.01f)
            {
                // For center pieces, add random direction
                centerToPiece = new Vector3(
                    Random.Range(-1f, 1f),
                    Random.Range(0.5f, 1f),
                    Random.Range(-1f, 1f)
                ).normalized;
                distanceFromCenter = 0.5f;
            }
            else
            {
                centerToPiece = centerToPiece.normalized;
            }

            // ============ REAL PHYSICS - LIKE A BREAKING PLATE! ============
            // Impact Energy = 0.5 * MASS * VELOCITY^2
            // Higher MASS = MORE impact energy (heavier plate hits harder!)
            // Higher VELOCITY = MUCH MORE energy (squared effect!)
            // Higher GRAVITY = higher velocity = more energy
            
            // Each piece gets a portion of the total impact energy
            // Pieces further from center fly faster (like real shattering)
            float distanceFactor = 0.3f + (distanceFromCenter / baseSize) * 1.7f;
            float energyForThisPiece = (totalImpactEnergy / 27f) * distanceFactor;
            
            // KEY FORMULA: velocity from kinetic energy
            // v = sqrt(2 * KE / m)
            // IMPORTANT: Heavier pieces (higher m) = SLOWER velocity (correct physics!)
            //            Lighter pieces (lower m) = FASTER velocity
            float pieceSpeed = Mathf.Sqrt((2f * energyForThisPiece) / pieceObj.mass);
            
            // Add natural randomness (real breaks aren't perfectly uniform)
            pieceSpeed *= Random.Range(0.7f, 1.3f);
            
            // Direction: radially outward from impact point
            Vector3 pieceVelocity = centerToPiece * pieceSpeed;
            
            // Add upward component (pieces tend to go up from ground impact)
            pieceVelocity.y += pieceSpeed * 0.25f;
            
            // Conservation of momentum: pieces inherit some of the cube's velocity
            Vector3 momentumTransfer = inheritedVelocity * 0.1f;
            
            // FINAL VELOCITY - purely physics based!
            pieceObj.velocity = pieceVelocity + momentumTransfer;

            Debug.Log($"Piece mass={pieceObj.mass:F2}kg energy={energyForThisPiece:F1}J speed={pieceObj.velocity.magnitude:F2}m/s");
        }
    }

    void CleanUpAfterExplosion()
    {
        // Clean up all pieces
        foreach (GameObject piece in cubePieces)
        {
            if (piece != null)
            {
                Destroy(piece);
            }
        }
        cubePieces.Clear();
        
        Destroy(gameObject);
    }

    // Methods to update pieces when parameters change
    public void UpdatePiecesMass()
    {
        float pieceMass = mass / 27f;
        foreach (GameObject piece in cubePieces)
        {
            if (piece != null)
            {
                PhysicsCubeObject pieceObj = piece.GetComponent<PhysicsCubeObject>();
                if (pieceObj != null)
                {
                    pieceObj.mass = pieceMass;
                }
            }
        }
        Debug.Log($"Updated {cubePieces.Count} pieces to mass: {pieceMass:F3}");
    }

    public void UpdatePiecesBounciness()
    {
        float pieceBounciness = bounciness * 0.8f;
        foreach (GameObject piece in cubePieces)
        {
            if (piece != null)
            {
                PhysicsCubeObject pieceObj = piece.GetComponent<PhysicsCubeObject>();
                if (pieceObj != null)
                {
                    pieceObj.bounciness = pieceBounciness;
                }
            }
        }
        Debug.Log($"Updated {cubePieces.Count} pieces to bounciness: {pieceBounciness:F3}");
    }

    public void UpdatePiecesFriction()
    {
        float pieceFriction = friction * 1.2f;
        foreach (GameObject piece in cubePieces)
        {
            if (piece != null)
            {
                PhysicsCubeObject pieceObj = piece.GetComponent<PhysicsCubeObject>();
                if (pieceObj != null)
                {
                    pieceObj.friction = pieceFriction;
                }
            }
        }
        Debug.Log($"Updated {cubePieces.Count} pieces to friction: {pieceFriction:F3}");
    }

    public void DestroyAllPieces()
    {
        foreach (GameObject piece in cubePieces)
        {
            if (piece != null)
                DestroyImmediate(piece);
        }
        cubePieces.Clear();
    }

    public override void UpdateBounds()
    {
        bounds.center = transform.position;
        bounds.size = new Vector3(width, height, depth);
    }

    void OnDestroy()
    {
        DestroyAllPieces();
    }
}