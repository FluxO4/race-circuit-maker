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


    Vector3 GetPointFromi(float i)
    {
        Vector3 result = Vector3.zero;
        // [OPTIMIZE]
        
        // NOTE we're assuming that i is across meaning there can be no branches when computing i
        // meaning we can only have at max one forward point
        
        // linearly searching for now
        int direction = 1;
        //bool hit = false;
        // doing the min in case i = 1, so we'll stay in bounds
        for (int index = (int) Mathf.Min(i * points.Count, points.Count - 1); index >= 0 && index < points.Count; index += direction)
        {
            if (points[index].normalizedPositionAlongCurve > i) // meaning we're past the thing
            {
                // if (hit)
                direction = -1;
            } else if (points[index].normalizedPositionAlongCurve < i)
            {
                if (direction == -1) // we came from the right so this is the region that contains 'i'
                {

                    // remap 
                }
                direction = 1;
            } 
            else
            {
                // we're exactly on the target

            }

        }
        return result;
    }

    public Vector3 GetPointFromij(float i, float j)
    {
        Vector3 result = Vector3.zero;

        Vector3 iPos = GetPointFromi(i);
        return result;
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

