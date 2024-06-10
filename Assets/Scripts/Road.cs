using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Road : MonoBehaviour
{
    [Range(2, 20)]
    public int width_wise_vertex_count = 10;

    [Range(0.1f, 10f)]
    public float length_wise_vertex_count_ratio = 1;

    [Range(1, 20)]
    public float tileX = 5;
    [Range(1, 20)]
    public float tileY = 5;

    [Range(0, 1)]
    public float tileOffsetX = 0;
    [Range(0, 1)]
    public float tileOffsetY = 0;

    

    
    public List<Point> associatedPoints;

    public RaceCircuitCreator creator;
    public MeshCollider myMeshCollider;

    float averageWidth = 1;
    [HideInInspector]
    public float roadLength = 1;

    private Mesh mesh;
    private Vector3[] vertices;

    public List<Railing> railings = new List<Railing>();

    [HideInInspector]
    public int yCount = 0;

    public Bridge bridge;

    public void RoadHighlight(bool activate)
    {
        //I dunno, gives it a temporary material or something to brighten it up, or maybe unity editor library has highlighting functions of its own. If the latter is the case, move this function to the editor script and add Road as a parameter, let's not inherit UnityEditor in this script
    }

    public void initializeRoad()
    {
        foreach(Point point in associatedPoints)
        {
            if(!point.includedInRoads.Contains(this))
                point.includedInRoads.Add(this);
        }
    }

    public void buildRoad()
    {
        //Again, if editor stuff is required, move this to the Editor script
        if(associatedPoints.Count <= 1)
        {
            return;
        }


        transform.position = Vector3.zero;

        averageWidth = 0;
        roadLength = 0;
        for(int i = 0; i < associatedPoints.Count; i++)
        {
            averageWidth += associatedPoints[i].crossSectionCurve.totalLength;
            if(i < associatedPoints.Count - 1)
            {
                roadLength = roadLength + associatedPoints[i].nextSegmentLength;
            }
        }
        averageWidth = averageWidth / associatedPoints.Count;

        tileX = 1;
        tileY = roadLength / averageWidth;


        int xCount = width_wise_vertex_count;
        yCount = (int)(length_wise_vertex_count_ratio * xCount * roadLength / averageWidth);
        //Debug.Log("Road will contain " + xCount + " vertices along width and " + yCount+ " vertices along length");
        //Debug.Log("Road is " + roadLength + " metres long, compared to the first segment which is " + associatedPoints[0].nextSegmentLength);

        if (mesh != null)
        {
            DestroyImmediate(mesh, true);
        }

        GetComponent<MeshFilter>().mesh = mesh = new Mesh();
        mesh.name = "Grid";

        vertices = new Vector3[(xCount + 1) * (yCount + 1)];
        Vector2[] uv = new Vector2[vertices.Length];


        int currentPoint = 0;
        float segmentLength = associatedPoints[currentPoint].nextSegmentLength;
        float cumulativeSegmentLength = 0;

        for (int i = 0, z = 0; z <= yCount; z++)
        {
            float z_norm = (float)z / yCount;

            float j_value = z_norm;

            j_value = (((float)z / yCount) * (roadLength) - cumulativeSegmentLength) / segmentLength;
            //Debug.Log("j_value is now " + j_value);

            while(j_value > 1 && currentPoint < associatedPoints.Count-1)
            {
                currentPoint += 1;
                cumulativeSegmentLength += segmentLength;
                
                segmentLength = associatedPoints[currentPoint].nextSegmentLength;
                if (segmentLength == 0)
                {
                    segmentLength = 1;
                }

                j_value = (((float)z / yCount) * (roadLength) - cumulativeSegmentLength) / segmentLength;
              //  Debug.Log("More than 1! So now it's j_value is now " + j_value);
            }


            for (int x = 0; x <= xCount; x++, i++)
            {
                float x_norm = (float)x / xCount;

                float i_value = x_norm;


                vertices[i] = associatedPoints[currentPoint].GetPointFromij(i_value, j_value) - associatedPoints[0].transform.position;
                uv[i] = new Vector2((float)x / xCount * tileX, (float)z / yCount * tileY);
            }
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

        myMeshCollider.sharedMesh = mesh;
        transform.position = associatedPoints[0].transform.position;


        foreach (Railing railing in railings)
        {
            if (railing.gameObject.activeSelf)
            {
                railing.parent = this;
                railing.build();
            }
            else
            {
                railing.removeMesh();
            }
        }

        if (bridge)
        {
            if (bridge.gameObject.activeSelf)
            {
                bridge.parent = this;
                bridge.build();
            }
            else
            {
                bridge.removeMesh();
            }
            // bridge.transform.position = transform.position;
        }

        //Builds the road mesh
    }

}
