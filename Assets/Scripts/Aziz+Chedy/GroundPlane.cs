using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class GroundPlane : MonoBehaviour
{
    void Start()
    {
        Mesh m = new Mesh();
        float s = 50f;
        m.vertices = new Vector3[] {
            new Vector3(-s,0,-s), new Vector3(s,0,-s),
            new Vector3(s,0,s), new Vector3(-s,0,s)
        };
        m.triangles = new int[] { 0, 2, 1, 0, 3, 2 };
        GetComponent<MeshFilter>().mesh = m;

        var mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0.15f, 0.15f, 0.15f);
        GetComponent<MeshRenderer>().material = mat;
    }
}