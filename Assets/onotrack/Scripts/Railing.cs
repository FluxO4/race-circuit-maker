using System.Linq;
using UnityEngine;

public class Railing : MonoBehaviour
{
    [Range(2, 20)]
    public float railingHeight = 2;

    [Range(0, 1)]
    public float min;
    [Range(0, 1)]
    public float max;
    public float height;

    [Range(0, 1)]
    public int side; // 0 is left, 1 is right

    public Road parent;
    Mesh mesh;

    public void build()
    {
        if (min > max)
        {
            min = max - 0.1f;
        }


        int xCount = 1;
        int minz = (int)(min * parent.yCount);
        int maxz = (int)(max * parent.yCount);
        int yCount = maxz - minz;

        if (minz >= maxz)
        {
            return;
        }

        side = Mathf.Clamp(side, 0, 1) > 0.5 ? 1 : 0;
        transform.position = Vector3.zero;

        if (mesh != null)
        {
            DestroyImmediate(mesh, true);
        }

        GetComponent<MeshFilter>().mesh = mesh = new Mesh();
        mesh.name = "Ribbon";

        Vector3[] vertices = new Vector3[(xCount + 1) * (yCount + 1)];
        Vector2[] uv = new Vector2[vertices.Length];


        int currentPoint = 0;
        float segmentLength = parent.associatedPoints[currentPoint].nextSegmentLength;
        float cumulativeSegmentLength = 0;



        for (int i = 0, z = minz; z <= maxz; z++)
        {
            float j_value = (((float)z / parent.yCount) * (parent.roadLength) - cumulativeSegmentLength) / segmentLength;
            //Debug.Log("j_value is now " + j_value);

            while (j_value > 1 && currentPoint < parent.associatedPoints.Count - 1)
            {
                currentPoint += 1;
                cumulativeSegmentLength += segmentLength;
                segmentLength = parent.associatedPoints[currentPoint].nextSegmentLength;
                if (segmentLength == 0)
                {
                    segmentLength = 1;
                }

                j_value = (((float)z / parent.yCount) * (parent.roadLength) - cumulativeSegmentLength) / segmentLength;
                //  Debug.Log("More than 1! So now it's j_value is now " + j_value);
            }

            Vector3 currentUp = parent.associatedPoints[currentPoint].GetUp();
            int nextPoint = Mathf.Min(currentPoint + 1, parent.associatedPoints.Count - 1);
            Vector3 nextUp = parent.associatedPoints[nextPoint].GetUp();

            Vector3 up = Vector3.Lerp(currentUp, nextUp, j_value) * railingHeight;

            vertices[i + side] = parent.associatedPoints[currentPoint].GetPointFromij(side, j_value) - parent.associatedPoints[0].transform.position + up;
            vertices[i + 1 - side] = parent.associatedPoints[currentPoint].GetPointFromij(side, j_value) - parent.associatedPoints[0].transform.position;


            uv[i] = new Vector2(0, (float)z / parent.yCount * parent.tileY * (max - min));
            uv[i + 1] = new Vector2(1, (float)z / parent.yCount * parent.tileY * (max - min));

            // Instantiate(parent.creator.testSphere, parent.associatedPoints[currentPoint].GetPointFromij(0, j_value) - parent.associatedPoints[0].transform.position, Quaternion.identity);
            // Instantiate(parent.creator.testSphere, parent.associatedPoints[currentPoint].GetPointFromij(0, j_value) - parent.associatedPoints[0].transform.position + up, Quaternion.identity);


            i += 2;
        }



        mesh.vertices = vertices;
        mesh.uv = uv;

        int[] triangles = new int[xCount * yCount * 6];
        for (int ti = 0, vi = 0, z = 0; z < yCount; z++, vi++)
        {
            for (int x = 0; x < xCount; x++, ti += 6, vi++)
            {
                triangles[ti] = vi;
                triangles[ti + 3] = triangles[ti + 2] = vi + 1;
                triangles[ti + 4] = triangles[ti + 1] = vi + xCount + 1;
                triangles[ti + 5] = vi + xCount + 2;
            }
        }

        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        GetComponent<MeshCollider>().sharedMesh = mesh;


        transform.position = parent.associatedPoints[0].transform.position;
    }


    public void removeMesh()
    {
        if (mesh != null)
        {
            DestroyImmediate(mesh, true);
        }
    }


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
