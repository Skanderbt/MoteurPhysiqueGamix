using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PhysicsSimulationManager : MonoBehaviour
{
    [Header("Simulation Control")]
    public bool simulationRunning = true;
    public float timeScale = 1.0f;

    [Header("Available Simulations")]
    public SimulationType currentSimulation = SimulationType.FreeFall;

    [Header("Global Physics Parameters")]
    public float gravity = 9.81f;
    public bool enableCollisions = true;
    public bool enableAirResistance = false;

    [Header("Spawn Parameters")]
    public float spawnMass = 1.0f;
    public float spawnBounciness = 0.5f;
    public float spawnFriction = 0.3f;
    public float spawnRadius = 0.3f;

    // Valeurs par d�faut pour chaque simulation
    private Dictionary<SimulationType, SimulationDefaults> simulationDefaults = new Dictionary<SimulationType, SimulationDefaults>()
    {
        {
            SimulationType.FreeFall,
            new SimulationDefaults { mass = 1.0f, bounciness = 0.1f, friction = 0.4f, radius = 0.5f }
        },
        {
            SimulationType.RigidBody3D,
            new SimulationDefaults { mass = 2.0f, bounciness = 0.1f, friction = 0.4f, radius = 0.5f }
        },
        {
            SimulationType.SphereBounce,
            new SimulationDefaults { mass = 1.0f, bounciness = 0.8f, friction = 0.05f, radius = 0.3f }
        },
        {
            SimulationType.RubikCube,
            new SimulationDefaults { mass = 3.0f, bounciness = 0.05f, friction = 0.5f, radius = 0.8f }
        }
    };

    // Listes s�par�es pour chaque type d'objet
    private List<GameObject> activeCubes = new List<GameObject>();
    private List<GameObject> activeSpheres = new List<GameObject>();
    private List<GameObject> activeRubikCubes = new List<GameObject>();

    // CHANGEMENT: Timer pour la physique
    private float physicsTimer = 0f;
    private const float PHYSICS_TIME_STEP = 0.02f; // 50 FPS pour la physique

    void Start()
    {
        Debug.Log("PhysicsSimulationManager started");
        ApplySimulationDefaults(currentSimulation);
        CreateInitialSimulation();
    }

    void Update()
    {
        if (!simulationRunning) return;
        // TEMPORARY FIX: Comment out input to avoid Input System conflict
        // HandleGlobalInput();
    }

    void FixedUpdate()
    {
        // CHANGEMENT: Physique � pas fixe pour plus de stabilit�
        if (!simulationRunning) return;

        // Accumuler le temps
        physicsTimer += Time.fixedDeltaTime;

        // Ex�cuter la physique � pas fixe
        while (physicsTimer >= PHYSICS_TIME_STEP)
        {
            UpdatePhysics(PHYSICS_TIME_STEP);
            physicsTimer -= PHYSICS_TIME_STEP;
        }
    }

    // CHANGEMENT: M�thode s�par�e pour la physique
    void UpdatePhysics(float deltaTime)
    {
        // Appliquer le time scale
        float scaledDeltaTime = deltaTime * timeScale;

        // Mettre � jour tous les objets physiques
        UpdateAllPhysicsObjects(scaledDeltaTime);

        // Check for object collisions
        if (enableCollisions)
        {
            CheckObjectCollisions();
        }
    }

    void UpdateAllPhysicsObjects(float deltaTime)
    {
        // CHANGEMENT: Mettre � jour tous les objets avec le m�me deltaTime
        foreach (GameObject obj in activeCubes)
        {
            if (obj != null)
            {
                PhysicsCubeObject physObj = obj.GetComponent<PhysicsCubeObject>();
                if (physObj != null && physObj.enabled)
                {
                    UpdateSinglePhysicsObject(physObj, deltaTime);
                }
            }
        }

        foreach (GameObject obj in activeSpheres)
        {
            if (obj != null)
            {
                PhysicsSphereObject physObj = obj.GetComponent<PhysicsSphereObject>();
                if (physObj != null && physObj.enabled)
                {
                    UpdateSinglePhysicsObject(physObj, deltaTime);
                }
            }
        }

        foreach (GameObject obj in activeRubikCubes)
        {
            if (obj != null)
            {
                RubikCubeObject physObj = obj.GetComponent<RubikCubeObject>();
                if (physObj != null && physObj.enabled && !physObj.isDestroyed)
                {
                    UpdateSinglePhysicsObject(physObj, deltaTime);
                }
            }
        }
    }

    void UpdateSinglePhysicsObject(PhysicsCubeObject physObj, float deltaTime)
    {
        // FIX: Simply call the virtual methods - let polymorphism handle the rest
        physObj.ApplyForces(deltaTime);
        physObj.Integrate(deltaTime);

        if (physObj.enableCollisions)
        {
            physObj.CheckGroundCollision();
        }
    }

    void CheckObjectCollisions()
    {
        List<PhysicsCubeObject> allCollidableObjects = GetAllCollidableObjects();

        // Check collisions between all objects
        for (int i = 0; i < allCollidableObjects.Count; i++)
        {
            for (int j = i + 1; j < allCollidableObjects.Count; j++)
            {
                PhysicsCubeObject obj1 = allCollidableObjects[i];
                PhysicsCubeObject obj2 = allCollidableObjects[j];

                if (obj1 != null && obj2 != null &&
                    obj1.enableCollisions && obj2.enableCollisions)
                {
                    // Check if objects are destroyed (only for RubikCubeObject)
                    bool obj1Destroyed = false;
                    bool obj2Destroyed = false;

                    RubikCubeObject rubik1 = obj1 as RubikCubeObject;
                    RubikCubeObject rubik2 = obj2 as RubikCubeObject;

                    if (rubik1 != null) obj1Destroyed = rubik1.isDestroyed;
                    if (rubik2 != null) obj2Destroyed = rubik2.isDestroyed;

                    if (!obj1Destroyed && !obj2Destroyed)
                    {
                        if (CheckBoundsIntersection(obj1, obj2))
                        {
                            ResolveObjectCollision(obj1, obj2);
                        }
                    }
                }
            }
        }
    }

    List<PhysicsCubeObject> GetAllCollidableObjects()
    {
        List<PhysicsCubeObject> allObjects = new List<PhysicsCubeObject>();

        // Add all cubes
        foreach (GameObject obj in activeCubes)
        {
            if (obj != null)
            {
                PhysicsCubeObject physObj = obj.GetComponent<PhysicsCubeObject>();
                if (physObj != null && physObj.enableCollisions)
                    allObjects.Add(physObj);
            }
        }

        // Add all spheres
        foreach (GameObject obj in activeSpheres)
        {
            if (obj != null)
            {
                PhysicsSphereObject physObj = obj.GetComponent<PhysicsSphereObject>();
                if (physObj != null && physObj.enableCollisions)
                    allObjects.Add(physObj);
            }
        }

        // Add all Rubik Cubes
        foreach (GameObject obj in activeRubikCubes)
        {
            if (obj != null)
            {
                RubikCubeObject physObj = obj.GetComponent<RubikCubeObject>();
                if (physObj != null && physObj.enableCollisions)
                {
                    // Only add if not destroyed
                    if (!physObj.isDestroyed)
                        allObjects.Add(physObj);
                }
            }
        }

        return allObjects;
    }

    void ResolveObjectCollision(PhysicsCubeObject obj1, PhysicsCubeObject obj2)
    {
        // Calculate collision normal and penetration
        Vector3 collisionNormal = (obj1.transform.position - obj2.transform.position).normalized;
        float distance = Vector3.Distance(obj1.transform.position, obj2.transform.position);
        float minDistance = (obj1.GetBounds().size.magnitude + obj2.GetBounds().size.magnitude) * 0.5f;
        float penetration = minDistance - distance;

        if (penetration > 0)
        {
            // Separate objects
            Vector3 separation = collisionNormal * penetration * 0.5f;
            obj1.transform.position += separation;
            obj2.transform.position -= separation;

            // Calculate collision response - conservation of momentum
            Vector3 relativeVelocity = obj1.velocity - obj2.velocity;
            float velocityAlongNormal = Vector3.Dot(relativeVelocity, collisionNormal);

            // Don't resolve if objects are moving apart
            if (velocityAlongNormal > 0) return;

            // Calculate impulse scalar
            float restitution = Mathf.Min(obj1.bounciness, obj2.bounciness);
            float impulseScalar = -(1 + restitution) * velocityAlongNormal;
            impulseScalar /= (1 / obj1.mass) + (1 / obj2.mass);

            // Apply impulse
            Vector3 impulse = collisionNormal * impulseScalar;
            obj1.velocity += impulse / obj1.mass;
            obj2.velocity -= impulse / obj2.mass;

            // Apply friction
            ApplyFriction(obj1, obj2, collisionNormal, relativeVelocity);
            
            // PHYSICS: Apply torque (rotation) from off-center collision
            ApplyCollisionTorque(obj1, obj2, collisionNormal, impulse);

            Debug.Log($"Collision between {obj1.name} and {obj2.name}");

            // Check for Rubik Cube destruction
            CheckRubikCubeDestruction(obj1, obj2, Mathf.Abs(velocityAlongNormal));
        }
    }
    
    void ApplyCollisionTorque(PhysicsCubeObject obj1, PhysicsCubeObject obj2, Vector3 collisionNormal, Vector3 impulse)
    {
        // Calculate contact point (approximation - midpoint between surfaces)
        Vector3 contactPoint = (obj1.transform.position + obj2.transform.position) * 0.5f;
        
        // Torque = r × F (cross product of radius vector and force)
        // r = contact point - center of mass
        Vector3 r1 = contactPoint - obj1.transform.position;
        Vector3 r2 = contactPoint - obj2.transform.position;
        
        // Calculate torque from impulse
        Vector3 torque1 = Vector3.Cross(r1, impulse);
        Vector3 torque2 = Vector3.Cross(r2, -impulse);
        
        // Angular acceleration = torque / moment of inertia
        // For a cube: I ≈ (1/6) * m * size²
        float size1 = (obj1.width + obj1.height + obj1.depth) / 3f;
        float size2 = (obj2.width + obj2.height + obj2.depth) / 3f;
        float inertia1 = (1f / 6f) * obj1.mass * size1 * size1;
        float inertia2 = (1f / 6f) * obj2.mass * size2 * size2;
        
        // Apply angular velocity change
        obj1.angularVelocity += torque1 / inertia1;
        obj2.angularVelocity += torque2 / inertia2;
    }

    void ApplyFriction(PhysicsCubeObject obj1, PhysicsCubeObject obj2, Vector3 collisionNormal, Vector3 relativeVelocity)
    {
        // Calculate tangent velocity (velocity perpendicular to collision normal)
        Vector3 tangentVelocity = relativeVelocity - Vector3.Dot(relativeVelocity, collisionNormal) * collisionNormal;

        if (tangentVelocity.magnitude > 0.01f)
        {
            Vector3 tangentDirection = tangentVelocity.normalized;
            float frictionMagnitude = Mathf.Min(obj1.friction, obj2.friction);

            // Apply friction impulse
            Vector3 frictionImpulse = tangentDirection * frictionMagnitude * tangentVelocity.magnitude;
            obj1.velocity -= frictionImpulse / obj1.mass;
            obj2.velocity += frictionImpulse / obj2.mass;
        }
    }

    void CheckRubikCubeDestruction(PhysicsCubeObject obj1, PhysicsCubeObject obj2, float impactSpeed)
    {
        // Check if obj1 is a RubikCube and should be destroyed
        RubikCubeObject rubik1 = obj1 as RubikCubeObject;
        if (rubik1 != null && !rubik1.isDestroyed)
        {
            // Calculate impact force: F = m * v
            float impactForce = impactSpeed * (rubik1.mass + obj2.mass) * 0.5f;
            Debug.Log($"Rubik1 impact force: {impactForce}, threshold: {rubik1.destructionForceThreshold}");
            
            if (impactForce > rubik1.destructionForceThreshold)
            {
                Debug.Log($"💥 Rubik Cube 1 destroyed by collision! Force: {impactForce}");
                rubik1.DestroyRubikCube();
            }
        }

        // Check if obj2 is a RubikCube and should be destroyed
        RubikCubeObject rubik2 = obj2 as RubikCubeObject;
        if (rubik2 != null && !rubik2.isDestroyed)
        {
            // Calculate impact force: F = m * v
            float impactForce = impactSpeed * (rubik2.mass + obj1.mass) * 0.5f;
            Debug.Log($"Rubik2 impact force: {impactForce}, threshold: {rubik2.destructionForceThreshold}");
            
            if (impactForce > rubik2.destructionForceThreshold)
            {
                Debug.Log($"💥 Rubik Cube 2 destroyed by collision! Force: {impactForce}");
                rubik2.DestroyRubikCube();
            }
        }
    }

    // Helper method to check if bounds intersect
    bool CheckBoundsIntersection(PhysicsCubeObject obj1, PhysicsCubeObject obj2)
    {
        Bounds bounds1 = obj1.GetBounds();
        Bounds bounds2 = obj2.GetBounds();
        return bounds1.Intersects(bounds2);
    }


    void HandleGlobalInput()
    {
        // TEMPORARY FIX: Disabled due to Input System conflict
        // To fix: Go to Edit > Project Settings > Player > Active Input Handling
        // Change from "Input System Package (New)" to "Both" or "Input Manager (Old)"
        
        /*
        if (Input.GetKeyDown(KeyCode.Space)) ToggleSimulation();
        if (Input.GetKeyDown(KeyCode.Alpha1)) SetSimulation(SimulationType.FreeFall);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SetSimulation(SimulationType.RigidBody3D);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SetSimulation(SimulationType.SphereBounce);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SetSimulation(SimulationType.RubikCube);
        if (Input.GetKeyDown(KeyCode.R)) ResetSimulation();
        */
    }

    public void SetSimulation(SimulationType type)
    {
        ClearAllObjects();
        currentSimulation = type;
        ApplySimulationDefaults(type);
        CreateInitialSimulation();

        Debug.Log($"Simulation changed to: {type}");
    }

    void CreateInitialSimulation()
    {
        switch (currentSimulation)
        {
            case SimulationType.FreeFall:
                CreateFreeFallSimulation();
                break;
            case SimulationType.RigidBody3D:
                CreateRigidBodySimulation();
                break;
            case SimulationType.SphereBounce:
                CreateSphereBounceSimulation();
                break;
            case SimulationType.RubikCube:
                CreateRubikCubeSimulation();
                break;
        }
    }

    void ApplySimulationDefaults(SimulationType type)
    {
        if (simulationDefaults.ContainsKey(type))
        {
            var defaults = simulationDefaults[type];
            spawnMass = defaults.mass;
            spawnBounciness = defaults.bounciness;
            spawnFriction = defaults.friction;
            spawnRadius = defaults.radius;
        }
    }

// D�truit TOUS les objets de tous types
public void ClearAllObjects()
    {
        // D�truire tous les cubes
        foreach (var cube in activeCubes)
        {
            if (cube != null)
            {
                Destroy(cube);
            }
        }
        activeCubes.Clear();

        // D�truire toutes les sph�res
        foreach (var sphere in activeSpheres)
        {
            if (sphere != null) Destroy(sphere);
        }
        activeSpheres.Clear();

        // D�truire tous les Rubik Cubes
        foreach (var rubik in activeRubikCubes)
        {
            if (rubik != null)
            {
                RubikCubeObject rubikObj = rubik.GetComponent<RubikCubeObject>();
                if (rubikObj != null)
                {
                    rubikObj.DestroyAllPieces();
                }
                Destroy(rubik);
            }
        }
        activeRubikCubes.Clear();

        Debug.Log("All objects destroyed: cubes, spheres and Rubik cubes");
    }

    void CreateFreeFallSimulation()
    {
        for (int i = 0; i < 3; i++)
        {
            SpawnCube(new Vector3(i * 2 - 2, 8 + i, 0));
        }
    }

    void CreateRigidBodySimulation()
    {
        GameObject cube = SpawnCube(new Vector3(0, 5, 0));
        PhysicsCubeObject cubeObj = cube.GetComponent<PhysicsCubeObject>();
        cubeObj.velocity = new Vector3(3f, 2f, 1f);
        cubeObj.SetColor(Color.yellow);
    }

    void CreateSphereBounceSimulation()
    {
        for (int i = 0; i < 4; i++)
        {
            GameObject sphere = SpawnSphere(new Vector3(i * 2f - 3, 3 + i * 2, 0));
            PhysicsSphereObject sphereObj = sphere.GetComponent<PhysicsSphereObject>();
            sphereObj.velocity = new Vector3(Random.Range(-3f, 3f), Random.Range(-1f, 1f), 0);
            sphereObj.SetColor(new Color(0f, 0.5f + i * 0.1f, 1f));
        }
    }

    void CreateRubikCubeSimulation()
    {
        // Spawn Rubik cube at moderate height to test breaking vs bouncing
        GameObject rubikCube = SpawnRubikCube(new Vector3(0, 15, 0));
        RubikCubeObject rubikObj = rubikCube.GetComponent<RubikCubeObject>();
        
        // Let gravity do the work - no initial velocity
        rubikObj.velocity = Vector3.zero;
        rubikObj.useGravity = true;
        rubikObj.enableCollisions = true;
        
        // Destruction threshold - needs significant impact to break
        // Lower mass = easier to break (fragile)
        // Higher mass = harder to break (sturdy)
        // Formula: Base threshold + mass factor
        rubikObj.destructionForceThreshold = 80f + (rubikObj.mass * 15f);

        // Calculate if it will break at this height
        float expectedImpactEnergy = 0.5f * rubikObj.mass * (2f * gravity * 15f);
        string willBreak = expectedImpactEnergy > rubikObj.destructionForceThreshold ? "BREAK" : "BOUNCE";
        
        Debug.Log($"Rubik Cube spawned at h=15m - Mass: {rubikObj.mass:F1}, Break Threshold: {rubikObj.destructionForceThreshold:F1}J");
        Debug.Log($"   Expected impact energy: {expectedImpactEnergy:F1}J → Will {willBreak}");
    }

    GameObject SpawnHeavyObstacle(Vector3 position)
    {
        GameObject obstacle = new GameObject("HeavyObstacle");
        PhysicsCubeObject obstacleObj = obstacle.AddComponent<PhysicsCubeObject>();

        obstacle.transform.position = position;
        obstacleObj.mass = 20f; // Very heavy
        obstacleObj.bounciness = 0.1f;
        obstacleObj.friction = 0.9f;
        obstacleObj.useGravity = false;
        obstacleObj.enableCollisions = true;
        obstacleObj.SetColor(Color.gray);

        // Large obstacles
        obstacleObj.width = 2f;
        obstacleObj.height = 2f;
        obstacleObj.depth = 2f;
        obstacleObj.CreateCubeMesh();

        activeCubes.Add(obstacle);
        return obstacle;
    }

    public void SpawnObject()
    {
        Vector3 spawnPos = new Vector3(Random.Range(-4f, 4f), 8f, Random.Range(-2f, 2f));

        switch (currentSimulation)
        {
            case SimulationType.SphereBounce:
                SpawnSphere(spawnPos);
                break;
            case SimulationType.RubikCube:
                SpawnRubikCube(spawnPos);
                break;
            case SimulationType.FreeFall:
            case SimulationType.RigidBody3D:
            default:
                SpawnCube(spawnPos);
                break;
        }
    }

    GameObject SpawnSphere(Vector3 position)
    {
        GameObject sphere = new GameObject("SpawnedSphere");
        PhysicsSphereObject sphereObj = sphere.AddComponent<PhysicsSphereObject>();

        sphere.transform.position = position;

        // APPLIQUER TOUS LES PARAM�TRES
        sphereObj.mass = spawnMass;
        sphereObj.radius = spawnRadius;
        sphereObj.bounciness = spawnBounciness;
        sphereObj.friction = spawnFriction;
        sphereObj.useGravity = true; // CORRECTION: Gravit� activ�e
        sphereObj.enableCollisions = enableCollisions;

        // RECR�ER LE MESH AVEC LE NOUVEAU RAYON
        sphereObj.CreateSphereMesh();
        sphereObj.SetColor(new Color(Random.value, Random.value, Random.value));

        sphereObj.velocity = new Vector3(Random.Range(-2f, 2f), Random.Range(-1f, 1f), Random.Range(-1f, 1f));

        activeSpheres.Add(sphere);
        Debug.Log($"Spawned sphere - Mass: {spawnMass}, Radius: {spawnRadius}, Bounciness: {spawnBounciness}");
        return sphere;
    }

    GameObject SpawnCube(Vector3 position)
    {
        GameObject cube = new GameObject("SpawnedCube");
        PhysicsCubeObject cubeObj = cube.AddComponent<PhysicsCubeObject>();

        cube.transform.position = position;

        // APPLIQUER TOUS LES PARAM�TRES - le rayon affecte la taille du cube
        cubeObj.mass = spawnMass;
        cubeObj.bounciness = spawnBounciness;
        cubeObj.friction = spawnFriction;
        cubeObj.useGravity = true;
        cubeObj.enableCollisions = enableCollisions;

        // UTILISER LE RAYON POUR LA TAILLE DES CUBES AUSSI
        cubeObj.width = spawnRadius * 2f;
        cubeObj.height = spawnRadius * 2f;
        cubeObj.depth = spawnRadius * 2f;

        // RECR�ER LE MESH AVEC LA NOUVELLE TAILLE
        cubeObj.CreateCubeMesh();
        cubeObj.SetColor(new Color(Random.value, Random.value, Random.value));

        cubeObj.velocity = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));

        activeCubes.Add(cube);
        Debug.Log($"Spawned cube - Mass: {spawnMass}, Size: {spawnRadius * 2f}, Bounciness: {spawnBounciness}");
        return cube;
    }

    GameObject SpawnRubikCube(Vector3 position)
    {
        GameObject rubikCube = new GameObject("SpawnedRubikCube");
        RubikCubeObject rubikObj = rubikCube.AddComponent<RubikCubeObject>();

        rubikCube.transform.position = position;

        // APPLIQUER TOUS LES PARAM�TRES AU RUBIK CUBE
        rubikObj.mass = spawnMass * 3f;
        rubikObj.bounciness = spawnBounciness * 0.2f;
        rubikObj.friction = spawnFriction;
        rubikObj.useGravity = true; // CORRECTION: Gravit� activ�e
        rubikObj.enableCollisions = enableCollisions;

        // LE RAYON AFFECTE LA TAILLE DU RUBIK CUBE
        rubikObj.baseSize = spawnRadius * 2f;

        // G�n�rer des couleurs al�atoires pour chaque spawn
        rubikObj.SetRandomColors();

        activeRubikCubes.Add(rubikCube);
        Debug.Log($"Spawned Rubik cube - Mass: {spawnMass * 3f}, Size: {spawnRadius * 2f}, Bounciness: {spawnBounciness * 0.2f}");
        return rubikCube;
    }

    // M�thodes pour contr�ler les param�tres de spawn
    public void SetSpawnMass(float mass)
    {
        spawnMass = mass;
        Debug.Log($"Spawn mass set to: {mass}");

        // Mettre � jour la masse de tous les objets existants
        UpdateAllObjectsMass();
    }

    public void SetSpawnBounciness(float bounciness)
    {
        spawnBounciness = bounciness;
        Debug.Log($"Spawn bounciness set to: {bounciness}");

        // Mettre � jour l'�lasticit� de tous les objets existants
        UpdateAllObjectsBounciness();
    }

    public void SetSpawnFriction(float friction)
    {
        spawnFriction = friction;
        Debug.Log($"Spawn friction set to: {friction}");

        // Mettre � jour le frottement de tous les objets existants
        UpdateAllObjectsFriction();
    }

    public void SetSpawnRadius(float radius)
    {
        spawnRadius = radius;
        Debug.Log($"Spawn radius set to: {radius}");

        // Mettre � jour la taille de tous les objets existants
        UpdateAllObjectsSize();
    }

    // Mettre � jour la masse de tous les objets
    void UpdateAllObjectsMass()
    {
        foreach (GameObject obj in activeCubes)
        {
            if (obj != null)
            {
                PhysicsCubeObject physObj = obj.GetComponent<PhysicsCubeObject>();
                if (physObj != null) physObj.mass = spawnMass;
            }
        }

        foreach (GameObject obj in activeSpheres)
        {
            if (obj != null)
            {
                PhysicsSphereObject physObj = obj.GetComponent<PhysicsSphereObject>();
                if (physObj != null) physObj.mass = spawnMass;
            }
        }

        foreach (GameObject obj in activeRubikCubes)
        {
            if (obj != null)
            {
                RubikCubeObject rubikObj = obj.GetComponent<RubikCubeObject>();
                if (rubikObj != null && !rubikObj.isDestroyed)
                {
                    rubikObj.mass = spawnMass * 3f;
                    // Update destruction threshold: lower mass = easier to break
                    rubikObj.destructionForceThreshold = 80f + (rubikObj.mass * 15f);
                    // Update all pieces
                    rubikObj.UpdatePiecesMass();
                }
            }
        }
    }

    // Mettre � jour l'�lasticit� de tous les objets
    void UpdateAllObjectsBounciness()
    {
        foreach (GameObject obj in activeCubes)
        {
            if (obj != null)
            {
                PhysicsCubeObject physObj = obj.GetComponent<PhysicsCubeObject>();
                if (physObj != null) physObj.bounciness = spawnBounciness;
            }
        }

        foreach (GameObject obj in activeSpheres)
        {
            if (obj != null)
            {
                PhysicsSphereObject physObj = obj.GetComponent<PhysicsSphereObject>();
                if (physObj != null) physObj.bounciness = spawnBounciness;
            }
        }

        foreach (GameObject obj in activeRubikCubes)
        {
            if (obj != null)
            {
                RubikCubeObject rubikObj = obj.GetComponent<RubikCubeObject>();
                if (rubikObj != null && !rubikObj.isDestroyed)
                {
                    rubikObj.bounciness = spawnBounciness * 0.2f;
                    rubikObj.UpdatePiecesBounciness();
                }
            }
        }
    }

    // Mettre � jour le frottement de tous les objets
    void UpdateAllObjectsFriction()
    {
        foreach (GameObject obj in activeCubes)
        {
            if (obj != null)
            {
                PhysicsCubeObject physObj = obj.GetComponent<PhysicsCubeObject>();
                if (physObj != null) physObj.friction = spawnFriction;
            }
        }

        foreach (GameObject obj in activeSpheres)
        {
            if (obj != null)
            {
                PhysicsSphereObject physObj = obj.GetComponent<PhysicsSphereObject>();
                if (physObj != null) physObj.friction = spawnFriction;
            }
        }

        foreach (GameObject obj in activeRubikCubes)
        {
            if (obj != null)
            {
                RubikCubeObject rubikObj = obj.GetComponent<RubikCubeObject>();
                if (rubikObj != null && !rubikObj.isDestroyed)
                {
                    rubikObj.friction = spawnFriction;
                    rubikObj.UpdatePiecesFriction();
                }
            }
        }
    }

    // Mettre � jour la taille de tous les objets
    void UpdateAllObjectsSize()
    {
        foreach (GameObject obj in activeCubes)
        {
            if (obj != null)
            {
                PhysicsCubeObject physObj = obj.GetComponent<PhysicsCubeObject>();
                if (physObj != null)
                {
                    physObj.width = spawnRadius * 2f;
                    physObj.height = spawnRadius * 2f;
                    physObj.depth = spawnRadius * 2f;
                    physObj.CreateCubeMesh();
                }
            }
        }

        foreach (GameObject obj in activeSpheres)
        {
            if (obj != null)
            {
                PhysicsSphereObject physObj = obj.GetComponent<PhysicsSphereObject>();
                if (physObj != null)
                {
                    physObj.radius = spawnRadius;
                    physObj.CreateSphereMesh();
                }
            }
        }

        foreach (GameObject obj in activeRubikCubes)
        {
            if (obj != null)
            {
                RubikCubeObject physObj = obj.GetComponent<RubikCubeObject>();
                if (physObj != null && !physObj.isDestroyed)
                {
                    physObj.baseSize = spawnRadius * 2f;
                    physObj.SetRandomColors(); // Recr�e le cube avec la nouvelle taille
                }
            }
        }
    }

    // Reset complet de la simulation
    public void ResetSimulation()
    {
        ClearAllObjects();
        ApplySimulationDefaults(currentSimulation);
        CreateInitialSimulation();
        Debug.Log("Simulation completely reset - all objects destroyed and recreated");
    }

    // M�thodes pour obtenir des informations physiques
    public float GetTotalKineticEnergy()
    {
        float totalEnergy = 0f;

        // Cubes
        foreach (GameObject obj in activeCubes)
        {
            if (obj != null)
            {
                PhysicsCubeObject physObj = obj.GetComponent<PhysicsCubeObject>();
                if (physObj != null)
                {
                    totalEnergy += 0.5f * physObj.mass * physObj.velocity.sqrMagnitude;
                }
            }
        }

        // Sph�res
        foreach (GameObject obj in activeSpheres)
        {
            if (obj != null)
            {
                PhysicsSphereObject physObj = obj.GetComponent<PhysicsSphereObject>();
                if (physObj != null)
                {
                    totalEnergy += 0.5f * physObj.mass * physObj.velocity.sqrMagnitude;
                }
            }
        }

        // Rubik Cubes
        foreach (GameObject obj in activeRubikCubes)
        {
            if (obj != null)
            {
                RubikCubeObject physObj = obj.GetComponent<RubikCubeObject>();
                if (physObj != null && !physObj.isDestroyed)
                {
                    totalEnergy += 0.5f * physObj.mass * physObj.velocity.sqrMagnitude;
                }
            }
        }

        return totalEnergy;
    }

    public float GetTotalPotentialEnergy()
    {
        float totalEnergy = 0f;

        // Cubes
        foreach (GameObject obj in activeCubes)
        {
            if (obj != null)
            {
                PhysicsCubeObject physObj = obj.GetComponent<PhysicsCubeObject>();
                if (physObj != null && physObj.useGravity)
                {
                    totalEnergy += physObj.mass * gravity * obj.transform.position.y;
                }
            }
        }

        // Sph�res
        foreach (GameObject obj in activeSpheres)
        {
            if (obj != null)
            {
                PhysicsSphereObject physObj = obj.GetComponent<PhysicsSphereObject>();
                if (physObj != null && physObj.useGravity)
                {
                    totalEnergy += physObj.mass * gravity * obj.transform.position.y;
                }
            }
        }

        // Rubik Cubes
        foreach (GameObject obj in activeRubikCubes)
        {
            if (obj != null)
            {
                RubikCubeObject physObj = obj.GetComponent<RubikCubeObject>();
                if (physObj != null && physObj.useGravity && !physObj.isDestroyed)
                {
                    totalEnergy += physObj.mass * gravity * obj.transform.position.y;
                }
            }
        }

        return totalEnergy;
    }

    public int GetActiveObjectCount()
    {
        return activeCubes.Count + activeSpheres.Count + activeRubikCubes.Count;
    }

    public float GetAverageVelocity()
    {
        int count = 0;
        float totalVelocity = 0f;

        // Cubes
        foreach (GameObject obj in activeCubes)
        {
            if (obj != null)
            {
                PhysicsCubeObject physObj = obj.GetComponent<PhysicsCubeObject>();
                if (physObj != null)
                {
                    totalVelocity += physObj.velocity.magnitude;
                    count++;
                }
            }
        }

        // Sph�res
        foreach (GameObject obj in activeSpheres)
        {
            if (obj != null)
            {
                PhysicsSphereObject physObj = obj.GetComponent<PhysicsSphereObject>();
                if (physObj != null)
                {
                    totalVelocity += physObj.velocity.magnitude;
                    count++;
                }
            }
        }

        // Rubik Cubes
        foreach (GameObject obj in activeRubikCubes)
        {
            if (obj != null)
            {
                RubikCubeObject physObj = obj.GetComponent<RubikCubeObject>();
                if (physObj != null && !physObj.isDestroyed)
                {
                    totalVelocity += physObj.velocity.magnitude;
                    count++;
                }
            }
        }

        return count > 0 ? totalVelocity / count : 0f;
    }

    // Getters pour les param�tres de spawn (pour l'UI)
    public float GetSpawnMass() { return spawnMass; }
    public float GetSpawnBounciness() { return spawnBounciness; }
    public float GetSpawnFriction() { return spawnFriction; }
    public float GetSpawnRadius() { return spawnRadius; }

    // Register exploded Rubik cube pieces for physics updates
    public void RegisterPieceAsCube(GameObject piece)
    {
        if (piece != null && !activeCubes.Contains(piece))
        {
            activeCubes.Add(piece);
            Debug.Log($"✅ Registered piece {piece.name} for physics updates");
        }
    }

    public void ToggleSimulation()
    {
        simulationRunning = !simulationRunning;
        Debug.Log($"Simulation {(simulationRunning ? "Resumed" : "Paused")}");
    }

    public void SetGravity(float newGravity)
    {
        gravity = newGravity;
        Debug.Log($"Gravity set to: {newGravity}");
        UpdateAllObjects();
    }

    public void ToggleCollisions(bool enabled)
    {
        enableCollisions = enabled;
        Debug.Log($"Collisions: {enabled}");
        UpdateAllObjects();
    }

    public void ToggleAirResistance(bool enabled)
    {
        enableAirResistance = enabled;
        Debug.Log($"Air Resistance: {enabled}");
        UpdateAllObjects();
    }

    void UpdateAllObjects()
    {
        // Mettre � jour tous les cubes
        foreach (GameObject obj in activeCubes)
        {
            if (obj != null)
            {
                PhysicsCubeObject physObj = obj.GetComponent<PhysicsCubeObject>();
                if (physObj != null)
                {
                    physObj.enableCollisions = enableCollisions;
                }
            }
        }

        // Mettre � jour toutes les sph�res
        foreach (GameObject obj in activeSpheres)
        {
            if (obj != null)
            {
                PhysicsSphereObject physObj = obj.GetComponent<PhysicsSphereObject>();
                if (physObj != null)
                {
                    physObj.enableCollisions = enableCollisions;
                }
            }
        }

        // Mettre � jour tous les Rubik Cubes
        foreach (GameObject obj in activeRubikCubes)
        {
            if (obj != null)
            {
                RubikCubeObject physObj = obj.GetComponent<RubikCubeObject>();
                if (physObj != null)
                {
                    physObj.enableCollisions = enableCollisions;
                }
            }
        }
    }



    public int GetActiveSimulationCount()
    {
        return GetActiveObjectCount();
    }
}

// Structure pour les valeurs par d�faut des simulations
public struct SimulationDefaults
{
    public float mass;
    public float bounciness;
    public float friction;
    public float radius;
}