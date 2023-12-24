using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RaceCircuitCreator))]
public class RaceCircuitEditor : Editor
{
    RaceCircuitCreator creator;
    RaceCircuit circuit;

    private void OnSceneGUI()
    {
        Draw(circuit.circuitCurve, false);
    }

    void DrawBezierBetweenPoints(Point p1, Point p2, Color bezierColor, Color handleColor)
    {
        Handles.DrawBezier(p1.transform.position, p2.transform.position, p1.controlPointPositionForward, p1.controlPointPositionBackward, bezierColor, null, 2);
        Handles.color = handleColor;
        Vector3 newPos1 = Handles.FreeMoveHandle(p1.controlPointPositionForward, Quaternion.identity, 0.1f, Vector2.zero, Handles.SphereHandleCap);
        Vector3 newPos2 = Handles.FreeMoveHandle(p1.controlPointPositionBackward, Quaternion.identity, 0.1f, Vector2.zero, Handles.SphereHandleCap);

        if (newPos1 != p1.controlPointPositionForward)
        {
            Undo.RecordObject(creator, "Move Anchor Point 1");
            p1.controlPointPositionForward = newPos1;
        }
        if (newPos2 != p1.controlPointPositionBackward)
        {
            Undo.RecordObject(creator, "Move Anchor Point 2");
            p1.controlPointPositionBackward = newPos2;
        }
    }

    void Draw(Curve curve, bool crossSection)
    {
        for (int i = 0; i < curve.points.Count; ++i)
        {
            Point firstPoint = curve.points[i];
            Point nextPoint = curve.points[(i + 1) % curve.points.Count];

            if (!crossSection || i != curve.points.Count - 1)
            {
                DrawBezierBetweenPoints(firstPoint, nextPoint, crossSection ? Color.yellow : Color.green, crossSection ? Color.blue : Color.red);
            }

            // if the one we're drawing right now is in the main path, we got a cross section
            // maybe we could just test for null or something instead of this tho
            if (!crossSection) { 
                Draw(firstPoint.crossSectionCurve, true);
            }
        }
    }

    private void OnEnable()
    {
        creator = (RaceCircuitCreator) target;
        // assuming there RaceCircuitCreator always holds a valid reference to a RaceCircuit
        Debug.Log(creator);
        Debug.Log(creator.raceCircuit.circuitCurve.points.Count);
        circuit = creator.raceCircuit;

    }
}
