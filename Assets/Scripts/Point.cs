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

    public float normalizedPositionAlongCurve;

    public List<float> curveLengths = null;

    public Curve crossSectionCurve;


    public void moveToGizmo(GameObject Gizmo)
    {
        moveToPosition(Gizmo.transform.position);
    }

    public void moveToPosition(Vector3 position)
    {
        transform.position = position;
        moveToTransform();
    }

    public void moveToTransform()
    {
        pointPosition = transform.position;

        // update other required stuff as well
        // updating the lengths of all the curves starting from itself to its forward points
        // [OPTIMIZE]

        UpdateLengths();
    }

    public void UpdateLengths()
    {
        curveLengths.Clear();

        for (int i = 0; i < forwardPoints.Count; ++i)
        {
            curveLengths.Add(EstimateCurveLength(pointPosition, controlPointPositionForward, forwardPoints[i].controlPointPositionBackward, forwardPoints[i].pointPosition));
        }
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

    public static float EstimateCurveLength(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, int subdivisions = 10)
    {
        float length = 0.0f;
        Vector2 previousPoint = p0;

        for (int i = 1; i <= subdivisions; i++)
        {
            float t = (float)i / subdivisions;
            Vector2 currentPoint = CalculateBezierPoint(t, p0, p1, p2, p3);
            length += Vector2.Distance(previousPoint, currentPoint);
            previousPoint = currentPoint;
        }

        return length;
    }

    public static Vector2 CalculateBezierPoint(float t, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;

        Vector2 p = uuu * p0; // (1-t)³ * p0
        p += 3 * uu * t * p1; // 3(1-t)²t * p1
        p += 3 * u * tt * p2; // 3(1-t)t² * p2
        p += ttt * p3; // t³ * p3

        return p;
    }



    public void DrawDebug()
    {
        //if (crossSectionCurve == null)
        //    return;

        //Vector3 ac = controlPointPositionForward - controlPointPositionBackward;

        //// grabbing the distance between each of the points and placing them in the direction along the perpendicular

        //Debug.DrawLine(controlPointPositionBackward, controlPointPositionBackward + ac, Color.red);

        //{
        //    Vector3 p1 = Vector3.ProjectOnPlane(crossSectionCurve.points.First().pointPosition - pointPosition, ac) + pointPosition;
        //    Vector3 p2 = Vector3.ProjectOnPlane(crossSectionCurve.points.Last().pointPosition - pointPosition, ac) + pointPosition;
        //    Debug.DrawLine(p1, p2, Color.magenta);
        //}

    }

    // NOTE: function call is only valid if we contain a cross section
    public void NormalizeCrossSection()
    {
        /*
         ////
        ////if (perp.sqrMagnitude == 0) // meaning the vectors were parallel (which is the case if we're auto setting the control points)
        ////{
        ////    // get a vector to any of the points in the cross section (we're computing some kind of up vector
        ////    // since we can't rely on unity's up being accurate because the road rotates in 3d
        ////    Vector3 blah = crossSectionCurve.points.First().pointPosition;
        ////    Vector3 vectorToBlah = blah - pointPosition;
        ////    Vector3 perpToBlahAndAB = Vector3.Cross(ab, vectorToBlah).normalized;

        ////    // now we go along this perpendicular axis to get ab and bc that aren't parallel
        ////    Vector3 pt = pointPosition + perpToBlahAndAB;
        ////    ab = pt - a;
        ////    bc = c - pt;
        ////    perp = Vector3.Cross(ab, bc).normalized;
        ////}
        ////
        ////if (crossSectionCurve == null)
        ////    return;

        ////Vector3 ac = controlPointPositionForward - controlPointPositionBackward;

        ////foreach (Point point in crossSectionCurve.points)
        ////{
        ////    Vector3 newPos = Vector3.ProjectOnPlane(point.pointPosition - pointPosition, ac) + pointPosition;

        ////    float dist = (point.pointPosition - pointPosition).magnitude;
        ////    Vector3 dir = (newPos - pointPosition).normalized;
        ////    point.moveToPosition(pointPosition + dir * dist);

        ////    point.moveToPosition(newPos);
        ////}
        ///*/

        Vector3 edgeVector = crossSectionCurve.points.Last().pointPosition - crossSectionCurve.points.First().pointPosition;

        Vector3 ac = (controlPointPositionForward - controlPointPositionBackward).normalized;

        // we're only doing this if the two points aren't sharing the same euclidian point in space
        if (edgeVector.sqrMagnitude != 0)
        {
            Vector3 fakeUp = Vector3.Cross(edgeVector, ac);

            Vector3 perpToCrossSectionPlane = Vector3.Cross(fakeUp, edgeVector).normalized;

            //if (Vector3.Dot(perpToCrossSectionPlane, ac) < 0)
            //    perpToCrossSectionPlane = -perpToCrossSectionPlane;

            Quaternion rotation = Quaternion.FromToRotation(perpToCrossSectionPlane, ac);

            foreach (Point point in crossSectionCurve.points)
            {
                Vector3 newPos = Vector3.ProjectOnPlane(point.pointPosition - pointPosition, perpToCrossSectionPlane) + pointPosition;
                point.moveToPosition((rotation * (newPos - pointPosition)) + pointPosition);
                // TODO do this for the controlPoints as well
            }

        }

    }

}
