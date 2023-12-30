using System.Collections.Generic;
using UnityEngine;

public class Curve : MonoBehaviour
{
    public List<Point> points = new List<Point>();

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
}

