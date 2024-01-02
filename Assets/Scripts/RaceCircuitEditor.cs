
using System;
using System.Collections.Generic;

using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(RaceCircuitCreator)), CanEditMultipleObjects]
public class RaceCircuitEditor : Editor
{
    /*Prefabs*/
    public GameObject gizmoPrefab;

    RaceCircuitCreator creator;

    private void OnSceneGUI()
    {
        creator.findClosestPoints();
        if (creator.circuitSelected)
        {
            if (creator.selectedRoad != null)
            {
                DrawRoadHandles(creator.selectedRoad);
            }
            else if (creator.selectedPoint != null)
            {
                DrawCircuitPointHandles(creator.selectedPoint);
            }
            else
            {
                foreach (Curve curve in creator.raceCurves)
                {
                    DrawCircuitCurveHandles(curve);
                }
            }
        }
        

       /* foreach (Curve curve in creator.raceCurves)
        {
            DrawHandles(curve);
        }*/

    }

    void DrawCircuitCurveHandles(Curve curve)
    {
        for(int i = 0; i < curve.points.Count; i++)
        {
            Point point = curve.points[i];
            DrawCircuitPointHandles(point);

            if (creator.showCurves)
            {
                if(i < curve.points.Count - 1)
                {
                    Point nextPoint = curve.points[i + 1];
                    Handles.DrawBezier(point.transform.position, nextPoint.transform.position, point.controlPointPositionForward, nextPoint.controlPointPositionBackward, Color.green, null, 2);
                }
            }
        }
    }




    void DrawRoadHandles(Road road)
    {



        for (int i = 0; i < road.associatedPoints.Count; i++)
        {
            Point point = road.associatedPoints[i];
            DrawCircuitPointHandles(point);

            if (creator.showCurves)
            {
                if (i < road.associatedPoints.Count - 1)
                {
                    Point nextPoint = road.associatedPoints[i + 1];
                    Handles.DrawBezier(point.transform.position, nextPoint.transform.position, point.controlPointPositionForward, nextPoint.controlPointPositionBackward, Color.green, null, 2);
                }
            }
        }
    }


    void DrawCircuitPointHandles(Point point)
    {
        Handles.color = Color.red;

        Vector3 newPos = Handles.FreeMoveHandle(point.pointPosition, Quaternion.identity, 0.3f, Vector2.zero, Handles.SphereHandleCap);
        if (newPos != point.pointPosition)
        {
            Undo.RecordObject(creator, "Move Anchor Point 1");
            point.transform.position = newPos;
            point.moveToTransform();
            if (creator.autoSetControlPoints)
            {
                point.AutoSetAnchorControlPoints();
            }
        }

        if (creator.editingCrossSection)
        {
            DrawCrossSectionCurveHandles(point);
        }

        if (creator.editingControlPoints)
        {
            DrawControlPointHandles(point);

        }
    }



    void DrawCrossSectionCurveHandles(Point circuitPoint)
    {
        for(int i = 0; i < circuitPoint.crossSectionCurve.points.Count; i++)
        {
            Point point = circuitPoint.crossSectionCurve.points[i];

            Handles.color = Color.cyan;
            Vector3 newPos = Handles.FreeMoveHandle(point.pointPosition, Quaternion.identity, 0.3f, Vector2.zero, Handles.SphereHandleCap);

            if (newPos != point.pointPosition)
            {
                Undo.RecordObject(creator, "Move Anchor Point 1");
                point.pointPosition = newPos;
                circuitPoint.PerpendicularizeCrossSection();
                point.AutoSetAnchorControlPoints();
            }

            if(i < circuitPoint.crossSectionCurve.points.Count - 1)
            {
                Point nextPoint = circuitPoint.crossSectionCurve.points[i + 1];
                Handles.DrawBezier(point.transform.position, nextPoint.transform.position, point.controlPointPositionForward, nextPoint.controlPointPositionBackward, Color.green, null, 2);
            }

        }
    }



    void DrawControlPointHandles(Point point)
    {
        Handles.color = Color.blue;

        Handles.DrawLine(point.controlPointPositionForward, point.pointPosition, 2);
        Handles.DrawLine(point.pointPosition, point.controlPointPositionBackward, 2);

        Vector3 newPos = Handles.FreeMoveHandle(point.controlPointPositionForward, Quaternion.identity, 0.3f, Vector2.zero, Handles.SphereHandleCap);
        if (newPos != point.controlPointPositionForward)
        {
            Undo.RecordObject(creator, "Move Anchor Point 1");
            point.controlPointPositionForward = newPos;

            if (!creator.independentControlPoints)
            {
                float dist = (point.pointPosition - point.controlPointPositionBackward).magnitude;
                Vector3 dir = (point.pointPosition - newPos).normalized;
                point.controlPointPositionBackward = point.pointPosition + dir * dist;
            }
        }


        newPos = Handles.FreeMoveHandle(point.controlPointPositionBackward, Quaternion.identity, 0.3f, Vector2.zero, Handles.SphereHandleCap);
        if (newPos != point.controlPointPositionBackward)
        {
            Undo.RecordObject(creator, "Move Anchor Point 2");
            point.controlPointPositionBackward = newPos;

            if (!creator.independentControlPoints)
            {
                float dist = (point.pointPosition - point.controlPointPositionForward).magnitude;
                Vector3 dir = (point.pointPosition - newPos).normalized;
                point.controlPointPositionForward = point.pointPosition + dir * dist;
            }
        }
    }

    



    /*
    void DrawHandles(Curve curve, bool crossSection = false)
    {
        // assuming the first point in the list is the first point
        // (though I guess it doesn't matter which one we start from)
        Point firstPoint = curve.points[0];
        Point point = firstPoint;
        do
        {
            Point nextPoint = point.nextPoint;

            if (!crossSection || (point.nextPoint != firstPoint && nextPoint != null))
                DrawBezierBetweenPoints(point, nextPoint, crossSection ? Color.red : Color.blue);

            if (!crossSection)
            {
                DrawHandles(point.crossSectionCurve, true);
            }

            point = nextPoint;
        } while (point && point != firstPoint);
    }

    void DrawBezierBetweenPoints(Point p1, Point p2, Color handleColor)
    {
        if (p1 == null || p2 == null)
            return;
        // Handles.DrawBezier(p1.transform.position, p2.transform.position, p1.controlPointPositionForward, p2.controlPointPositionBackward, bezierColor, null, 2);
        Handles.color = handleColor;
        Vector3 newPos = Handles.FreeMoveHandle(p1.controlPointPositionForward, Quaternion.identity, 0.3f, Vector2.zero, Handles.SphereHandleCap);

        Handles.DrawLine(p1.controlPointPositionForward, p1.controlPointPositionBackward, 2);

        if (newPos != p1.controlPointPositionForward)
        {
            Undo.RecordObject(creator, "Move Anchor Point 1");
            p1.controlPointPositionForward = newPos;

            float dist = (p1.pointPosition - p1.controlPointPositionBackward).magnitude;
            Vector3 dir = (p1.pointPosition - newPos).normalized;
            p1.controlPointPositionBackward = p1.pointPosition + dir * dist;

            // p1.PerpendicularizeCrossSection();

        }

        newPos = Handles.FreeMoveHandle(p2.controlPointPositionBackward, Quaternion.identity, 0.3f, Vector2.zero, Handles.SphereHandleCap);
        if (newPos != p2.controlPointPositionBackward)
        {
            Undo.RecordObject(creator, "Move Anchor Point 2");
            p2.controlPointPositionBackward = newPos;

            float dist = (p2.pointPosition - p2.controlPointPositionForward).magnitude;
            Vector3 dir = (p2.pointPosition - newPos).normalized;
            p2.controlPointPositionForward = p2.pointPosition + dir * dist;

            // p2.PerpendicularizeCrossSection();
        }
    }
    */

    private void OnEnable()
    {
        creator = (RaceCircuitCreator)target;
    }


}
