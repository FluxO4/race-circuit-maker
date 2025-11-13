using OnomiCircuitShaper.Engine.Data;
using OnomiCircuitShaper.Engine.Interface;
using OnomiCircuitShaper.Engine.EditRealm;
using OnomiCircuitShaper.Unity.Utilities;
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace OnomiCircuitShaper.Unity.Editor
{
    public class OnomiCircuitShaperEditorSceneGUI : UnityEditor.Editor
    {
        protected ICircuitShaper _circuitShaper;

        // --- Handle Drawing Logic ---

        protected void DrawAllHandles(OnomiCircuitShaper target)
        {
            if (target.Data?.circuitData?.CircuitCurves == null) return;

            // Use a local copy of the handle matrix to avoid affecting other editor GUI
            var matrix = Handles.matrix;

            foreach (CircuitCurveData curve in target.Data.circuitData.CircuitCurves)
            {
                // First, draw the Bézier curve segments
                for (int i = 0; i < curve.CurvePoints.Count; i++)
                {
                    Point p1 = _circuitShaper.GetLiveCircuit.Curves[curve].Points[curve.CurvePoints[i]];
                    
                    if (curve.IsClosed || i < curve.CurvePoints.Count - 1)
                    {
                        Point p2 = _circuitShaper.GetLiveCircuit.Curves[curve].Points[curve.CurvePoints[(i + 1) % curve.CurvePoints.Count]];
                        
                        Handles.DrawBezier(
                            p1.PointPosition.ToUnityVector3(),
                            p2.PointPosition.ToUnityVector3(),
                            p1.ForwardControlPointPosition.ToUnityVector3(),
                            p2.BackwardControlPointPosition.ToUnityVector3(),
                            Color.green,
                            null,
                            2f
                        );
                    }
                }

                // Then, draw the interactive point handles on top
                foreach (CircuitPointData point in curve.CurvePoints)
                {
                    DrawPointHandles(point);
                }
            }

            Handles.matrix = matrix;
        }

        private void DrawPointHandles(PointData point)
        {
            if (point == null || _circuitShaper == null) return;

            bool isSelected = _circuitShaper.SelectedPoints.Contains(point);

            Vector3 anchorPos = point.PointPosition.ToUnityVector3();
            Vector3 forwardPos = point.ForwardControlPointPosition.ToUnityVector3();
            Vector3 backwardPos = point.BackwardControlPointPosition.ToUnityVector3();

            // --- Draw Lines connecting control points to anchor ---
            Handles.color = isSelected ? Color.cyan : Color.blue;
            Handles.DrawLine(anchorPos, forwardPos);
            Handles.DrawLine(anchorPos, backwardPos);

            // --- Draw Control Point Handles ---
            float cpSize = HandleUtility.GetHandleSize(forwardPos) * 0.08f;
            
            // Forward CP
            Handles.color = isSelected ? Color.cyan : new Color(0, 0.5f, 0.5f);
            EditorGUI.BeginChangeCheck();
            Vector3 newForwardPos = Handles.FreeMoveHandle(forwardPos, cpSize, Vector3.zero, Handles.SphereHandleCap);
            if (EditorGUI.EndChangeCheck())
            {
                point.ForwardControlPointPosition = newForwardPos.ToNumericsVector3();
            }

            // Backward CP
            Handles.color = isSelected ? Color.cyan : new Color(0, 0.5f, 0.5f);
            EditorGUI.BeginChangeCheck();
            Vector3 newBackwardPos = Handles.FreeMoveHandle(backwardPos, cpSize, Vector3.zero, Handles.SphereHandleCap);
            if (EditorGUI.EndChangeCheck())
            {
                point.BackwardControlPointPosition = newBackwardPos.ToNumericsVector3();
            }

            // --- Draw Anchor Point Handle ---
            float anchorSize = HandleUtility.GetHandleSize(anchorPos) * 0.12f;
            Handles.color = isSelected ? Color.yellow : Color.red;
            
            // Use Handles.Button for a clickable sphere that doesn't draw a position gizmo
            if (Handles.Button(anchorPos, Quaternion.identity, anchorSize, anchorSize, Handles.SphereHandleCap))
            {
                if (Event.current.shift)
                {
                    _circuitShaper.AddPointToSelection(point as CircuitPointData);
                }
                else
                {
                    _circuitShaper.SelectPoint(point as CircuitPointData);
                }
                Repaint(); // Redraw inspector to reflect selection change
            }
            
            // If this is the single selected point, show the standard transform gizmo for movement
            if (isSelected && _circuitShaper.SelectedPoints.Count == 1)
            {
                EditorGUI.BeginChangeCheck();
                Vector3 newAnchorPos = Handles.PositionHandle(anchorPos, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    point.PointPosition = newAnchorPos.ToNumericsVector3();
                }
            }
        }
    }
}