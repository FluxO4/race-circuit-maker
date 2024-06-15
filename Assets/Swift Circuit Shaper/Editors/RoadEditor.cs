using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Road))]
public class RoadEditor : SharedEditorMethods
{


    /* Road properties
     * 
     * Mesh vertex count properties (number of vertices along width, and length/width vertex distance ratio
     * 
     * UV mapping properties (X and Y tiling and offset)
     * 
     * Points that are present in the road, and a UI to easily move, insert and delete them
     * 
     * Point editing like in the main circuit creator object (control points, cross sections, etc.), only for the points in the road
     * 
     * 
     * 
     */
    public override void OnInspectorGUI()
    {
        GUILayout.Label("Debug Inspector");
        DrawDefaultInspector();
    }

    private void OnSceneGUI()
    {
        DrawRoadHandles(myRoad, myRoad.creator);
    }

    Road myRoad;

    private void OnEnable()
    {
        myRoad = (Road)target;
    }

}
