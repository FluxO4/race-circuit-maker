<<<<<<< Updated upstream
=======
using System;
using System.Collections.Generic;
>>>>>>> Stashed changes
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
<<<<<<< Updated upstream


=======
    private GameObject currentSelectedObject;
>>>>>>> Stashed changes
    private void OnSceneGUI()
    {
        Draw(circuit.circuitCurve, false);
        foreach (Point point in circuit.circuitCurve.points)
        {
            point.DrawDebug();
            point.NormalizeCrossSection();

            foreach (Point crossSectionPoint in point.crossSectionCurve.points)
            {
                crossSectionPoint.AutoSetAnchorControlPoints();
            }
            point.crossSectionCurve.points.First().AutoSetStart();
            point.crossSectionCurve.points.Last().AutoSetEnd();
        }

        if (Event.current.isKey)
        {
            if (Event.current.keyCode == KeyCode.M)
            {
                Debug.Log("Helo");
                foreach (Point point in circuit.circuitCurve.points)
                {
                    point.UpdateLengths();
                    foreach (Point crossSectionPoint in point.crossSectionCurve.points)
                    {
                        crossSectionPoint.UpdateLengths();
                    }
                }

            }
        }

    }

    void DrawBezierBetweenPoints(Point p1, Point p2, Color bezierColor, Color handleColor)
    {
        Handles.DrawBezier(p1.transform.position, p2.transform.position, p1.controlPointPositionForward, p2.controlPointPositionBackward, bezierColor, null, 2);
        Handles.color = handleColor;
        Vector3 newPos = Handles.FreeMoveHandle(p1.controlPointPositionForward, Quaternion.identity, 0.3f, Vector2.zero, Handles.SphereHandleCap);
        

        if (newPos != p1.controlPointPositionForward)
        {
            Undo.RecordObject(creator, "Move Anchor Point 1");
            p1.controlPointPositionForward = newPos;

            float dist = (p1.pointPosition - p1.controlPointPositionBackward).magnitude;
            Vector3 dir = (p1.pointPosition - newPos).normalized;
            p1.controlPointPositionBackward = p1.pointPosition + dir * dist;

        }

        newPos = Handles.FreeMoveHandle(p2.controlPointPositionBackward, Quaternion.identity, 0.3f, Vector2.zero, Handles.SphereHandleCap);
        if (newPos != p2.controlPointPositionBackward)
        {
            Undo.RecordObject(creator, "Move Anchor Point 2");
            p2.controlPointPositionBackward = newPos;

            float dist = (p2.pointPosition - p2.controlPointPositionForward).magnitude;
            Vector3 dir = (p2.pointPosition - newPos).normalized;
            p2.controlPointPositionForward = p2.pointPosition + dir * dist;
        }
    }

    void Draw(Curve curve, bool crossSection)
    {
        for (int i = 0; i < curve.points.Count; ++i)
        {
            Point firstPoint = curve.points[i];

            foreach (Point nextPoint in firstPoint.forwardPoints)
            {
                if (!crossSection || i != curve.points.Count - 1)
                {
                    DrawBezierBetweenPoints(firstPoint, nextPoint, crossSection ? Color.yellow : Color.green, crossSection ? Color.blue : Color.red);
                }

                // if the one we're drawing right now is in the main path, we got a cross section
                // maybe we could just test for null or something instead of this tho
                if (!crossSection)
                {
                    Draw(firstPoint.crossSectionCurve, true);
                }
            }
        }
    }

    

    private void OnEnable()
    {
        Selection.selectionChanged -= selectionChange;
        creator = (RaceCircuitCreator)target;
        // assuming there RaceCircuitCreator always holds a valid reference to a RaceCircuit
<<<<<<< Updated upstream
        circuit = creator.raceCircuit;


        circuit.circuitCurve.Reinitialize();

        foreach (Point point in circuit.circuitCurve.points)
=======
        creator.raceCircuit.circuitCurve.Reinitialize();
        bool flag =  creator.raceCircuit.bigGizmoList.Count == 0;
        foreach (Point point in creator.raceCircuit.circuitCurve.points)
>>>>>>> Stashed changes
        {
            /*if(flag)
            {
                GameObject gizmo = Instantiate(gizmoPrefab, point.pointPosition, Quaternion.identity, creator.raceCircuit.gizmos.transform);
                creator.raceCircuit.bigGizmoList.Add(gizmo);
            }*/
            
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

<<<<<<< Updated upstream
        foreach (Point p in circuit.circuitCurve.points)
        {
            Debug.Log($"{circuit.circuitCurve.totalCurveLength}: {p.normalizedPositionAlongCurve}");
            foreach (Point c in p.crossSectionCurve.points)
            {
                Debug.Log($"{p.crossSectionCurve.totalCurveLength}: {c.normalizedPositionAlongCurve}");
            }
        }
=======
        /*Debug.Log(creator);
        Debug.Log(creator.raceCircuit.circuitCurve.points.Count);*/
        circuit = creator.raceCircuit;
>>>>>>> Stashed changes

    }

    private void OnDisable()
    {
        /*selectionChange();*/
        /*Selection.selectionChanged += selectionChange;*/
        
    }
    private void selectionChange()
    {
        currentSelectedObject = Selection.activeGameObject;
        Debug.Log("OnDisable called. Last Selected: " + currentSelectedObject);
        /*if (!creator.raceCircuit.bigGizmoList.Contains(currentSelectedObject))
        {
            for (int i = 0; i < creator.raceCircuit.bigGizmoList.Count; i++)
            {
                DestroyImmediate(creator.raceCircuit.bigGizmoList[i]);
            }
            creator.raceCircuit.bigGizmoList.Clear();
        }*/

    }

    private void Awake()
    {
        Debug.Log("Up and running");
        Selection.selectionChanged += selectionChange;
    }

}


[InitializeOnLoad]
public class Startup
{
    public static GameObject creator;
    static Startup()
    {
        Startup.creator = GameObject.FindGameObjectWithTag("RaceCircuitRootObject");
        Debug.Log($"Up and running + {creator.gameObject.name}");
        Selection.selectionChanged += selectionChange;
    }
}

