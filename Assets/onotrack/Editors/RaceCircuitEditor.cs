
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(RaceCircuitCreator)), CanEditMultipleObjects]
public class RaceCircuitEditor : SharedEditorMethods
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



       /* if (GUILayout.Button("Perpendicularize points"))
        {
            creator.PerpendicularizeAllCrossSections();
        }*/


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

                    DrawBufferCurveHandles(mouseRay, creator);

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
                DrawCircuitCurveHandles(curve, creator);
            }
                
            
            
            
        }

        

       /* foreach (Curve curve in creator.raceCurves)
        {
            DrawHandles(curve);
        }*/

    }






    

    private void OnEnable()
    {
        creator = (RaceCircuitCreator)target;
    }


}
