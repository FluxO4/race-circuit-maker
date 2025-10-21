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

    public void ReorganiseChildren()
    {

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
            point.pointIndex = i;

            point.parentCurve = this;
            point.creator = creator;

            if (point.crossSectionCurve)
            {
                point.crossSectionCurve.creator = creator;
                point.crossSectionCurve.AutoSetAllControlPoints();
                foreach(Point cpoint in point.crossSectionCurve.points)
                {
                    cpoint.creator = creator;

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
        for (int i = 0; i < points.Count; i++)
        {
            points[i].UpdateLength();
        }

        totalLength = 0;

        for (int i = 0; i < points.Count; i++)
        {
            Point point = points[i];
            totalLength += point.nextSegmentLength;
        }

        float accumulator = 0;
        for (int i = 0; i < points.Count; i++)
        {
            Point point = points[i];


            if (!isClosed)
            {
                accumulator += point.prevSegmentLength;
                point.normalizedPositionAlongCurve = accumulator / totalLength;
            }
            else
            {
                point.normalizedPositionAlongCurve = accumulator / totalLength;
                accumulator += point.nextSegmentLength;
            }
        }
    }


    public Vector3 LerpAlongCurve(float value01)
    {
        value01 = Mathf.Clamp01(value01);
        if (isClosed)
        {
            if(value01 == 0 || value01 == 1)
            {
                return points[0].pointPosition;
            }
            int beforePoint = 0;
            int afterPoint = 1;
            for(int i = 0; i < points.Count; i++)
            {
                if (points[i].normalizedPositionAlongCurve >= value01)
                {
                    afterPoint = i;
                    beforePoint = i - 1;
                    break;
                }
            }
            float localLerper = value01 - points[beforePoint].normalizedPositionAlongCurve;

            return Point.CalculateBezierPoint(points[beforePoint].pointPosition, points[beforePoint].controlPointPositionForward, points[afterPoint].controlPointPositionBackward, points[afterPoint].pointPosition, localLerper);

        }
        else
        {
            if (value01 == 0)
            {
                return points[0].pointPosition;
            }else if(value01 == 1)
            {
                return points[points.Count - 1].pointPosition;
            }


            int beforePoint = points.Count - 1;
            int afterPoint = 0;
            for (int i = 0; i < points.Count; i++)
            {
                if (points[i].normalizedPositionAlongCurve >= value01)
                {
                    afterPoint = i;
                    beforePoint = i - 1;
                    break;
                }
            }
            float distanceBefore = points[beforePoint].normalizedPositionAlongCurve;
            float distanceAfter = points[afterPoint].normalizedPositionAlongCurve;
            float localLerper = (value01 - distanceBefore) / (distanceAfter - distanceBefore);

            return Point.CalculateBezierPoint(points[beforePoint].pointPosition, points[beforePoint].controlPointPositionForward, points[afterPoint].controlPointPositionBackward, points[afterPoint].pointPosition, localLerper);
        }
    }

    //Old technique, less efficient
    /*
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
    }*/

}

