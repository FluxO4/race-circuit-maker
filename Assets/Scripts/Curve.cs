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

    public bool IsClosedProperty { get { return isClosed; } set { AutoSetPreviousAndNextPoints(); isClosed = value; } }

    public float totalLength = 0;




    public void AutoSetAllControlPoints()
    {
        foreach(Point point in points)
        {
            point.AutoSetAnchorControlPoints();
        }
    }

    


    public void AutoSetPreviousAndNextPoints()
    {
        points.Clear();

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            Transform nextChild = transform.GetChild((i + 1) % transform.childCount);
            Point point = child.GetComponent<Point>();

            Point nextPoint = nextChild.GetComponent<Point>();

            point.gameObject.name = "Point " + i.ToString();
            point.nextPoint = nextPoint;
            point.nextPoint.prevPoint = point;

            point.parentCurve = this;
            point.creator = creator;

            if (point.crossSectionCurve)
            {
                foreach(Point cpoint in point.crossSectionCurve.points)
                {
                    CrossSectionPointGizmo t = cpoint.GetComponent<CrossSectionPointGizmo>();
                    if (t) t.parentPoint = point;
                }
            }

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
        for(int i = 0; i < points.Count; i++)
        {
            points[i].UpdateLength();
        }


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

