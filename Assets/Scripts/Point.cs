using System.Collections;
using System.Collections.Generic;
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
    public List<Point> backwardPoints;

    //Forward direction
    bool continuesForward;
    public Vector3 controlPointPositionForward;
    public List<Point> forwardPoints;

    float circuitPosition; // % along the circuit's length. Initially, this will just be distance along curve, but ideally this needs to be normalised average time taken to reach here from the starting point


    public Curve crossSectionCurve;


    public void moveToGizmo(GameObject Gizmo)
    {
        transform.position = Gizmo.transform.position;
        pointPosition = transform.position;

        // update other required stuff as well
    }
 
}
