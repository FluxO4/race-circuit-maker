using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class Curve : MonoBehaviour
{
    public List<Point> points = new List<Point>();
    public float totalCurveLength = 0;

    public void buildPath()
    {
        //Takes the first point, and recursively draws curves to the next point propagating along its forwardPoints list
    }

    public void Reinitialize()
    {
        points.Clear();

        for (int i = 0; i < transform.childCount; i++)
        {
            Point point = transform.GetChild(i).GetComponent<Point>();
            point.forwardPoints.Clear();
            point.backwardPoints.Clear();
        }

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            Transform nextChild = transform.GetChild((i + 1) % transform.childCount);
            Point point = child.GetComponent<Point>();

            bool isClosed = point.crossSectionCurve != null;
            Point nextPoint = nextChild.GetComponent<Point>();
            point.moveToTransform();

            if (isClosed || i != transform.childCount - 1)
            {
                point.forwardPoints.Add(nextPoint);
                nextPoint.backwardPoints.Add(point);
            }

            points.Add(point);
        }
    }


    Vector3 GetPointFromi(Curve curve, float i)
    {
        // [OPTIMIZE]

        // NOTE we're assuming that i is across meaning there can be no branches when computing i
        // meaning we can only have at max one forward point
        // meaning this only works for CrossSection curves

        // linearly searching for now
        int index = 0;
        for (; index < curve.points.Count; index++)
        {
            if (curve.points[index].normalizedPositionAlongCurve == i)
            {
                // we're exactly on the thing
                return curve.points[index].pointPosition;
            } 
            else if (curve.points[index].normalizedPositionAlongCurve < i)
            {
                // remap the range i.e. ilerp [p[i].normalized, p[i + 1].normalized] -> 0, 1

                // lerp: x = a + (b-a) * t
                // ilerp x - a /  b - a

                float a = curve.points[index].normalizedPositionAlongCurve;
                float b = curve.points[index + 1].normalizedPositionAlongCurve;
                float t = (i - a) / (b - a);
                return Point.CalculateBezierPoint(t, curve.points[index].pointPosition, curve.points[index].controlPointPositionForward, curve.points[index + 1].controlPointPositionBackward,
                    curve.points[index + 1].pointPosition);

            }
        }

        return Vector3.zero;
    }

    // a and b refer to the anchor points in the big loop which contains the curves we're getting the point from
    public Vector3 GetPointFromij(Point a, Point b, float i, float j)
    {
        Vector3 iaPos = GetPointFromi(a.crossSectionCurve, i);
        Vector3 ibPos = GetPointFromi(b.crossSectionCurve, i);

        // scaling the control points 
        float scale = totalCurveLength / (a.pointPosition - b.pointPosition).magnitude;
        float dist = (iaPos - ibPos).magnitude;

        Vector3 controlForward = (a.controlPointPositionForward / scale) * dist;
        Vector3 controlBackward = (b.controlPointPositionBackward / scale) * dist;

        return Point.CalculateBezierPoint(j, iaPos, controlForward, controlBackward, ibPos);
    }

    // NOTE by default we're assuming the curve has no branching thus index 0 for forwardPoint
    public void ComputeNormalizedPoints(int branchIndex = 0)
    {
        // first compute the total curve length
        totalCurveLength = 0;
        for (int i = 0; i < points.Count; ++i)
        {
            if (points[i].curveLengths.Count > 0)
                totalCurveLength += points[i].curveLengths[branchIndex];
            else 
                break;
        }

        float accumulation = 0;
        for (int i = 0; i < points.Count; ++i)
        {
            points[i].normalizedPositionAlongCurve = accumulation / totalCurveLength;
            if (points[i].curveLengths.Count > 0)
                accumulation += points[i].curveLengths[branchIndex];
        }
    }

}

