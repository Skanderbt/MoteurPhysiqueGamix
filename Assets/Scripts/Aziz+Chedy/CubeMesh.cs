using UnityEngine;

public class CubeMesh
{
    public Vector3[] vertices;
    public int[] triangles;

    public CubeMesh(float a = 1f, float b = 1f, float c = 1f)
    {
        float hx = a * 0.5f, hy = b * 0.5f, hz = c * 0.5f;
        vertices = new Vector3[]
        {
            new(-hx, -hy, -hz), new(hx, -hy, -hz),
            new(hx,  hy, -hz), new(-hx,  hy, -hz),
            new(-hx, -hy,  hz), new(hx, -hy,  hz),
            new(hx,  hy,  hz), new(-hx,  hy,  hz)
        };

        triangles = new int[]
        {
            0,2,1, 0,3,2,
            4,5,6, 4,6,7,
            0,1,5, 0,5,4,
            2,3,7, 2,7,6,
            0,4,7, 0,7,3,
            1,2,6, 1,6,5
        };
    }
}