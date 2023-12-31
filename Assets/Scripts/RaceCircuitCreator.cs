using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[ExecuteAlways]
public class RaceCircuitCreator : MonoBehaviour
{
    [Range(2, 10)]
    public int cross_section_point_count = 3;

    [Range(2, 20)]
    public int width_wise_vertex_count = 10;

    [Range(0.1f, 10f)]
    public float length_wise_vertex_count_ratio = 1;

    //References
    public RaceCircuit raceCircuit;
    public GameObject gizmoHolder;
    public GameObject circuitPointGizmoHolder;
    public List<GameObject> circuitPointGizmoList = new List<GameObject>();
    /*public GameObject gizmoPrefab;*//**/


    //Prefabs:
    public GameObject circuitPointGizmoPrefab; // Might want multiple gizmo prefabs of multiplesizes
    public GameObject smallGizmoPrefab;
    public GameObject pointPrefab;
    public GameObject roadPrefab;


    //PUT ALL EDITOR RELATED CODE HERE


    //ADD FUNCTIONS AND VARIABLES, all are inspector button handlers
    public void ADD_POINT_ALONG_TRACK()
    {

    }

    public void ADD_POINT_OUTSIDE()
    {

    }

    public void CONNECT()
    {
        
    }

    public void ADD_ROAD()
    {

    }

    public void REMOVE_ROAD()
    {

    }

    public void EDIT_CROSS_SECTION()
    {
        //all gizmos are hidden or disabled or whatever, and new gizmos are created for only the cross-section points
    }







    //DRAWING FUNCTIONS AND VARIABLES

    private void DrawCircuitCurve()
    {
        
    }

    private void DrawCrossSectionCurve(Point point)
    {

    }

    private void BuildRoad()
    {

    }

    void CreateGizmo()
    {
        // Creates a gizmo based on a prefab that is selected and used to move POINTs around
    }



    //SELECTION FUNCITONS AND VARIABLES

    //Some kind of state variable saying what is selected, circuit or road. This state will be read by a button refresher function that makes buttons interactive and non-interactive based on it

    bool circuitSelected;
    Road selectedRoad; //Null if none selected
    Point selectedPoint; //Null if none selected



    public void SelectCircuit()
    {
        Debug.Log("SELECTED CIRCUIT");
        //activated when circuit object is selected
        circuitSelected = true;
        //Spline is shown for the entire network

        //TIDDY MOVE THAT REDRAW CODE HERE


        //Gizmos are created at each POINT on the circuit curve
        foreach (Point point in raceCircuit.circuitCurve.points)
        {
            GameObject t = Instantiate(circuitPointGizmoPrefab, point.transform.position, Quaternion.identity);
            t.transform.SetParent(circuitPointGizmoHolder.transform);
            t.GetComponent<CircuitPointGizmo>().correspondingPoint = point;
            circuitPointGizmoList.Add(t);
        }

        
    }

    public void SelectRoad(Road selectedRoad)
    {
        //Highlight the road somehow. Maybe give it a temporary material or something
        //Spline is shown for only the POINTS on the road
        //Only road points have gizmos, others are deleted
    }

    public void SelectPoint(Point _selectedPoint)
    {
        selectedPoint = _selectedPoint;
        //if Circuit is selected, circuit stays selected, and moving the gizmo moves the corresponding POINT

        
    }

    //Each of the above Select function also has a Deselect counterpart that destroys Gizmos and stuff like that


    public void DeselectAll()
    {
        //Activate this when you click on an empty space in the scene
        if (circuitSelected)
        {
            if (selectedPoint)
            {
                selectedPoint = null;
            }

            if (circuitPointGizmoList.Count > 0)
            {
                for (int i = circuitPointGizmoList.Count - 1; i >= 0; i = i - 1)
                {
                    DestroyImmediate(circuitPointGizmoList[i]);
                }
                circuitPointGizmoList.Clear();
            }
            circuitSelected = false;
            
        }

        
    }

    public void ButtonRefresh()
    {
        //Reads the selection state and updates buttons
    }

    private void Update()
    {
        if (EditorApplication.isPlaying)
        {
            DestroyImmediate(this);
        }
    }

    void OnEnable()
    {
        //creator = (RaceCircuitCreator)target;

        Debug.Log("Started");
        //Debug.Log("Found a creator root object w ith name: " + raceCircuitCreator);

        //Selection.selectionChanged += OnSelectionChanged;
        Selection.selectionChanged += OnSelectionChanged;
        Debug.Log("Selection function set baby!");
    }

    private void OnDestroy()
    {
        Selection.selectionChanged -= OnSelectionChanged;
    }
    private void OnDisable()
    {
        Selection.selectionChanged -= OnSelectionChanged;
    }

    private void OnSelectionChanged() //Called when selection changes in the editor
    {
        GameObject currentSelectedObject = Selection.activeGameObject;
        
        if (currentSelectedObject != null)
        {
            Debug.Log("Selected " + currentSelectedObject.name);
            if (currentSelectedObject == gameObject)
            {
                SelectCircuit();
            }
            else if (currentSelectedObject.GetComponent<CircuitPointGizmo>())
            {
                SelectPoint(currentSelectedObject.GetComponent<CircuitPointGizmo>().correspondingPoint);
            }
            else if (currentSelectedObject.GetComponent<Road>())
            {
                SelectRoad(currentSelectedObject.GetComponent<Road>());
            }
        }
        else //When clicking elsewhere
        {
            DeselectAll();
        }
    }
}

/*
public static class InitHelper
{
    public static RaceCircuitCreator raceCircuitCreator;


    //[InitializeOnLoadMethod]
    static void StartUp()
    {
        //creator = (RaceCircuitCreator)target;
        raceCircuitCreator = GameObject.FindGameObjectWithTag("RaceCircuitRootObject").GetComponent<RaceCircuitCreator>();
        Debug.Log("Started");
        Debug.Log("Found a creator root object w ith name: " + raceCircuitCreator);

        Selection.selectionChanged += OnSelectionChanged;
    }


    private static void OnSelectionChanged() //Called when selection changes in the editor
    {
        GameObject currentSelectedObject = Selection.activeGameObject;
        if (currentSelectedObject != null)
        {
            if (currentSelectedObject == raceCircuitCreator.gameObject)
            {
                raceCircuitCreator.SelectCircuit();
            }
            else if (currentSelectedObject.GetComponent<CircuitPointGizmo>())
            {
                raceCircuitCreator.SelectPoint(currentSelectedObject.GetComponent<CircuitPointGizmo>().correspondingPoint);
            }
            else if (currentSelectedObject.GetComponent<Road>())
            {
                raceCircuitCreator.SelectRoad(currentSelectedObject.GetComponent<Road>());
            }
        }
        else //When clicking elsewhere
        {
            raceCircuitCreator.DeselectAll();
        }
    }
}*/