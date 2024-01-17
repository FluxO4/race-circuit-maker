
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




    GUIStyle toggleButtonStyle = null;

    bool displayDebugInspector = false;

    public override void OnInspectorGUI()
    {
        creator = (RaceCircuitCreator)target;
        EditorGUI.BeginChangeCheck();



        if (toggleButtonStyle == null)
        {
            toggleButtonStyle = new GUIStyle(EditorStyles.miniButton);
        }

        creator.EDIT = GUILayout.Toggle(creator.EDIT, "EDIT", toggleButtonStyle);

        if (!creator.EDIT) {

            return; 
        
        }



        if (GUILayout.Button("Perpendicularize points"))
        {
            creator.PerpendicularizeAllCrossSections();
        }


        //isButtonPressed = GUILayout.Toggle(isButtonPressed, "Add Point", toggleButtonStyle);

        creator.AddPoint = GUILayout.Toggle(creator.AddPoint, "Add Point", toggleButtonStyle);

        creator.selectPoints = GUILayout.Toggle(creator.selectPoints, "Select Points", toggleButtonStyle);

        creator.creatingCurve = GUILayout.Toggle(creator.creatingCurve, "Create Curve", toggleButtonStyle);

        if (creator.selectPoints)
        {
            

            GUILayout.Label("Selected points:");
            foreach(Point point in creator.selectedPoints)
            {
                GUILayout.Label(" - " + point.parentCurve.name + ": " + point.name);

            }

            if (creator.selectedPoints.Count > 0)
            {
                GUILayout.BeginHorizontal();


                if (creator.selectedPoints.Count > 1 && creator.continuousPoints)
                {
                    if (GUILayout.Button("Build New Road"))
                    {
                        creator.buildNewRoadFromSelectedPoints();
                        Debug.Log("New road built!");
                    }
                }

                GUILayout.EndHorizontal();
            }


            


        }

        displayDebugInspector = GUILayout.Toggle(displayDebugInspector, "Show Debug Inspector", toggleButtonStyle);

        if (EditorGUI.EndChangeCheck())
        {
            if (creator.selectPoints)
            {
                
            }
            else
            {
                creator.DeselectAllPoints();
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


    private int controlID = -1; // To store the control ID

    private void OnSceneGUI()
    {
        if (!creator.EDIT) return;


        if (creator.circuitSelected)
        {
            Event guiEvent = Event.current;
            Ray mouseRay = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition);


            if (creator.AddPoint || creator.creatingCurve)
            {
                if (guiEvent.type == EventType.MouseDown)
                {
                    controlID = GUIUtility.GetControlID(FocusType.Passive);
                    GUIUtility.hotControl = controlID;
                }

                if (GUIUtility.hotControl == controlID && guiEvent.type == EventType.MouseUp && guiEvent.button == 0)
                {
                    if (creator.AddPoint)
                    {
                        creator.addPoint(mouseRay);
                        creator.AddPoint = false;
                        GUIUtility.hotControl = 0;
                        controlID = -1;
                        guiEvent.Use();
                    }

                    if (creator.creatingCurve)
                    {
                        Debug.Log("Mouse button up detected");
                        creator.addPointToCurveBuffer(mouseRay);

                        GUIUtility.hotControl = 0;
                        controlID = -1;
                        guiEvent.Use();
                    }
                }

                if (GUIUtility.hotControl == controlID && guiEvent.type == EventType.MouseUp && guiEvent.button == 1)
                {
                    if (creator.creatingCurve)
                    {
                        creator.finishCreatingCurve();

                        GUIUtility.hotControl = 0;
                        controlID = -1;
                        guiEvent.Use();
                    }
                }


                if (creator.creatingCurve)
                {

                    DrawBufferCurveHandles(mouseRay);

                    if (guiEvent.type == EventType.KeyDown && guiEvent.keyCode == KeyCode.Escape)
                    {
                        creator.abortCreatingCurve();
                    }

                    if (guiEvent.type == EventType.KeyDown && (guiEvent.keyCode == KeyCode.Return || guiEvent.keyCode == KeyCode.KeypadEnter))
                    {
                        creator.finishCreatingCurve();
                    }


                }
            }


            if (controlID ==-1)
            foreach (Curve curve in creator.raceCircuit.circuitCurves)
            {
                DrawCircuitCurveHandles(curve);
            }
                
            
            
            
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
            Vector3 newPos = Handles.FreeMoveHandle(handlePos, Quaternion.identity, 2f, Vector3.zero, Handles.SphereHandleCap);

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
                if (curve.isClosed)
                {
                    if (i < curve.points.Count)
                    {
                        Point nextPoint = curve.points[i].nextPoint;
                        Handles.DrawBezier(point.transform.position, nextPoint.transform.position, point.controlPointPositionForward, nextPoint.controlPointPositionBackward, Color.green, null, 2);
                    }
                }
                else
                {
                    if (i < curve.points.Count - 1)
                    {
                        Point nextPoint = curve.points[i + 1];
                        Handles.DrawBezier(point.transform.position, nextPoint.transform.position, point.controlPointPositionForward, nextPoint.controlPointPositionBackward, Color.green, null, 2);
                    }
                }
            }
        }
    }


    void DrawBufferCurveHandles(Ray ray)
    {
        Curve curve = creator.curveBuffer;

        if (curve)
        {

            for (int i = 0; i < curve.points.Count; i++)
            {
                Point point = curve.points[i];
                DrawCircuitPointHandles(point);


                if (curve.isClosed)
                {
                    if (i < curve.points.Count)
                    {
                        Point nextPoint = curve.points[i].nextPoint;
                        Handles.DrawBezier(point.transform.position, nextPoint.transform.position, point.controlPointPositionForward, nextPoint.controlPointPositionBackward, Color.green, null, 2);
                    }
                }
                else
                {
                    if (i < curve.points.Count - 1)
                    {
                        Point nextPoint = curve.points[i + 1];
                        Handles.DrawBezier(point.transform.position, nextPoint.transform.position, point.controlPointPositionForward, nextPoint.controlPointPositionBackward, Color.green, null, 2);
                    }
                }

            }


            Vector3 mousePos = ray.origin;
            if (ray.direction.y == 0)
            {
                if (ray.origin.y != creator.curveBufferHeight)
                {
                    return;
                }
            }

            float t = (creator.curveBufferHeight - ray.origin.y) / ray.direction.y;

            if (t < 0)
            {
                return;
            }

            mousePos = ray.origin + t * ray.direction;

            if(curve.points.Count > 0)
            Handles.DrawBezier(curve.points[^1].transform.position, mousePos, curve.points[^1].controlPointPositionForward, (mousePos + curve.points[^1].controlPointPositionForward) *0.5f, Color.green, null, 2);


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
        Handles.color = point.Selected ? creator.selectedPointColor: creator.pointGizmoColor ;
        //Handles.DrawSolidDisc(point.pointPosition, SceneView.GetAllSceneCameras()[0].transform.position - point.pointPosition, 2);
        Event e = Event.current;
        int controlID = GUIUtility.GetControlID(FocusType.Passive);
        switch (e.GetTypeForControl(controlID))
        {
            case EventType.MouseDown:
                if (HandleUtility.nearestControl == controlID && e.button == 0)
                {
                    Debug.Log("Handle clicked!");



                    if (creator.selectPoints)
                    {
                        if (point.Selected)
                        {
                            creator.DeselectPoint(point);
                        }
                        else
                        {
                            creator.SelectPoint(point);
                        }
                    }
                    // Handle logic when the handle is clicked but newPos isn't different
                }
                break;
        }


        Vector3 newPos = Handles.FreeMoveHandle(controlID, point.pointPosition, Quaternion.identity, 4f, Vector2.zero, Handles.SphereHandleCap);

        if (creator.selectPoints) return;

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


        Handles.color = Color.yellow;
        Vector3 handlePos = point.rotatorPointPosition;

        Handles.DrawLine(point.pointPosition, handlePos);
        Vector3 newPo2 = Handles.FreeMoveHandle(handlePos, Quaternion.identity, 2f, Vector3.zero, Handles.SphereHandleCap);

        if (newPo2 != handlePos)
        {
            point.rotatorPointPosition = newPo2;
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

        Vector3 newPos = Handles.FreeMoveHandle(point.controlPointPositionForward, Quaternion.identity, 2f, Vector2.zero, Handles.SphereHandleCap);
        
        
        if (newPos != point.controlPointPositionForward)
        {
            newPos = Vector3.ProjectOnPlane(newPos - point.pointPosition, point.transform.up) + point.pointPosition;

            Undo.RecordObject(creator, "Move Anchor Point 1");
            point.controlPointPositionForward = newPos;
            //creator.pointTransformChanged = true;


            if (!creator.independentControlPoints)
            {
                float dist = (point.pointPosition - point.controlPointPositionBackward).magnitude;
                Vector3 dir = (point.pointPosition - newPos).normalized;
                point.controlPointPositionBackward = point.pointPosition + dir * dist;
            }
        }


        newPos = Handles.FreeMoveHandle(point.controlPointPositionBackward, Quaternion.identity, 2f, Vector2.zero, Handles.SphereHandleCap);
        if (newPos != point.controlPointPositionBackward)
        {
            newPos = Vector3.ProjectOnPlane(newPos - point.pointPosition, point.transform.up) + point.pointPosition;

            Undo.RecordObject(creator, "Move Anchor Point 2");
            point.controlPointPositionBackward = newPos;
            //creator.pointTransformChanged = true;
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
