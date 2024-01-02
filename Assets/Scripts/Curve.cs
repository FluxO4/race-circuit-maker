using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Curve : MonoBehaviour
{
    public RaceCircuitCreator creator;

    public List<Point> points = new List<Point>();

    public bool isClosed = false;

    [HideInInspector]
    public bool prevIsClosed = false;

    public bool IsClosedProperty { get { return isClosed; } set { Reinitialize(); isClosed = value; } }

    public float totalLength = 0;




    public void AutoSetAllControlPoints()
    {
        foreach(Point point in points)
        {
            point.AutoSetAnchorControlPoints();
        }
    }

    


    public void Reinitialize()
    {
        points.Clear();

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            Transform nextChild = transform.GetChild((i + 1) % transform.childCount);
            Point point = child.GetComponent<Point>();

            Point nextPoint = nextChild.GetComponent<Point>();
            point.moveToTransform();

            point.nextPoint = nextPoint;
            point.nextPoint.prevPoint = point;

            point.parentCurve = this;
            point.creator = creator;

            points.Add(point);
        }

        if (!isClosed)
        {
            points.First().prevPoint = null;
            points.Last().nextPoint = null;
        }
    }

    public void NormalizeCurvePoints()
    {
        totalLength = 0;

        {
            Point firstPoint = points[0];
            Point point = firstPoint;
            do
            {
                Point nextPoint = point.nextPoint;
                totalLength += point.nextSegmentLength;
                point = nextPoint;
            } while (point && point != firstPoint);
        }

        {
            Point firstPoint = points[0];
            Point point = firstPoint;

            float accumulator = 0;
            do
            {
                Point nextPoint = point.nextPoint;
                if (!isClosed)
                {
                    accumulator += point.prevSegmentLength;
                    point.normalizedPositionAlongCurve = accumulator / totalLength;
                } else
                {
                    point.normalizedPositionAlongCurve = accumulator / totalLength;
                    accumulator += point.nextSegmentLength;
                }
                point = nextPoint;
            } while (point && point != firstPoint);
        }
    }

}

