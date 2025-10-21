using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Unity.VisualScripting;

public class Bridge : MonoBehaviour
{
    public float edgeWidth;
    public float bridgeHeight;
    public float flangeWidth;
    public float flangeHeight;
    public float flangeDepth = 45.0f;
    public float curbHeight;

    public int tileX;
    public int tileY;

    public Road parent;

    Mesh mesh;

    public void build()
    {
        transform.position = Vector3.zero;

        Mesh[] intermediateMeshes = new Mesh[15];

        int xCount = 1;
        int yCount = parent.yCount;

        List<Vector3[]> meshVertices = new List<Vector3[]>();
        List<Vector2[]> meshUVs = new List<Vector2[]>();
        List<int[]> meshTriangles = new List<int[]>();

        for (int i = 0; i < 15; ++i)
        {
            intermediateMeshes[i] = new Mesh();
            meshVertices.Add(new Vector3[(xCount + 1) * (yCount + 1)]);
            meshUVs.Add(new Vector2[(xCount + 1) * (yCount + 1)]);
            meshTriangles.Add(new int[xCount * yCount * 6]);
        }

        List<Vector2> bridgeShapePoints = new List<Vector2>()
        {
            new Vector3(0, 0),
            new Vector2(0, curbHeight),
            new Vector2(edgeWidth, curbHeight),
            new Vector2(edgeWidth, curbHeight - flangeDepth),
            new Vector2(edgeWidth + flangeWidth, curbHeight - flangeDepth),
            new Vector2(edgeWidth + flangeWidth, curbHeight - flangeDepth - flangeHeight),
            new Vector2(edgeWidth, curbHeight - flangeDepth - flangeHeight),
            new Vector2(0, curbHeight - bridgeHeight),
            // the final two shape points are extracted from index 7
        };

        int currentPoint = 0;
        float segmentLength = parent.associatedPoints[currentPoint].nextSegmentLength;
        float cumulativeSegmentLength = 0;

        for (int i = 0, z = 0; z <= yCount; z++)
        {
            float j_value = (((float)z / yCount) * (parent.roadLength) - cumulativeSegmentLength) / segmentLength;
            //Debug.Log("j_value is now " + j_value);

            while (j_value > 1 && currentPoint < parent.associatedPoints.Count-1)
            {
                currentPoint += 1;
                cumulativeSegmentLength += segmentLength;
                segmentLength = parent.associatedPoints[currentPoint].nextSegmentLength;
                if(segmentLength == 0)
                {
                    segmentLength = 1;
                }

                j_value = (((float)z / yCount) * (parent.roadLength) - cumulativeSegmentLength) / segmentLength;
                //  Debug.Log("More than 1! So now it's j_value is now " + j_value);
            }

            int nextPoint = Mathf.Min(currentPoint + 1, parent.associatedPoints.Count - 1);
            Vector3 currentUp = parent.associatedPoints[currentPoint].GetUp();
            Vector3 nextUp = parent.associatedPoints[nextPoint].GetUp();

            Vector3 currentEV = parent.associatedPoints[currentPoint].GetEV();
            Vector3 nextEV = parent.associatedPoints[nextPoint].GetEV();

            Vector3 up = Vector3.Lerp(currentUp, nextUp, j_value).normalized;
            Vector3 right = Vector3.Lerp(currentEV, nextEV, j_value).normalized;

            Vector3 leftOrigin = parent.associatedPoints[currentPoint].GetPointFromij(0, j_value) - parent.transform.position;
            Vector3 rightOrigin = parent.associatedPoints[currentPoint].GetPointFromij(1, j_value) - parent.transform.position;

            for (int shapeIndex = 0; shapeIndex < bridgeShapePoints.Count - 1; ++shapeIndex)
            {
                int leftIndex = shapeIndex;
                int rightIndex = shapeIndex + 7;
                meshVertices[leftIndex][i + 1] = leftOrigin + up * bridgeShapePoints[shapeIndex].y + -right * bridgeShapePoints[shapeIndex].x;
                meshVertices[leftIndex][i] = leftOrigin + up * bridgeShapePoints[shapeIndex + 1].y + -right * bridgeShapePoints[shapeIndex + 1].x;

                meshUVs[leftIndex][i + 1] = new Vector2(0, (float)z / yCount * tileY);
                meshUVs[leftIndex][i] = new Vector2((meshVertices[leftIndex][i] - meshVertices[leftIndex][i + 1]).magnitude * tileX, (float)z / yCount * tileY);

                meshVertices[rightIndex][i] = rightOrigin + up * bridgeShapePoints[shapeIndex].y + right * bridgeShapePoints[shapeIndex].x;
                meshVertices[rightIndex][i + 1] = rightOrigin + up * bridgeShapePoints[shapeIndex + 1].y + right * bridgeShapePoints[shapeIndex + 1].x;

                meshUVs[rightIndex][i] = new Vector2(0, (float)z / yCount * tileY);
                meshUVs[rightIndex][i + 1] = new Vector2((meshVertices[rightIndex][i] - meshVertices[rightIndex][i + 1]).magnitude * tileX, (float)z / yCount * tileY);

                //Instantiate(parent.creator.testSphere, meshVertices[leftIndex][i], Quaternion.identity);
                //Instantiate(parent.creator.testSphere, meshVertices[leftIndex][i + 1], Quaternion.identity);

                //Instantiate(parent.creator.testSphere, meshVertices[rightIndex][i], Quaternion.identity);
                //Instantiate(parent.creator.testSphere, meshVertices[rightIndex][i + 1], Quaternion.identity);
            }


            meshVertices[14][i + 1] = leftOrigin + up * bridgeShapePoints[7].y + -right * bridgeShapePoints[7].x;
            meshVertices[14][i] = rightOrigin + up * bridgeShapePoints[7].y + right * bridgeShapePoints[7].x;

            meshUVs[14][i + 1] = new Vector2(0, (float)z / yCount * tileY);
            meshUVs[14][i] = new Vector2((meshVertices[14][i] - meshVertices[14][i + 1]).magnitude * tileX, (float)z / yCount * tileY);

            i += 2;

            // vertices[i] = parent.associatedPoints[currentPoint].GetPointFromij(0, j_value) - parent.associatedPoints[0].transform.position + up;
            // vertices[i + 1] = parent.associatedPoints[currentPoint].GetPointFromij(0, j_value) - parent.associatedPoints[0].transform.position;
            // 
            // 
            // uv[i] = new Vector2(0, (float)z / yCount * parent.tileY);
            // uv[i + 1] = new Vector2(1, (float)z / yCount * parent.tileY);




            // Instantiate(parent.creator.testSphere, parent.associatedPoints[currentPoint].GetPointFromij(0, j_value) - parent.associatedPoints[0].transform.position, Quaternion.identity);
            // Instantiate(parent.creator.testSphere, parent.associatedPoints[currentPoint].GetPointFromij(0, j_value) - parent.associatedPoints[0].transform.position + up, Quaternion.identity);
        }

        for (int i = 0; i < meshVertices.Count; ++i)
        {
            for (int ti = 0, vi = 0, z = 0; z < yCount; z++, vi++)
            {
                for (int x = 0; x < xCount; x++, ti += 6, vi++)
                {
                    meshTriangles[i][ti] = vi;
                    meshTriangles[i][ti + 3] = meshTriangles[i][ti + 2] = vi + 1;
                    meshTriangles[i][ti + 4] = meshTriangles[i][ti + 1] = vi + xCount + 1;
                    meshTriangles[i][ti + 5] = vi + xCount + 2;
                }
            }
        }

        CombineInstance[] combine = new CombineInstance[intermediateMeshes.Length];

        for (int i = 0; i < intermediateMeshes.Length; ++i)
        {
            intermediateMeshes[i].vertices = meshVertices[i];
            intermediateMeshes[i].uv = meshUVs[i];
            intermediateMeshes[i].triangles = meshTriangles[i];
            combine[i].mesh = intermediateMeshes[i];
            combine[i].transform = transform.localToWorldMatrix;
        }

        Mesh combinedMesh = new Mesh();
        combinedMesh.CombineMeshes(combine);

        // mesh.name = "Bridge";

        if (mesh != null)
        {
            DestroyImmediate(mesh, true);
        }

        GetComponent<MeshFilter>().mesh = mesh = new Mesh();
        mesh.name = "Bridge";
        mesh.vertices = combinedMesh.vertices;
        mesh.uv = combinedMesh.uv;
        mesh.triangles = combinedMesh.triangles;
        mesh.RecalculateNormals();


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
