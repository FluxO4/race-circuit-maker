using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Point : MonoBehaviour
{
    public int PointIndex;

    public bool GizmoVisibility; //If true, a gizmo is shown that can be selected
    public bool Selected; //If true, the gizmo is highlighted, and can be moved

    public Vector3 pointPosition;

    //Backward direction
    bool continuesBackward;
    public Vector3 controlPointPositionBackward;
    public List<Point> backwardPoints = new List<Point>();

    //Forward direction
    bool continuesForward;
    public Vector3 controlPointPositionForward;
    public List<Point> forwardPoints = new List<Point>();

    float circuitPosition; // % along the circuit's length. Initially, this will just be distance along curve, but ideally this needs to be normalised average time taken to reach here from the starting point


    public Curve crossSectionCurve;


    public void moveToGizmo(GameObject Gizmo)
    {
        transform.position = Gizmo.transform.position;
        pointPosition = transform.position;

        // update other required stuff as well
    }

    public void moveToTransform()
    {
        pointPosition = transform.position;
    }

    void AutoSetAnchorHelper()
    {
        Vector3 anchorPos = pointPosition;
        Vector3 dir = Vector3.zero;
        float[] neighbourDistances = new float[2];

        {
            Vector3 offset = backwardPoints.First().pointPosition - anchorPos;
            dir += offset.normalized;
            neighbourDistances[0] = offset.magnitude;
        }

        {
            Vector3 offset = forwardPoints.First().pointPosition - anchorPos;
            dir -= offset.normalized;
            neighbourDistances[1] = -offset.magnitude;
        }

        dir.Normalize();

        controlPointPositionBackward = anchorPos + dir * neighbourDistances[0] * .5f;
        controlPointPositionForward = anchorPos + dir * neighbourDistances[1] * .5f;
    }

    public void AutoSetStart()
    {
        controlPointPositionForward = (pointPosition + forwardPoints.First().controlPointPositionBackward) * .5f;
    }

    public void AutoSetEnd()
    {
        controlPointPositionBackward = (backwardPoints.Last().controlPointPositionForward + pointPosition) * .5f;
    }

    public void AutoSetAnchorControlPoints()
    {
        if (crossSectionCurve != null)
        {
            AutoSetAnchorHelper();
        }
        else
        {
            if (backwardPoints.Count != 0 && forwardPoints.Count != 0) 
            {
                AutoSetAnchorHelper();
            }
        }
    }
}
