using UnityEngine;
using System.Collections.Generic;

public class CollisionManager : MonoBehaviour
{
    private List<GameObject> collidableObjects = new List<GameObject>();

    void Update()
    {
        CheckCollisions();
    }

    void CheckCollisions()
    {
        // Simple collision detection between registered objects
        for (int i = 0; i < collidableObjects.Count; i++)
        {
            for (int j = i + 1; j < collidableObjects.Count; j++)
            {
                if (collidableObjects[i] != null && collidableObjects[j] != null)
                {
                    CheckCollision(collidableObjects[i], collidableObjects[j]);
                }
            }
        }
    }

    void CheckCollision(GameObject obj1, GameObject obj2)
    {
        // Simple bounding box collision
        var renderer1 = obj1.GetComponent<Renderer>();
        var renderer2 = obj2.GetComponent<Renderer>();

        if (renderer1 != null && renderer2 != null)
        {
            if (renderer1.bounds.Intersects(renderer2.bounds))
            {
                // Collision detected - you can add collision response here
                Debug.Log($"Collision between {obj1.name} and {obj2.name}");
            }
        }
    }

    public void RegisterObject(GameObject obj)
    {
        if (!collidableObjects.Contains(obj))
        {
            collidableObjects.Add(obj);
        }
    }

    public void UnregisterObject(GameObject obj)
    {
        collidableObjects.Remove(obj);
    }
}