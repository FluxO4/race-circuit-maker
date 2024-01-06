
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(RaceCircuitCreator)), CanEditMultipleObjects]
public class RaceCircuitEditor : Editor
{


  /*  public override VisualElement CreateInspectorGUI()
    {
        // Create a new VisualElement to be the root of our inspector UI
        VisualElement myInspector = new VisualElement();

        // Add a simple label
        myInspector.Add(new Label("This is a custom inspector"));

        // Return the finished inspector UI
        return myInspector;
    }*/

    bool isButtonPressed = false;
    GUIStyle toggleButtonStyle = null;

    bool displayDebugInspector = true;

    public override void OnInspectorGUI()
    {
        creator = (RaceCircuitCreator)target;
        EditorGUI.BeginChangeCheck();



        if (toggleButtonStyle == null)
        {
            toggleButtonStyle = new GUIStyle(EditorStyles.miniButton);
        }

        if (GUILayout.Button("Perpendicularize points"))
        {
            creator.PerpendicularizeAllCrossSections();
        }


        isButtonPressed = GUILayout.Toggle(isButtonPressed, "Add Point", toggleButtonStyle);

        displayDebugInspector = GUILayout.Toggle(displayDebugInspector, "Show Debug Inspector", toggleButtonStyle);

        if (EditorGUI.EndChangeCheck())
        {
            if (isButtonPressed)
            {
                OnButtonPressed();
            }
            else
            {
                OnButtonUnpressed();
            }
        }

        if (displayDebugInspector) {

            GUILayout.Label("Debug Inspector");

            DrawDefaultInspector();
        }
    }

    public void OnButtonPressed()
    {
        Debug.Log("Add button pressed");

    }


    public void OnButtonUnpressed()
    {
        Debug.Log("Add button unpressed");

    }









    /*Prefabs*/
    public GameObject gizmoPrefab;

    RaceCircuitCreator creator;



    private void OnSceneGUI()
    {

        if (creator.circuitSelected)
        {
            Event guiEvent = Event.current;
            Ray mouseRay = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition);


            if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 && guiEvent.shift)
            {
                Debug.Log("MOUSE INPUT DETECTED!");
                creator.mouseInput(guiEvent.mousePosition, mouseRay);
            }
        }


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
                foreach (Curve curve in creator.raceCircuit.circuitCurves)
                {
                    DrawCircuitCurveHandles(curve);
                }
            }
        }
        

        foreach (Curve curve in creator.raceCircuit.circuitCurves)
        {
            DrawRotatorHandle(curve);
        }

       /* foreach (Curve curve in creator.raceCurves)
        {
            DrawHandles(curve);
        }*/

    }




    void DrawRotatorHandle(Curve curve)
    {
        Handles.color = Color.yellow;
        foreach(Point point in curve.points)
        {
            Vector3 handlePos = point.rotatorPointPosition;
            Handles.DrawLine(point.pointPosition, handlePos);
            Vector3 newPos = Handles.FreeMoveHandle(handlePos, Quaternion.identity, 0.3f, Vector3.zero, Handles.SphereHandleCap);

            if (newPos != handlePos)
            {
                point.rotatorPointPosition = newPos;
            }

        }
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

            if (creator.autoSetControlPoints)
            {
                point.AutoSetAnchorControlPoints();
            }
        }

        if (creator.editingCrossSection)
        {
            DrawCrossSectionPointHandles(point);
        }

        if (creator.editingControlPoints)
        {
            DrawControlPointHandles(point);

        }
    }



    void DrawCrossSectionPointHandles(Point circuitPoint)
    {
        int c = circuitPoint.crossSectionCurve.points.Count;
        for (int i = 0; i < c; i++)
        {
            Point point = circuitPoint.crossSectionCurve.points[i];

            Handles.color = Color.cyan;
            Vector3 newPos = Handles.FreeMoveHandle(point.pointPosition, Quaternion.identity, 0.3f, Vector2.zero, Handles.SphereHandleCap);

            if (newPos != point.pointPosition)
            {
                Undo.RecordObject(creator, "Move Anchor Point 1");
                newPos = Vector3.ProjectOnPlane(newPos - circuitPoint.pointPosition, circuitPoint.transform.forward) + circuitPoint.pointPosition;
                //point.ProjectSelf(circuitPoint.pointPosition, circuitPoint.GetAC());

                if (i == 0 || i == c - 1)
                {
                    newPos = Vector3.Project(newPos - circuitPoint.pointPosition, circuitPoint.transform.right) + circuitPoint.pointPosition;
                    point.pointPosition = newPos;
                    circuitPoint.transformToAlignEndPoints();
                }
                else
                {
                    point.pointPosition = newPos;
                }

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
            newPos = Vector3.ProjectOnPlane(newPos - point.pointPosition, point.transform.up) + point.pointPosition;

            Undo.RecordObject(creator, "Move Anchor Point 1");
            point.controlPointPositionForward = newPos;
            creator.pointTransformChanged = true;


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
            newPos = Vector3.ProjectOnPlane(newPos - point.pointPosition, point.transform.up) + point.pointPosition;

            Undo.RecordObject(creator, "Move Anchor Point 2");
            point.controlPointPositionBackward = newPos;
            creator.pointTransformChanged = true;
            //point.PerpendicularizeCrossSection(true);
            

            if (!creator.independentControlPoints)
            {
                float dist = (point.pointPosition - point.controlPointPositionForward).magnitude;
                Vector3 dir = (point.pointPosition - newPos).normalized;
                point.controlPointPositionForward = point.pointPosition + dir * dist;
            }
        }
    }

    

    private void OnEnable()
    {
        creator = (RaceCircuitCreator)target;
    }


}
