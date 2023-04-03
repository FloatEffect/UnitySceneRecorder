using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class SimpleMeshGenerator : MonoBehaviour
{
    Vector3[] vertices;
    int[] triangles;
    Mesh mesh;


    // Start is called before the first frame update
    void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        vertices = new Vector3[4];
        triangles = new int[6];
    }

    // Update is called once per frame
    void Update()
    {

        vertices[0] = new Vector3(1, 0, 0);
        vertices[1] = new Vector3(-2 - 2*Mathf.Sin(Time.time), 0, -2 - 2*Mathf.Cos(Time.time));
        vertices[2] = new Vector3(1, 0, 1);
        vertices[3] = new Vector3(0, 0, 1);
        triangles[0] = 0;
        triangles[1] = 1;
        triangles[2] = 2;
        triangles[3] = 2;
        triangles[4] = 1;
        triangles[5] = 3;


        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
    }
}
