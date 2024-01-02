using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Point : MonoBehaviour
{
    public int PointIndex;

    public bool GizmoVisibility; //If true, a gizmo is shown that can be selected
    public bool Selected; //If true, the gizmo is highlighted, and can be moved

    public Vector3 pointPosition;

    public Vector3 controlPointPositionBackward;
    public Vector3 controlPointPositionForward;

    public Vector3 relativeRotateHandlePosition = Vector3.up * 5;

    bool continuesBackward;
    bool continuesForward;

    public Point nextPoint;
    public Point prevPoint;

    public Curve parentCurve;
    public RaceCircuitCreator creator;

    public float nextSegmentLength;
    public float prevSegmentLength;

    float circuitPosition; // % along the circuit's length. Initially, this will just be distance along curve, but ideally this needs to be normalised average time taken to reach here from the starting point

    public float normalizedPositionAlongCurve;

    public Curve crossSectionCurve;

    public CircuitPointGizmo myGizmo;

    public void EnableGizmo(bool enable)
    {
        GetComponent<MeshRenderer>().enabled = enable;
    }

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
        Vector3 diff = transform.position - pointPosition;

        pointPosition = transform.position;
        controlPointPositionForward += diff;
        controlPointPositionBackward += diff;

        UpdateLength();
        // update other required stuff as well
    }

    public void UpdateLength()
    {
        if (nextPoint)
            nextSegmentLength = EstimateCurveLength(pointPosition, controlPointPositionForward, nextPoint.controlPointPositionBackward, nextPoint.pointPosition);
        else
            nextSegmentLength = 0f;

        if (prevPoint)
            prevSegmentLength = EstimateCurveLength(prevPoint.pointPosition, prevPoint.controlPointPositionForward, controlPointPositionBackward, pointPosition);
        else
            prevSegmentLength = 0f;
    }

    void AutoSetAnchorHelper()
    {
        Vector3 anchorPos = pointPosition;
        Vector3 dir = Vector3.zero;
        float[] neighbourDistances = new float[2];

        {
            Vector3 offset = prevPoint.pointPosition - anchorPos;
            dir += offset.normalized;
            neighbourDistances[0] = offset.magnitude;
        }

        {
            Vector3 offset = nextPoint.pointPosition - anchorPos;
            dir -= offset.normalized;
            neighbourDistances[1] = -offset.magnitude;
        }

        dir.Normalize();

        controlPointPositionBackward = anchorPos + dir * neighbourDistances[0] * .5f;
        controlPointPositionForward = anchorPos + dir * neighbourDistances[1] * .5f;
    }

    void AutoSetStart()
    {
        controlPointPositionForward = (pointPosition + nextPoint.controlPointPositionBackward) * .5f;
    }

    void AutoSetEnd()
    {
        controlPointPositionBackward = (prevPoint.controlPointPositionForward + pointPosition) * .5f;
    }

    public void AutoSetAnchorControlPoints()
    {
        if (prevPoint && nextPoint)
        {
            AutoSetAnchorHelper();
        }
        else if (!prevPoint)
        {
            AutoSetStart();
        }
        else if (!nextPoint)
        {
            AutoSetEnd();
        }
    }

    // NOTE: is there a better way to do this?
    public static float EstimateCurveLength(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, int subdivisions = 10)
    {
        float length = 0.0f;
        Vector3 previousPoint = p0;

        for (int i = 1; i <= subdivisions; i++)
        {
            float t = (float)i / subdivisions;
            Vector3 currentPoint = CalculateBezierPoint(p0, p1, p2, p3, t);
            length += Vector3.Distance(previousPoint, currentPoint);
            previousPoint = currentPoint;
        }

        return length;
    }

    public static Vector3 EvaluateQuadratic(Vector3 a, Vector3 b, Vector3 c, float t)
    {
        Vector3 p0 = Vector3.Lerp(a, b, t);
        Vector3 p1 = Vector3.Lerp(b, c, t);
        return Vector3.Lerp(p0, p1, t);
    }

    public static Vector3 CalculateBezierPoint(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t)
    {
        Vector3 p0 = EvaluateQuadratic(a, b, c, t);
        Vector3 p1 = EvaluateQuadratic(b, c, d, t);
        return Vector3.Lerp(p0, p1, t);
    }

    public Vector3 GetPointFromi(float i)
    {
        // [OPTIMIZE]

        // NOTE we're assuming that i is across meaning there can be no branches when computing i
        // meaning we can only have at max one forward point
        // meaning this only works for CrossSection curves

        // linearly searching for now
        i = Mathf.Max(0, Mathf.Min(i, 1));

        for (int index = 0; index < crossSectionCurve.points.Count; index++)
        {
            if (Mathf.Abs(crossSectionCurve.points[index].normalizedPositionAlongCurve - i) < 0.01f)
            {
                // we're exactly on the thing
                return crossSectionCurve.points[index].pointPosition;
            }
            else if ((index < crossSectionCurve.points.Count - 1) && crossSectionCurve.points[index].normalizedPositionAlongCurve < i && crossSectionCurve.points[index + 1].normalizedPositionAlongCurve > i)
            {
                // remap the range i.e. ilerp [p[i].normalized, p[i + 1].normalized] -> 0, 1

                // lerp: x = a + (b-a) * t
                // ilerp x - a /  b - a

                float a = crossSectionCurve.points[index].normalizedPositionAlongCurve;
                float b = crossSectionCurve.points[index + 1].normalizedPositionAlongCurve;
                float t = (i - a) / (b - a);
                return Point.CalculateBezierPoint(crossSectionCurve.points[index].pointPosition,
                                                    crossSectionCurve.points[index].controlPointPositionForward,
                                                    crossSectionCurve.points[index + 1].controlPointPositionBackward,
                                                    crossSectionCurve.points[index + 1].pointPosition,
                                                    t);

            }
        }

        /*Debug.Log("OUT OF RANGE SOMEHOW! FIX THIS!");*/
       // return Vector3.zero;
        return crossSectionCurve.points[0].pointPosition;
    }

    // a and b refer to the anchor points in the big loop which contains the curves we're getting the point from
    public Vector3 GetPointFromij(float i, float j)
    {
        Vector3 iaPos = GetPointFromi(i);
        Vector3 ibPos = nextPoint.GetPointFromi(i);

        // TODO: rethink this
        // scaling the control points 
        float curveLength = nextSegmentLength;

        float curveLengthFromStraightLineDistanceEstimatorMultiplier = curveLength / (pointPosition - nextPoint.pointPosition).magnitude;
        float dist = (iaPos - ibPos).magnitude;
        float estimatedCurvedLength = dist * curveLengthFromStraightLineDistanceEstimatorMultiplier;
        float scale = estimatedCurvedLength / curveLength;

        Vector3 controlForward = ((controlPointPositionForward - pointPosition) * scale) + iaPos;
        Vector3 controlBackward = ((nextPoint.controlPointPositionBackward - nextPoint.pointPosition) * scale) + ibPos;


        return Point.CalculateBezierPoint(iaPos, controlForward, controlBackward, ibPos, j);
    }



    // NOTE: function call is only valid if we contain a cross section
    public void PerpendicularizeCrossSection()
    {
        if(crossSectionCurve == null)
        {
            return;
        }
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

            relativeRotateHandlePosition = Vector3.Cross(edgeVector, perpToCrossSectionPlane).normalized;

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
