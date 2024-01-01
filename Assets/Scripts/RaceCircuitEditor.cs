
using System;
using System.Collections.Generic;

using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(RaceCircuitCreator))]
public class RaceCircuitEditor : Editor
{
    /*Prefabs*/
    public GameObject gizmoPrefab;

    RaceCircuitCreator creator;
    RaceCircuit circuit;

    private GameObject currentSelectedObject;

    private void OnSceneGUI()
    {
        foreach (Curve curve in creator.raceCurves)
        {
            DrawHandles(curve);
        }

    }

    void DrawHandles(Curve curve, bool crossSection = false)
    {
        // assuming the first point in the list is the first point
        // (though I guess it doesn't matter which one we start from)
        Point firstPoint = curve.points[0];
        Point point = firstPoint;
        do
        {
            Point nextPoint = point.nextPoint;

            if (!crossSection || point.nextPoint != firstPoint)
                DrawBezierBetweenPoints(point, nextPoint, crossSection ? Color.red : Color.blue);

            if (!crossSection)
            {
                DrawHandles(point.crossSectionCurve, true);
            }

            point = nextPoint;
        } while (point != firstPoint);
    }

    void DrawBezierBetweenPoints(Point p1, Point p2, Color handleColor)
    {
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


    private void OnEnable()
    {
        //Selection.selectionChanged -= selectionChange;
        creator = (RaceCircuitCreator)target;
        // assuming there RaceCircuitCreator always holds a valid reference to a RaceCircuit

        // circuit = creator.raceCircuit;


        /*  circuit.circuitCurve.Reinitialize();

          foreach (Point point in circuit.circuitCurve.points)

          creator.raceCircuit.circuitCurve.Reinitialize();
          bool flag =  creator.raceCircuit.bigGizmoList.Count == 0;
          foreach (Point point in creator.raceCircuit.circuitCurve.points)

          {

              point.crossSectionCurve.Reinitialize();
              point.UpdateLengths();


              point.NormalizeCrossSection();
              point.AutoSetAnchorControlPoints();
              foreach (Point crossSectionPoint in point.crossSectionCurve.points)
              {
                  crossSectionPoint.UpdateLengths();
                  crossSectionPoint.AutoSetAnchorControlPoints();
              }
              point.crossSectionCurve.points.First().AutoSetStart();
              point.crossSectionCurve.points.Last().AutoSetEnd();
          }

          circuit.circuitCurve.ComputeNormalizedPoints();
          foreach (Point point in circuit.circuitCurve.points)
          {
              point.crossSectionCurve.ComputeNormalizedPoints();
          }


          foreach (Point p in circuit.circuitCurve.points)
          {
              Debug.Log($"{circuit.circuitCurve.totalCurveLength}: {p.normalizedPositionAlongCurve}");
              foreach (Point c in p.crossSectionCurve.points)
              {
                  Debug.Log($"{p.crossSectionCurve.totalCurveLength}: {c.normalizedPositionAlongCurve}");
              }
          }*/

        /*Debug.Log(creator);
        Debug.Log(creator.raceCircuit.circuitCurve.points.Count);*/



    }
}
