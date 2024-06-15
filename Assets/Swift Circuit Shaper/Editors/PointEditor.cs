using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Point))]
public class PointEditor : SharedEditorMethods
{
    public override void OnInspectorGUI()
    {
        GUILayout.Label("Point Cross Section Properties", EditorStyles.boldLabel);

        if (myPoint.crossSectionCurve != null)
        {

            // CrossSectionPointCount slider
            int pointCount = EditorGUILayout.IntSlider("CrossSectionPointCount", myPoint.crossSectionPointCount, 2, 10);
            if (pointCount != myPoint.crossSectionPointCount)
            {
                myPoint.crossSectionPointCount = pointCount;
                OnCrossSectionPointCountChanged(); // Call your function here
            }

            GUILayout.Space(10); // Add some space for visual separation
            GUILayout.Label("Debug Inspector");
        }
        DrawDefaultInspector();
    }

    // Function to run when the slider value changes
    private void OnCrossSectionPointCountChanged()
    {
        //Update the cross section
        if (!myPoint.creator) return;

        myPoint.ChangeCrossSectionPointCount(myPoint.crossSectionPointCount);

        Debug.Log("CrossSectionPointCount changed to: " + myPoint.crossSectionPointCount);


    }

    private void OnSceneGUI()
    {
        if (myPoint.crossSectionCurve == null) return;
        DrawCircuitPointHandles(myPoint, myPoint.creator);
    }

    Point myPoint;

    private void OnEnable()
    {
        myPoint = (Point)target;
    }

}
