using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions.Must;

public class Curve : MonoBehaviour
{
    public List<Point> points = new List<Point>();

    public bool isClosed = true;

    public void buildPath()
    {
        //Takes the first point, and recursively draws curves to the next point propagating along its forwardPoints list
    }

    public void Reinitialize()
    {
        points.Clear();

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            Transform nextChild = transform.GetChild((i + 1) % transform.childCount);
            Point point = child.GetComponent<Point>();

            bool isClosed = point.crossSectionCurve != null;
            Point nextPoint = nextChild.GetComponent<Point>();
            point.moveToTransform();

            point.nextPoint = nextPoint;
            point.nextPoint.prevPoint = point;

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
        for (int index = 0; index < curve.points.Count; index++)
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
                return Point.CalculateBezierPoint(curve.points[index].pointPosition, 
                                                    curve.points[index].controlPointPositionForward, 
                                                    curve.points[index + 1].controlPointPositionBackward,
                                                    curve.points[index + 1].pointPosition, 
                                                    t);

            }
        }

        return Vector3.zero;
    }

    // a and b refer to the anchor points in the big loop which contains the curves we're getting the point from
    public Vector3 GetPointFromij(Point a, Point b, float i, float j)
    {
        Vector3 iaPos = GetPointFromi(a.crossSectionCurve, i);
        Vector3 ibPos = GetPointFromi(b.crossSectionCurve, i);

        // TODO: rethink this
        // scaling the control points 
        float curveLength = 1; // a.curveLengths[b];
        float scale = curveLength / (a.pointPosition - b.pointPosition).magnitude;
        float dist = (iaPos - ibPos).magnitude;

        Vector3 controlForward = ((a.controlPointPositionForward - a.pointPosition) / scale) * dist + a.pointPosition;
        Vector3 controlBackward = ((b.controlPointPositionBackward - b.pointPosition) / scale) * dist + b.pointPosition;

        return Point.CalculateBezierPoint(iaPos, controlForward, controlBackward, ibPos, j);
        return Vector3.zero;
    }

}

