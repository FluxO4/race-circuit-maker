using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

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
    GameObject gizmoHolder;
    /*public GameObject gizmoPrefab;*//**/


    //Prefabs:
    GameObject largeGizmoPrefab; // Might want multiple gizmo prefabs of multi
    GameObject smallGizmoPrefab;
    GameObject pointPrefab;
    GameObject roadPrefab;


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
        //activated when circuit object is selected
        //Spline is shown for the entire network
        //Gizmos are created at each POINT on the circuit curve
        
    }

    public void SelectRoad()
    {
        //Highlight the road somehow. Maybe give it a temporary material or something
        //Spline is shown for only the POINTS on the road
        //Only road points have gizmos, others are deleted
    }

    public void SelectPoint()
    {
        //if Circuit is selected, circuit stays selected, and moving the gizmo moves the corresponding POINT

        
    }

    //Each of the above Select function also has a Deselect counterpart that destroys Gizmos and stuff like that

    public void DeselectAll()
    {
        //Activate this when you click on an empty space in the scene. Thing to detect is that neither the root Race Circuit object nor any of its children are selected in the hierarchy maybe. You can think of a faster way to do this
    }

    public void ButtonRefresh()
    {
        //Reads the selection state and updates buttons
    }


    private void OnEnable()
    {
        
    }
}
