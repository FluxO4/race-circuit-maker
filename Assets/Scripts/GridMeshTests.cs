using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
[ExecuteInEditMode]
public class GridMeshTests : MonoBehaviour
{
    [Range(2, 20)]
    public int xSize =5;
    [Range(2, 20)]
    public int  zSize =5;

    [Range(1, 20)]
    public float tileX = 5;
    [Range(1, 20)]
    public float tileY = 5;


    private Mesh mesh;
    private Vector3[] vertices;

    private void Awake()
    {
        Generate();
    }

    private void OnValidate()
    {
        if (mesh != null)
        {
            DestroyImmediate(mesh, true);
        }
        Generate();
    }

    private void Generate()
    {
        GetComponent<MeshFilter>().mesh = mesh = new Mesh();
        mesh.name = "Grid";

        vertices = new Vector3[(xSize + 1) * (zSize + 1)];
        Vector2[] uv = new Vector2[vertices.Length];
        for (int i = 0, z = 0; z <= zSize; z++)
        {
            for (int x = 0; x <= xSize; x++, i++)
            {
                float x_norm = (float)x / xSize;
                float z_norm = (float)z / zSize;


                vertices[i] = new Vector3(x_norm, (Mathf.Sin(10*x_norm) + Mathf.Cos(10*z_norm))*0.1f, z_norm);
                uv[i] = new Vector2((float)x / xSize * tileX, (float)z / zSize * tileY);
            }
        }

        mesh.vertices = vertices;
        mesh.uv = uv;

        int[] triangles = new int[xSize * zSize * 6];
        for (int ti = 0, vi = 0, z = 0; z < zSize; z++, vi++)
        {
            for (int x = 0; x < xSize; x++, ti += 6, vi++)
            {
                triangles[ti] = vi;
                triangles[ti + 3] = triangles[ti + 2] = vi + 1;
                triangles[ti + 4] = triangles[ti + 1] = vi + xSize + 1;
                triangles[ti + 5] = vi + xSize + 2;
            }
        }

        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }
}
