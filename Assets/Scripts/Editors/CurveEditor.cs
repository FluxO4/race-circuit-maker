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
        DrawCircuitCurveHandles(myCurve, myCurve.creator);
    }

    Curve myCurve;

    private void OnEnable()
    {
        myCurve = (Curve)target;
    }
}
