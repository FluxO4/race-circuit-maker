using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Curve))]
public class CurveEditor : SharedEditorMethods
{
    public override void OnInspectorGUI()
    {
        GUILayout.Label("Debug Inspector");
        DrawDefaultInspector();
    }

    private void OnSceneGUI()
    {
        if (myCurve.points.Count == 0) return;
        if (myCurve.points[0].crossSectionCurve == null) return;
        DrawCircuitCurveHandles(myCurve, myCurve.creator);
    }

    Curve myCurve;

    private void OnEnable()
    {
        myCurve = (Curve)target;
    }
}
